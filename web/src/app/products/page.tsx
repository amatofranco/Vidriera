"use client";

import { useEffect, useRef, useState } from "react";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { runWithConcurrency } from "@/lib/concurrency";
import {
  ApiError,
  assignProductSection,
  createProduct,
  createSection,
  deleteProduct,
  deleteSection,
  fetchCompanyLogoUrl,
  generateCatalog,
  getCatalogHistory,
  getProducts,
  getSections,
  reorderSectionProducts,
  reorderTopLevel,
  updateStock,
  uploadCompanyLogo,
  uploadSheet,
  type CatalogHistoryItem,
  type GenerateCatalogResult,
  type Product,
  type Section,
} from "@/lib/api";

// Mismo límite que el backend ([RequestSizeLimit] en ProductsController/SectionsController) --
// validar acá evita que un archivo de más subiendo por una conexión lenta se quede
// "colgado" varios minutos antes de fallar con un error de red genérico.
const MAX_FILE_SIZE_BYTES = 20_000_000;
const MAX_FILE_SIZE_LABEL = "20MB";

function formatFileSize(bytes: number) {
  return `${(bytes / 1_000_000).toFixed(1)}MB`;
}

export default function ProductsPage() {
  const router = useRouter();
  const { auth, isLoading: authLoading, logout } = useAuth();

  const [products, setProducts] = useState<Product[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [newFiles, setNewFiles] = useState<File[]>([]);
  const [newName, setNewName] = useState("");
  const [isCreating, setIsCreating] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<{ done: number; total: number } | null>(
    null
  );
  const newFileInputRef = useRef<HTMLInputElement>(null);

  const [isGenerating, setIsGenerating] = useState(false);
  const [catalogResult, setCatalogResult] = useState<GenerateCatalogResult | null>(null);

  const [catalogHistory, setCatalogHistory] = useState<CatalogHistoryItem[]>([]);
  const [isHistoryOpen, setIsHistoryOpen] = useState(false);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);

  const [confirmingDeleteId, setConfirmingDeleteId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const [pendingBulkDelete, setPendingBulkDelete] = useState<{
    label: string;
    targets: Product[];
  } | null>(null);
  const [isBulkDeleting, setIsBulkDeleting] = useState(false);

  const [search, setSearch] = useState("");
  const [draggedTopLevelId, setDraggedTopLevelId] = useState<string | null>(null);
  const [draggedSectionMemberId, setDraggedSectionMemberId] = useState<string | null>(null);

  const [sections, setSections] = useState<Section[]>([]);
  const [newSectionFile, setNewSectionFile] = useState<File | null>(null);
  const [newSectionName, setNewSectionName] = useState("");
  const [isCreatingSection, setIsCreatingSection] = useState(false);
  const newSectionFileInputRef = useRef<HTMLInputElement>(null);
  const [confirmingDeleteSectionId, setConfirmingDeleteSectionId] = useState<string | null>(null);
  const [isDeletingSection, setIsDeletingSection] = useState(false);

  const [isBulkAssigningSection, setIsBulkAssigningSection] = useState(false);
  const [bulkAssignSelectedIds, setBulkAssignSelectedIds] = useState<Set<string>>(new Set());
  const [bulkAssignTargetId, setBulkAssignTargetId] = useState("");
  const [isApplyingBulkAssign, setIsApplyingBulkAssign] = useState(false);

  const [logoUrl, setLogoUrl] = useState<string | null>(null);
  const [isUploadingLogo, setIsUploadingLogo] = useState(false);

  useEffect(() => {
    if (!authLoading && !auth) {
      router.replace("/login");
    }
  }, [auth, authLoading, router]);

  async function loadProducts(token: string, options?: { silent?: boolean }) {
    // "Silent" skips the isLoading flag -- used for refetches after an action already
    // succeeded (assign/delete a section, bulk-assign) just to resync sortOrder, where
    // swapping the whole list out for "Cargando..." reads as a glitch, not a refresh.
    const silent = options?.silent ?? false;
    if (!silent) setIsLoading(true);
    setError(null);
    try {
      const result = await getProducts(token);
      setProducts(result);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        logout();
        router.replace("/login");
        return;
      }
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar los productos.");
    } finally {
      if (!silent) setIsLoading(false);
    }
  }

  async function loadSections(token: string) {
    try {
      const result = await getSections(token);
      setSections(result);
    } catch {
      // Las carátulas son un complemento -- si esto falla, la lista de productos
      // sigue andando igual (se ven todos como sueltos).
    }
  }

  async function loadCatalogHistory(token: string) {
    setIsLoadingHistory(true);
    try {
      const result = await getCatalogHistory(token);
      setCatalogHistory(result);
    } catch {
      // El historial es un complemento -- no bloquea el resto de la página si falla.
    } finally {
      setIsLoadingHistory(false);
    }
  }

  useEffect(() => {
    if (!auth) return;
    // Fetching from the API on mount/auth-change, not derivable during render.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    loadProducts(auth.token);
    loadSections(auth.token);
  }, [auth]);

  useEffect(() => {
    if (!auth) return;
    let cancelled = false;
    let objectUrl: string | null = null;

    fetchCompanyLogoUrl(auth.token).then((url) => {
      if (cancelled) return;
      objectUrl = url;
      setLogoUrl(url);
    });

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [auth]);

  async function handleLogoChange(file: File) {
    if (!auth) return;
    setIsUploadingLogo(true);
    setError(null);
    try {
      await uploadCompanyLogo(auth.token, file);
      const url = await fetchCompanyLogoUrl(auth.token);
      setLogoUrl(url);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudo subir el banner.");
    } finally {
      setIsUploadingLogo(false);
    }
  }

  async function handleToggleStock(product: Product) {
    if (!auth) return;
    const nextValue = !product.hasStock;
    setProducts((prev) =>
      prev.map((p) => (p.id === product.id ? { ...p, hasStock: nextValue } : p))
    );
    try {
      await updateStock(auth.token, product.id, nextValue);
    } catch {
      setProducts((prev) =>
        prev.map((p) => (p.id === product.id ? { ...p, hasStock: !nextValue } : p))
      );
      setError("No se pudo actualizar el stock, intentá de nuevo.");
    }
  }

  // Operates on whatever the search box currently shows, so a filtered subset can be
  // bulk-toggled without touching the rest of a long (e.g. 200-product) list.
  async function handleBulkStockToggle(nextValue: boolean) {
    if (!auth) return;
    const query = search.trim().toLowerCase();
    const targets = products.filter(
      (p) => p.name.toLowerCase().includes(query) && p.hasStock !== nextValue
    );
    if (targets.length === 0) return;

    const targetIds = new Set(targets.map((p) => p.id));
    setProducts((prev) =>
      prev.map((p) => (targetIds.has(p.id) ? { ...p, hasStock: nextValue } : p))
    );
    try {
      await Promise.all(targets.map((p) => updateStock(auth.token, p.id, nextValue)));
    } catch {
      setError("No se pudo actualizar el stock de todos los productos, revisá la lista.");
      loadProducts(auth.token);
    }
  }

  async function handleCreateProduct(e: React.FormEvent) {
    e.preventDefault();
    if (!auth || newFiles.length === 0) return;
    setIsCreating(true);
    setError(null);

    // The name override only makes sense for a single file; for a bulk pick every
    // product falls back to its own filename (the backend's existing default).
    const nameOverride = newFiles.length === 1 ? newName.trim() || undefined : undefined;
    const failed: { name: string; message: string }[] = [];
    const oversized = newFiles.filter((f) => f.size > MAX_FILE_SIZE_BYTES);
    const files = newFiles.filter((f) => f.size <= MAX_FILE_SIZE_BYTES);
    setUploadProgress({ done: 0, total: files.length });

    for (const file of oversized) {
      failed.push({
        name: file.name,
        message: `Pesa ${formatFileSize(file.size)}, supera el máximo de ${MAX_FILE_SIZE_LABEL}.`,
      });
    }

    await runWithConcurrency(files, 4, async (file) => {
      try {
        const created = await createProduct(auth!.token, file, nameOverride);
        setProducts((prev) => [...prev, created]);
      } catch (err) {
        failed.push({
          name: file.name,
          message: err instanceof ApiError ? err.message : "Error desconocido",
        });
      }
      setUploadProgress((prev) => (prev ? { ...prev, done: prev.done + 1 } : prev));
    });

    setIsCreating(false);
    setUploadProgress(null);
    setNewFiles([]);
    setNewName("");
    if (newFileInputRef.current) newFileInputRef.current.value = "";

    if (failed.length > 0) {
      setError(
        `${failed.length} de ${newFiles.length} archivo(s) no se pudieron subir: ${failed
          .map((f) => `${f.name} (${f.message})`)
          .join(", ")}`
      );
    }
  }

  async function handleUploadSheet(product: Product, file: File) {
    if (!auth) return;
    if (file.size > MAX_FILE_SIZE_BYTES) {
      setError(
        `"${file.name}" pesa ${formatFileSize(file.size)}, supera el máximo de ${MAX_FILE_SIZE_LABEL}.`
      );
      return;
    }
    try {
      await uploadSheet(auth.token, product.id, file);
      setProducts((prev) =>
        prev.map((p) => (p.id === product.id ? { ...p, hasSheet: true } : p))
      );
    } catch {
      setError(`No se pudo subir la ficha de "${product.name}".`);
    }
  }

  async function handleDeleteProduct(product: Product) {
    if (!auth) return;
    setIsDeleting(true);
    setError(null);
    try {
      await deleteProduct(auth.token, product.id);
      setProducts((prev) => prev.filter((p) => p.id !== product.id));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : `No se pudo borrar "${product.name}".`);
    } finally {
      setIsDeleting(false);
      setConfirmingDeleteId(null);
    }
  }

  function requestBulkDeleteSelected() {
    // Same checkbox as "tiene stock" -- checking it means "selected", whether that
    // selection ends up in a generated catalog or gets bulk-deleted.
    const targets = products.filter((p) => p.hasStock);
    if (targets.length === 0) return;
    setPendingBulkDelete({
      label: `${targets.length} producto${targets.length === 1 ? "" : "s"} seleccionado${targets.length === 1 ? "" : "s"}`,
      targets,
    });
  }

  function requestBulkDeleteAll() {
    if (filteredProducts.length === 0) return;
    setPendingBulkDelete({
      label: search.trim()
        ? `${filteredProducts.length} producto${filteredProducts.length === 1 ? "" : "s"} filtrado${filteredProducts.length === 1 ? "" : "s"}`
        : `TODOS los productos (${filteredProducts.length})`,
      targets: filteredProducts,
    });
  }

  async function handleConfirmBulkDelete() {
    if (!auth || !pendingBulkDelete) return;
    setIsBulkDeleting(true);
    setError(null);
    const targets = pendingBulkDelete.targets;
    const failed: string[] = [];

    await runWithConcurrency(targets, 4, async (product) => {
      try {
        await deleteProduct(auth!.token, product.id);
        setProducts((prev) => prev.filter((p) => p.id !== product.id));
      } catch {
        failed.push(product.name);
      }
    });

    setIsBulkDeleting(false);
    setPendingBulkDelete(null);
    if (failed.length > 0) {
      setError(`No se pudieron borrar: ${failed.join(", ")}`);
    }
  }

  // Sections and loose (unsectioned) products share one ordering space at the top
  // level -- this is the same list the backend interleaves when it walks the catalog
  // to decide where each cover/product actually lands in the merged PDF.
  function buildTopLevelRows(): Array<
    | { type: "section"; id: string; sortOrder: number; section: Section }
    | { type: "product"; id: string; sortOrder: number; product: Product }
  > {
    const looseProducts = products.filter((p) => p.sectionId === null);
    return [
      ...sections.map((s) => ({ type: "section" as const, id: s.id, sortOrder: s.sortOrder, section: s })),
      ...looseProducts.map((p) => ({ type: "product" as const, id: p.id, sortOrder: p.sortOrder, product: p })),
    ].sort((a, b) => a.sortOrder - b.sortOrder);
  }

  async function persistTopLevelReorder(rows: ReturnType<typeof buildTopLevelRows>) {
    if (!auth) return;
    try {
      await reorderTopLevel(
        auth.token,
        rows.map((r) => ({ type: r.type, id: r.id }))
      );
    } catch {
      setError("No se pudo guardar el nuevo orden.");
      loadProducts(auth.token);
      loadSections(auth.token);
    }
  }

  function moveTopLevelItem(id: string, toIndex: number) {
    const rows = buildTopLevelRows();
    const fromIndex = rows.findIndex((r) => r.id === id);
    if (fromIndex === -1) return;
    const clampedToIndex = Math.min(Math.max(toIndex, 0), rows.length - 1);
    if (clampedToIndex === fromIndex) return;

    const next = [...rows];
    const [moved] = next.splice(fromIndex, 1);
    next.splice(clampedToIndex, 0, moved);

    const sortOrderById = new Map(next.map((row, index) => [row.id, index]));
    setSections((prev) =>
      prev.map((s) => (sortOrderById.has(s.id) ? { ...s, sortOrder: sortOrderById.get(s.id)! } : s))
    );
    setProducts((prev) =>
      prev.map((p) =>
        p.sectionId === null && sortOrderById.has(p.id) ? { ...p, sortOrder: sortOrderById.get(p.id)! } : p
      )
    );

    persistTopLevelReorder(next);
  }

  function handleTopLevelDrop(targetId: string) {
    if (!draggedTopLevelId || draggedTopLevelId === targetId) {
      setDraggedTopLevelId(null);
      return;
    }
    const toIndex = buildTopLevelRows().findIndex((r) => r.id === targetId);
    if (toIndex !== -1) moveTopLevelItem(draggedTopLevelId, toIndex);
    setDraggedTopLevelId(null);
  }

  function handleTopLevelMoveToPosition(id: string, rawValue: string) {
    const position = parseInt(rawValue, 10);
    if (Number.isNaN(position)) return;
    moveTopLevelItem(id, position - 1);
  }

  function sectionMembers(sectionId: string) {
    return products.filter((p) => p.sectionId === sectionId).sort((a, b) => a.sortOrder - b.sortOrder);
  }

  // Same dual meaning as the per-product checkbox: toggles "tiene stock" for every
  // member normally, or toggles their bulk-assign selection while that mode is active.
  function sectionCheckboxChecked(sectionId: string) {
    const members = sectionMembers(sectionId);
    if (members.length === 0) return false;
    return isBulkAssigningSection
      ? members.every((p) => bulkAssignSelectedIds.has(p.id))
      : members.every((p) => p.hasStock);
  }

  async function handleToggleSectionCheckbox(section: Section) {
    const members = sectionMembers(section.id);
    if (members.length === 0) return;
    const nextValue = !sectionCheckboxChecked(section.id);

    if (isBulkAssigningSection) {
      setBulkAssignSelectedIds((prev) => {
        const next = new Set(prev);
        members.forEach((p) => (nextValue ? next.add(p.id) : next.delete(p.id)));
        return next;
      });
      return;
    }

    if (!auth) return;
    const targets = members.filter((p) => p.hasStock !== nextValue);
    if (targets.length === 0) return;
    const targetIds = new Set(targets.map((p) => p.id));
    setProducts((prev) => prev.map((p) => (targetIds.has(p.id) ? { ...p, hasStock: nextValue } : p)));
    try {
      await Promise.all(targets.map((p) => updateStock(auth.token, p.id, nextValue)));
    } catch {
      setError(`No se pudo actualizar el stock de todos los productos de "${section.name}".`);
      loadProducts(auth.token, { silent: true });
    }
  }

  async function persistSectionReorder(sectionId: string, members: Product[]) {
    if (!auth) return;
    try {
      await reorderSectionProducts(
        auth.token,
        sectionId,
        members.map((p) => p.id)
      );
    } catch {
      setError("No se pudo guardar el orden de la carátula.");
      loadProducts(auth.token);
    }
  }

  function moveSectionMember(sectionId: string, id: string, toIndex: number) {
    const members = sectionMembers(sectionId);
    const fromIndex = members.findIndex((p) => p.id === id);
    if (fromIndex === -1) return;
    const clampedToIndex = Math.min(Math.max(toIndex, 0), members.length - 1);
    if (clampedToIndex === fromIndex) return;

    const next = [...members];
    const [moved] = next.splice(fromIndex, 1);
    next.splice(clampedToIndex, 0, moved);

    const sortOrderById = new Map(next.map((p, index) => [p.id, index]));
    setProducts((prev) =>
      prev.map((p) => (sortOrderById.has(p.id) ? { ...p, sortOrder: sortOrderById.get(p.id)! } : p))
    );

    persistSectionReorder(sectionId, next);
  }

  function handleSectionMemberDrop(sectionId: string, targetId: string) {
    if (!draggedSectionMemberId || draggedSectionMemberId === targetId) {
      setDraggedSectionMemberId(null);
      return;
    }
    const toIndex = sectionMembers(sectionId).findIndex((p) => p.id === targetId);
    if (toIndex !== -1) moveSectionMember(sectionId, draggedSectionMemberId, toIndex);
    setDraggedSectionMemberId(null);
  }

  function handleSectionMemberMoveToPosition(sectionId: string, id: string, rawValue: string) {
    const position = parseInt(rawValue, 10);
    if (Number.isNaN(position)) return;
    moveSectionMember(sectionId, id, position - 1);
  }

  async function handleCreateSection(e: React.FormEvent) {
    e.preventDefault();
    if (!auth || !newSectionFile) return;
    if (newSectionFile.size > MAX_FILE_SIZE_BYTES) {
      setError(
        `"${newSectionFile.name}" pesa ${formatFileSize(newSectionFile.size)}, supera el máximo de ${MAX_FILE_SIZE_LABEL}.`
      );
      return;
    }
    setIsCreatingSection(true);
    setError(null);
    try {
      const created = await createSection(auth.token, newSectionFile, newSectionName.trim() || undefined);
      setSections((prev) => [...prev, created]);
      setNewSectionFile(null);
      setNewSectionName("");
      if (newSectionFileInputRef.current) newSectionFileInputRef.current.value = "";
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudo crear la carátula.");
    } finally {
      setIsCreatingSection(false);
    }
  }

  async function handleDeleteSection(section: Section) {
    if (!auth) return;
    setIsDeletingSection(true);
    setError(null);
    try {
      await deleteSection(auth.token, section.id);
      setSections((prev) => prev.filter((s) => s.id !== section.id));
      // Members are detached server-side, not deleted -- refetch so their new
      // top-level sectionId/sortOrder come back in sync.
      loadProducts(auth.token, { silent: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : `No se pudo borrar la carátula "${section.name}".`);
    } finally {
      setIsDeletingSection(false);
      setConfirmingDeleteSectionId(null);
    }
  }

  async function handleAssignSection(product: Product, sectionId: string | null) {
    if (!auth) return;
    const previousSectionId = product.sectionId;
    setProducts((prev) => prev.map((p) => (p.id === product.id ? { ...p, sectionId } : p)));
    try {
      await assignProductSection(auth.token, product.id, sectionId);
      // Sort order shifts server-side (appended at the end of the destination) --
      // refetch so the position numbers reflect where it actually landed.
      loadProducts(auth.token, { silent: true });
    } catch {
      setProducts((prev) => prev.map((p) => (p.id === product.id ? { ...p, sectionId: previousSectionId } : p)));
      setError(`No se pudo mover "${product.name}".`);
    }
  }

  function toggleBulkAssignMode() {
    setIsBulkAssigningSection((prev) => !prev);
    setBulkAssignSelectedIds(new Set());
    setBulkAssignTargetId("");
  }

  function toggleBulkAssignSelected(productId: string) {
    setBulkAssignSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(productId)) next.delete(productId);
      else next.add(productId);
      return next;
    });
  }

  async function handleApplyBulkAssign() {
    if (!auth || bulkAssignSelectedIds.size === 0) return;
    setIsApplyingBulkAssign(true);
    setError(null);

    const targetSectionId = bulkAssignTargetId || null;
    const targetIds = Array.from(bulkAssignSelectedIds);
    const failed: string[] = [];

    await runWithConcurrency(targetIds, 4, async (productId) => {
      try {
        await assignProductSection(auth!.token, productId, targetSectionId);
      } catch {
        failed.push(products.find((p) => p.id === productId)?.name ?? productId);
      }
    });
    await loadProducts(auth.token, { silent: true });

    setIsApplyingBulkAssign(false);
    setIsBulkAssigningSection(false);
    setBulkAssignSelectedIds(new Set());
    setBulkAssignTargetId("");

    if (failed.length > 0) {
      setError(`${failed.length} producto(s) no se pudieron asociar: ${failed.join(", ")}`);
    }
  }

  async function handleGenerateCatalog() {
    if (!auth) return;
    const selected = products.filter((p) => p.hasStock && p.hasSheet);
    if (selected.length === 0) return;

    setIsGenerating(true);
    setError(null);
    setCatalogResult(null);
    try {
      const result = await generateCatalog(auth.token, selected.map((p) => p.id));
      setCatalogResult(result);
      // Un catálogo nuevo puede haber podado los más viejos (límite de 10) del lado del
      // backend -- refrescar el historial si está abierto para que quede en sync.
      if (isHistoryOpen) loadCatalogHistory(auth.token);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudo generar el catálogo.");
    } finally {
      setIsGenerating(false);
    }
  }

  function toggleCatalogHistory() {
    if (!auth) return;
    const next = !isHistoryOpen;
    setIsHistoryOpen(next);
    if (next) loadCatalogHistory(auth.token);
  }

  if (authLoading || !auth) {
    return null;
  }

  const selectableCount = products.filter((p) => p.hasStock && p.hasSheet).length;
  const checkedCount = products.filter((p) => p.hasStock).length;
  const filteredProducts = products.filter((p) =>
    p.name.toLowerCase().includes(search.trim().toLowerCase())
  );

  const searchQuery = search.trim().toLowerCase();
  const matchesSearch = (name: string) => name.toLowerCase().includes(searchQuery);

  const allTopLevelRows = buildTopLevelRows();
  const filteredTopLevelRows = !searchQuery
    ? allTopLevelRows
    : allTopLevelRows.filter((row) => {
        if (row.type === "product") return matchesSearch(row.product.name);
        return matchesSearch(row.section.name) || sectionMembers(row.section.id).some((m) => matchesSearch(m.name));
      });

  // Same "if the carátula's own name matches, show all its members; otherwise only the
  // matching ones" rule used to decide whether the carátula shows up at all above -- applied
  // here to what's actually rendered underneath it, instead of always showing every member.
  function visibleSectionMembers(sectionId: string, sectionName: string) {
    const members = sectionMembers(sectionId);
    if (!searchQuery || matchesSearch(sectionName)) return members;
    return members.filter((m) => matchesSearch(m.name));
  }

  function renderProductRow(product: Product, sectionId: string | null) {
    const isDragged = sectionId ? draggedSectionMemberId === product.id : draggedTopLevelId === product.id;
    const positionMax = sectionId ? sectionMembers(sectionId).length : allTopLevelRows.length;
    const positionValue = sectionId
      ? sectionMembers(sectionId).findIndex((p) => p.id === product.id) + 1
      : allTopLevelRows.findIndex((r) => r.id === product.id) + 1;

    return (
      <li
        key={product.id}
        onDragOver={(e) => e.preventDefault()}
        onDrop={() =>
          sectionId ? handleSectionMemberDrop(sectionId, product.id) : handleTopLevelDrop(product.id)
        }
        className={`flex items-center justify-between gap-3 px-4 py-3 ${isDragged ? "opacity-40" : ""}`}
      >
        <span
          draggable
          onDragStart={() =>
            sectionId ? setDraggedSectionMemberId(product.id) : setDraggedTopLevelId(product.id)
          }
          onDragEnd={() => (sectionId ? setDraggedSectionMemberId(null) : setDraggedTopLevelId(null))}
          title="Arrastrar para reordenar"
          className="cursor-grab select-none text-zinc-400 dark:text-zinc-600"
        >
          ⠿
        </span>
        <input
          key={`${product.id}-${positionValue}`}
          type="number"
          min={1}
          max={positionMax}
          defaultValue={positionValue}
          onBlur={(e) =>
            sectionId
              ? handleSectionMemberMoveToPosition(sectionId, product.id, e.target.value)
              : handleTopLevelMoveToPosition(product.id, e.target.value)
          }
          onKeyDown={(e) => {
            if (e.key === "Enter") e.currentTarget.blur();
          }}
          title="Posición en el orden"
          className="w-12 rounded border border-zinc-300 px-1 py-0.5 text-center text-xs text-zinc-700 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-300"
        />
        <label className="flex flex-1 items-center gap-3">
          <input
            type="checkbox"
            checked={isBulkAssigningSection ? bulkAssignSelectedIds.has(product.id) : product.hasStock}
            onChange={() =>
              isBulkAssigningSection ? toggleBulkAssignSelected(product.id) : handleToggleStock(product)
            }
            title={isBulkAssigningSection ? "Seleccionar para asociar a carátula" : "Tiene stock"}
            className="h-4 w-4"
            style={{ accentColor: isBulkAssigningSection ? "#e4c98a" : "#c9a86a" }}
          />
          <span className="text-zinc-900 dark:text-zinc-50">{product.name}</span>
        </label>
        <select
          value={product.sectionId ?? ""}
          onChange={(e) => handleAssignSection(product, e.target.value || null)}
          title="Carátula"
          className="rounded border border-zinc-300 bg-white px-1 py-1 text-xs text-zinc-700 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-300"
        >
          <option value="">Sin carátula</option>
          {sections.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name}
            </option>
          ))}
        </select>

        {confirmingDeleteId === product.id ? (
          <div className="flex items-center gap-2 text-xs">
            <span className="text-zinc-600 dark:text-zinc-400">¿Borrar?</span>
            <button
              onClick={() => handleDeleteProduct(product)}
              disabled={isDeleting}
              className="rounded bg-red-600 px-2 py-1 font-medium text-white hover:bg-red-500 disabled:opacity-50"
            >
              Sí
            </button>
            <button
              onClick={() => setConfirmingDeleteId(null)}
              disabled={isDeleting}
              className="rounded bg-zinc-200 px-2 py-1 font-medium text-zinc-800 hover:bg-zinc-300 dark:bg-zinc-700 dark:text-zinc-100"
            >
              Cancelar
            </button>
          </div>
        ) : (
          <div className="flex items-center gap-3">
            {product.hasSheet ? (
              <span className="text-xs text-emerald-600 dark:text-emerald-400">Ficha cargada</span>
            ) : (
              <label className="cursor-pointer text-xs text-amber-600 underline dark:text-amber-400">
                Subir ficha
                <input
                  type="file"
                  accept="application/pdf"
                  className="hidden"
                  onChange={(e) => {
                    const file = e.target.files?.[0];
                    if (file) handleUploadSheet(product, file);
                  }}
                />
              </label>
            )}
            <button
              onClick={() => setConfirmingDeleteId(product.id)}
              className="text-xs text-red-600 underline hover:text-red-500 dark:text-red-400"
            >
              Borrar
            </button>
          </div>
        )}
      </li>
    );
  }

  return (
    <div
      className="w-full flex-1 px-4 py-10"
      style={{
        // Same wood-niche photo used on the login/catalog screens, zoomed into just the
        // grained panel (no bookshelves) and tinted browner with a gradient on top -- gives
        // real wood-grain texture instead of a flat color, without needing a new asset.
        backgroundImage:
          "radial-gradient(ellipse 80% 60% at 50% 0%, rgba(240,220,174,0.55) 0%, rgba(160,110,60,0.55) 45%, rgba(90,55,25,0.75) 100%), url('/login-bg.jpg')",
        backgroundSize: "100% 100%, 240%",
        backgroundPosition: "center, center 38%",
        backgroundRepeat: "no-repeat, no-repeat",
        backgroundAttachment: "fixed, fixed",
      }}
    >
      <div className="fixed top-4 left-4 z-10">
        <Image
          src="/vidriera-logo.png"
          alt="Vidriera"
          width={1000}
          height={245}
          className="h-11 w-auto"
        />
      </div>
      <div className="mx-auto w-full max-w-3xl">
        <header className="mb-6">
        {logoUrl && (
          // eslint-disable-next-line @next/next/no-img-element -- imagen autenticada vía blob URL, no un asset estático de Next
          <img
            src={logoUrl}
            alt={`Banner de ${auth.companyName}`}
            className="mb-4 h-40 w-full rounded-xl bg-white object-contain opacity-85 shadow-lg"
          />
        )}
        <div className="flex items-center justify-between rounded-xl border border-white/10 bg-black/25 px-5 py-4 backdrop-blur-sm">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-[#c9a86a] text-sm font-semibold text-zinc-900">
              {auth.name.charAt(0).toUpperCase()}
            </div>
            <div className="flex flex-col leading-tight">
              <span className="text-sm font-semibold text-zinc-50">{auth.name}</span>
              <span className="text-xs text-zinc-300">{auth.companyName}</span>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <label className="cursor-pointer rounded-md border border-white/15 px-3 py-1.5 text-xs font-medium text-[#e4c98a] transition-colors hover:bg-white/10">
              {isUploadingLogo ? "Subiendo..." : logoUrl ? "Cambiar banner" : "Subir banner"}
              <input
                type="file"
                accept="image/*"
                className="hidden"
                disabled={isUploadingLogo}
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) handleLogoChange(file);
                }}
              />
            </label>
            <button
              onClick={logout}
              className="rounded-md border border-white/15 px-3 py-1.5 text-xs font-medium text-zinc-100 transition-colors hover:border-red-400/40 hover:bg-red-500/10 hover:text-red-300"
            >
              Salir
            </button>
          </div>
        </div>
      </header>

      <form
        onSubmit={handleCreateProduct}
        className="mb-3 flex flex-wrap items-center gap-3 rounded-xl border border-black/10 bg-[#ecdcc0] px-4 py-3 shadow-lg dark:border-white/10 dark:bg-zinc-900"
      >
        <span className="w-40 shrink-0 text-xs font-medium whitespace-nowrap text-zinc-600 dark:text-zinc-400">
          Ficha/s PDF <span className="text-zinc-400 dark:text-zinc-500">(máx. {MAX_FILE_SIZE_LABEL} c/u)</span>
        </span>
        <label className="flex cursor-pointer items-center gap-2 rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-700 hover:bg-zinc-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800">
          <span className="w-28 shrink-0 rounded bg-[#c9a86a] px-2 py-0.5 text-center text-xs font-medium whitespace-nowrap text-zinc-900">
            Elegir archivos
          </span>
          <span className="max-w-[160px] truncate">
            {newFiles.length === 0
              ? "Ningún archivo elegido"
              : newFiles.length === 1
                ? newFiles[0].name
                : `${newFiles.length} archivos elegidos`}
          </span>
          <input
            ref={newFileInputRef}
            type="file"
            accept="application/pdf"
            multiple
            required
            onChange={(e) => setNewFiles(Array.from(e.target.files ?? []))}
            className="hidden"
          />
        </label>
        <input
          type="text"
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          disabled={newFiles.length > 1}
          placeholder="Nombre (opcional)"
          className="w-48 rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-900 outline-none focus:border-zinc-500 disabled:opacity-50 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
        />
        <button
          type="submit"
          title={newFiles.length > 1 ? `Cargar ${newFiles.length} productos` : "Nuevo producto"}
          disabled={newFiles.length === 0 || isCreating}
          className="flex items-center justify-center rounded-md bg-[#8a5a35] px-3 py-1.5 text-white transition-colors hover:bg-[#a06b41] disabled:opacity-50"
        >
          {isCreating ? (
            <span className="h-4 w-4 animate-spin rounded-full border-2 border-white/30 border-t-white" />
          ) : (
            <span className="text-lg leading-none">+</span>
          )}
        </button>

        {uploadProgress && (
          <div className="h-1.5 w-full overflow-hidden rounded-full bg-black/10">
            <div
              className="h-full rounded-full bg-[#c9a86a] transition-all"
              style={{
                width: `${(uploadProgress.done / uploadProgress.total) * 100}%`,
              }}
            />
          </div>
        )}
      </form>

      <form
        onSubmit={handleCreateSection}
        className="mb-6 flex flex-wrap items-center gap-3 rounded-xl border border-black/10 bg-[#ecdcc0] px-4 py-3 shadow-lg dark:border-white/10 dark:bg-zinc-900"
      >
        <span className="w-40 shrink-0 text-xs font-medium whitespace-nowrap text-zinc-600 dark:text-zinc-400">
          PDF de carátula <span className="text-zinc-400 dark:text-zinc-500">(máx. {MAX_FILE_SIZE_LABEL})</span>
        </span>
        <label className="flex cursor-pointer items-center gap-2 rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-700 hover:bg-zinc-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800">
          <span className="w-28 shrink-0 rounded bg-[#c9a86a] px-2 py-0.5 text-center text-xs font-medium whitespace-nowrap text-zinc-900">
            Elegir archivo
          </span>
          <span className="max-w-[160px] truncate">
            {newSectionFile ? newSectionFile.name : "Ningún archivo elegido"}
          </span>
          <input
            ref={newSectionFileInputRef}
            type="file"
            accept="application/pdf"
            required
            onChange={(e) => setNewSectionFile(e.target.files?.[0] ?? null)}
            className="hidden"
          />
        </label>
        <input
          type="text"
          value={newSectionName}
          onChange={(e) => setNewSectionName(e.target.value)}
          placeholder="Nombre (opcional)"
          className="w-48 rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-900 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
        />
        <button
          type="submit"
          title="Nueva carátula"
          disabled={!newSectionFile || isCreatingSection}
          className="flex items-center justify-center rounded-md bg-[#8a5a35] px-3 py-1.5 text-white transition-colors hover:bg-[#a06b41] disabled:opacity-50"
        >
          {isCreatingSection ? (
            <span className="h-4 w-4 animate-spin rounded-full border-2 border-white/30 border-t-white" />
          ) : (
            <span className="text-lg leading-none">+</span>
          )}
        </button>
      </form>

      {error && (
        <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-950 dark:text-red-300">
          {error}
        </p>
      )}

      {(products.length > 0 || sections.length > 0) && (
        <div className="mb-3 flex flex-wrap items-center gap-2">
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Buscar por nombre..."
            className="min-w-0 flex-1 rounded-md border border-white/20 bg-black/20 px-3 py-2 text-sm text-white placeholder:text-zinc-300 outline-none focus:border-[#e4c98a]"
          />
          <button
            type="button"
            onClick={() =>
              isBulkAssigningSection
                ? setBulkAssignSelectedIds((prev) => {
                    const next = new Set(prev);
                    filteredProducts.forEach((p) => next.add(p.id));
                    return next;
                  })
                : handleBulkStockToggle(true)
            }
            className="rounded-md border border-white/20 px-3 py-2 text-xs font-medium whitespace-nowrap text-zinc-100 transition-colors hover:bg-white/10"
          >
            Marcar {search.trim() ? "filtrados" : "todos"}
          </button>
          <button
            type="button"
            onClick={() =>
              isBulkAssigningSection
                ? setBulkAssignSelectedIds((prev) => {
                    const next = new Set(prev);
                    filteredProducts.forEach((p) => next.delete(p.id));
                    return next;
                  })
                : handleBulkStockToggle(false)
            }
            className="rounded-md border border-white/20 px-3 py-2 text-xs font-medium whitespace-nowrap text-zinc-100 transition-colors hover:bg-white/10"
          >
            Desmarcar {search.trim() ? "filtrados" : "todos"}
          </button>
          {!isBulkAssigningSection && (
            <button
              type="button"
              onClick={requestBulkDeleteSelected}
              disabled={checkedCount === 0}
              className="rounded-md border border-red-400/30 px-3 py-2 text-xs font-medium whitespace-nowrap text-red-300 transition-colors hover:bg-red-500/10 disabled:opacity-40"
            >
              Borrar seleccionados ({checkedCount})
            </button>
          )}
          {!isBulkAssigningSection && (
            <button
              type="button"
              onClick={requestBulkDeleteAll}
              className="rounded-md border border-red-400/30 px-3 py-2 text-xs font-medium whitespace-nowrap text-red-300 transition-colors hover:bg-red-500/10"
            >
              Borrar {search.trim() ? "filtrados" : "todos"}
            </button>
          )}
          {sections.length > 0 && (
            <button
              type="button"
              onClick={toggleBulkAssignMode}
              disabled={isApplyingBulkAssign}
              className={`rounded-md border px-3 py-2 text-xs font-medium whitespace-nowrap transition-colors disabled:opacity-40 ${
                isBulkAssigningSection
                  ? "border-[#e4c98a] bg-[#e4c98a]/20 text-[#f0dca8]"
                  : "border-white/20 text-zinc-100 hover:bg-white/10"
              }`}
            >
              {isBulkAssigningSection ? "Cancelar asociación" : "Asociar a carátula"}
            </button>
          )}
        </div>
      )}

      {isBulkAssigningSection && (
        <div className="mb-3 flex flex-wrap items-center justify-between gap-3 rounded-md border border-[#e4c98a]/40 bg-[#e4c98a]/10 px-4 py-3 text-sm text-[#f0dca8]">
          <span>
            Tildá los productos de la lista para asociarlos ({bulkAssignSelectedIds.size} seleccionado
            {bulkAssignSelectedIds.size === 1 ? "" : "s"}).
          </span>
          <div className="flex items-center gap-2">
            <select
              value={bulkAssignTargetId}
              onChange={(e) => setBulkAssignTargetId(e.target.value)}
              disabled={isApplyingBulkAssign}
              className="rounded border border-white/20 bg-black/20 px-2 py-1.5 text-xs text-white disabled:opacity-50"
            >
              <option value="">Sin carátula</option>
              {sections.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </select>
            <button
              onClick={handleApplyBulkAssign}
              disabled={bulkAssignSelectedIds.size === 0 || isApplyingBulkAssign}
              className="flex items-center gap-2 rounded bg-[#c9a86a] px-3 py-1.5 text-xs font-medium text-zinc-900 hover:bg-[#d4b57a] disabled:opacity-50"
            >
              {isApplyingBulkAssign && (
                <span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-zinc-900/25 border-t-zinc-900" />
              )}
              {isApplyingBulkAssign ? "Aplicando..." : `Aplicar (${bulkAssignSelectedIds.size})`}
            </button>
          </div>
        </div>
      )}

      {pendingBulkDelete && (
        <div className="mb-3 flex flex-wrap items-center justify-between gap-3 rounded-md border border-red-400/30 bg-red-500/10 px-4 py-3 text-sm text-red-200">
          <span>¿Borrar {pendingBulkDelete.label}? Esta acción no se puede deshacer.</span>
          <div className="flex gap-2">
            <button
              onClick={handleConfirmBulkDelete}
              disabled={isBulkDeleting}
              className="rounded bg-red-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-red-500 disabled:opacity-50"
            >
              {isBulkDeleting ? "Borrando..." : "Sí, borrar"}
            </button>
            <button
              onClick={() => setPendingBulkDelete(null)}
              disabled={isBulkDeleting}
              className="rounded bg-zinc-700 px-3 py-1.5 text-xs font-medium text-white hover:bg-zinc-600 disabled:opacity-50"
            >
              Cancelar
            </button>
          </div>
        </div>
      )}

      {isLoading ? (
        <p className="text-zinc-200">Cargando productos...</p>
      ) : products.length === 0 && sections.length === 0 ? (
        <p className="text-zinc-200">Todavía no hay productos ni carátulas cargadas.</p>
      ) : filteredTopLevelRows.length === 0 ? (
        <p className="mb-8 text-zinc-200">Ningún producto coincide con la búsqueda.</p>
      ) : (
        <ul className="mb-6 max-h-[520px] divide-y divide-zinc-200 overflow-y-auto rounded-xl border border-black/10 bg-[#ecdcc0] shadow-lg dark:divide-zinc-800 dark:border-white/10 dark:bg-zinc-900">
          {filteredTopLevelRows.map((row) =>
            row.type === "section" ? (
              <li key={row.id} className="bg-black/5 dark:bg-white/5">
                <div
                  onDragOver={(e) => e.preventDefault()}
                  onDrop={() => handleTopLevelDrop(row.id)}
                  className={`flex items-center justify-between gap-3 px-4 py-3 ${
                    draggedTopLevelId === row.id ? "opacity-40" : ""
                  }`}
                >
                  <span
                    draggable
                    onDragStart={() => setDraggedTopLevelId(row.id)}
                    onDragEnd={() => setDraggedTopLevelId(null)}
                    title="Arrastrar para reordenar"
                    className="cursor-grab select-none text-zinc-400 dark:text-zinc-600"
                  >
                    ⠿
                  </span>
                  <input
                    key={`${row.id}-${allTopLevelRows.findIndex((r) => r.id === row.id) + 1}`}
                    type="number"
                    min={1}
                    max={allTopLevelRows.length}
                    defaultValue={allTopLevelRows.findIndex((r) => r.id === row.id) + 1}
                    onBlur={(e) => handleTopLevelMoveToPosition(row.id, e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === "Enter") e.currentTarget.blur();
                    }}
                    title="Posición en el orden"
                    className="w-12 rounded border border-zinc-300 px-1 py-0.5 text-center text-xs text-zinc-700 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-300"
                  />
                  <input
                    type="checkbox"
                    checked={sectionCheckboxChecked(row.section.id)}
                    onChange={() => handleToggleSectionCheckbox(row.section)}
                    disabled={sectionMembers(row.section.id).length === 0}
                    title={
                      isBulkAssigningSection
                        ? "Seleccionar todos los productos de esta carátula"
                        : "Marcar/desmarcar stock de todos los productos de esta carátula"
                    }
                    className="h-4 w-4 disabled:opacity-30"
                    style={{ accentColor: isBulkAssigningSection ? "#e4c98a" : "#c9a86a" }}
                  />
                  <span className="flex-1 font-semibold text-zinc-900 dark:text-zinc-50">
                    📑 {row.section.name}
                  </span>
                  {confirmingDeleteSectionId === row.id ? (
                    <div className="flex items-center gap-2 text-xs">
                      <span className="text-zinc-600 dark:text-zinc-400">¿Borrar carátula?</span>
                      <button
                        onClick={() => handleDeleteSection(row.section)}
                        disabled={isDeletingSection}
                        className="rounded bg-red-600 px-2 py-1 font-medium text-white hover:bg-red-500 disabled:opacity-50"
                      >
                        Sí
                      </button>
                      <button
                        onClick={() => setConfirmingDeleteSectionId(null)}
                        disabled={isDeletingSection}
                        className="rounded bg-zinc-200 px-2 py-1 font-medium text-zinc-800 hover:bg-zinc-300 dark:bg-zinc-700 dark:text-zinc-100"
                      >
                        Cancelar
                      </button>
                    </div>
                  ) : (
                    <button
                      onClick={() => setConfirmingDeleteSectionId(row.id)}
                      className="text-xs text-red-600 underline hover:text-red-500 dark:text-red-400"
                    >
                      Borrar carátula
                    </button>
                  )}
                </div>
                {visibleSectionMembers(row.section.id, row.section.name).length > 0 && (
                  <ul className="divide-y divide-black/5 border-t border-black/10 pl-8 dark:divide-white/5 dark:border-white/10">
                    {visibleSectionMembers(row.section.id, row.section.name).map((product) =>
                      renderProductRow(product, row.section.id)
                    )}
                  </ul>
                )}
              </li>
            ) : (
              renderProductRow(row.product, null)
            )
          )}
        </ul>
      )}

      <div className="rounded-xl border border-black/10 bg-[#ecdcc0] p-5 shadow-lg dark:border-white/10 dark:bg-zinc-900">
        <div className="flex flex-wrap items-center gap-3">
          <button
            onClick={handleGenerateCatalog}
            disabled={selectableCount === 0 || isGenerating}
            className="flex items-center gap-2 rounded-md bg-[#c9a86a] px-4 py-2 text-sm font-medium text-zinc-900 transition-colors hover:bg-[#d4b57a] disabled:opacity-50"
          >
            {isGenerating && (
              <span className="h-4 w-4 animate-spin rounded-full border-2 border-zinc-900/25 border-t-zinc-900" />
            )}
            {isGenerating
              ? "Generando catálogo..."
              : `Generar catálogo (${selectableCount} con stock y ficha)`}
          </button>
          <button
            onClick={toggleCatalogHistory}
            className={`rounded-md border px-3 py-2 text-sm font-medium transition-colors ${
              isHistoryOpen
                ? "border-[#8a5a35] bg-[#8a5a35]/10 text-[#8a5a35] dark:text-[#c9a86a]"
                : "border-zinc-300 text-zinc-700 hover:bg-zinc-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800"
            }`}
          >
            {isHistoryOpen ? "Ocultar historial" : "Historial de catálogos"}
          </button>
        </div>

        {isGenerating && (
          <p className="mt-2 text-xs text-zinc-600 dark:text-zinc-400">
            Puede tardar un momento si el catálogo tiene muchos productos.
          </p>
        )}

        {isHistoryOpen && (
          <div className="mt-4 border-t border-black/10 pt-4 dark:border-white/10">
            {isLoadingHistory ? (
              <p className="text-sm text-zinc-600 dark:text-zinc-400">Cargando historial...</p>
            ) : catalogHistory.length === 0 ? (
              <p className="text-sm text-zinc-600 dark:text-zinc-400">Todavía no generaste ningún catálogo.</p>
            ) : (
              <>
                <p className="mb-2 text-xs text-zinc-500 dark:text-zinc-500">
                  Se guardan los últimos {catalogHistory.length < 10 ? catalogHistory.length : 10} catálogos generados; al pasar ese límite, el más viejo se borra.
                </p>
                <ul className="divide-y divide-black/10 rounded-md border border-black/10 dark:divide-white/10 dark:border-white/10">
                  {catalogHistory.map((item) => (
                    <li key={item.id} className="flex flex-wrap items-center justify-between gap-2 px-3 py-2 text-sm">
                      <div className="flex flex-col">
                        <span className="text-zinc-900 dark:text-zinc-50">
                          {new Date(item.generatedAt).toLocaleString()}
                        </span>
                        <span className="text-xs text-zinc-500">
                          {item.productCount} producto{item.productCount === 1 ? "" : "s"}
                          {item.expiresAt && ` · vence ${new Date(item.expiresAt).toLocaleDateString()}`}
                        </span>
                      </div>
                      <div className="flex items-center gap-2">
                        {item.isExpired ? (
                          <span className="rounded-full bg-zinc-200 px-2 py-0.5 text-xs text-zinc-600 dark:bg-zinc-700 dark:text-zinc-300">
                            Expirado
                          </span>
                        ) : (
                          <a
                            href={item.viewUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-blue-600 underline dark:text-blue-400"
                          >
                            Abrir
                          </a>
                        )}
                      </div>
                    </li>
                  ))}
                </ul>
              </>
            )}
          </div>
        )}

        {catalogResult && (
          <div className="mt-4 text-sm text-zinc-700 dark:text-zinc-300">
            <p>
              Link:{" "}
              <a
                href={catalogResult.url}
                target="_blank"
                rel="noopener noreferrer"
                className="text-blue-600 underline dark:text-blue-400"
              >
                {catalogResult.url}
              </a>
            </p>
            {catalogResult.expiresAt && (
              <p className="text-zinc-500">
                Expira: {new Date(catalogResult.expiresAt).toLocaleString()}
              </p>
            )}
          </div>
        )}
      </div>
      </div>
    </div>
  );
}
