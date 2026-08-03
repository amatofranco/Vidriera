"use client";

import { useEffect, useRef, useState } from "react";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import {
  ApiError,
  createProduct,
  deleteProduct,
  fetchCompanyLogoUrl,
  generateCatalog,
  getProducts,
  reorderProducts,
  updateStock,
  uploadCompanyLogo,
  uploadSheet,
  type GenerateCatalogResult,
  type Product,
} from "@/lib/api";

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

  const [confirmingDeleteId, setConfirmingDeleteId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [pendingBulkDelete, setPendingBulkDelete] = useState<{
    label: string;
    targets: Product[];
  } | null>(null);
  const [isBulkDeleting, setIsBulkDeleting] = useState(false);

  const [search, setSearch] = useState("");
  const [draggedId, setDraggedId] = useState<string | null>(null);

  const [logoUrl, setLogoUrl] = useState<string | null>(null);
  const [isUploadingLogo, setIsUploadingLogo] = useState(false);

  useEffect(() => {
    if (!authLoading && !auth) {
      router.replace("/login");
    }
  }, [auth, authLoading, router]);

  async function loadProducts(token: string) {
    setIsLoading(true);
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
      setIsLoading(false);
    }
  }

  useEffect(() => {
    if (!auth) return;
    // Fetching from the API on mount/auth-change, not derivable during render.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    loadProducts(auth.token);
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
    setUploadProgress({ done: 0, total: newFiles.length });

    // The name override only makes sense for a single file; for a bulk pick every
    // product falls back to its own filename (the backend's existing default).
    const nameOverride = newFiles.length === 1 ? newName.trim() || undefined : undefined;
    const failed: { name: string; message: string }[] = [];
    const files = newFiles;
    const CONCURRENCY = 4;
    let cursor = 0;

    async function worker() {
      while (cursor < files.length) {
        const file = files[cursor++];
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
      }
    }

    await Promise.all(Array.from({ length: Math.min(CONCURRENCY, files.length) }, worker));

    setIsCreating(false);
    setUploadProgress(null);
    setNewFiles([]);
    setNewName("");
    if (newFileInputRef.current) newFileInputRef.current.value = "";

    if (failed.length > 0) {
      setError(
        `${failed.length} de ${files.length} archivo(s) no se pudieron subir: ${failed
          .map((f) => f.name)
          .join(", ")}`
      );
    }
  }

  async function handleUploadSheet(product: Product, file: File) {
    if (!auth) return;
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

  function toggleSelectedForDeletion(id: string) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function requestBulkDeleteSelected() {
    const targets = products.filter((p) => selectedIds.has(p.id));
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
    const CONCURRENCY = 4;
    let cursor = 0;

    async function worker() {
      while (cursor < targets.length) {
        const product = targets[cursor++];
        try {
          await deleteProduct(auth!.token, product.id);
          setProducts((prev) => prev.filter((p) => p.id !== product.id));
          setSelectedIds((prev) => {
            if (!prev.has(product.id)) return prev;
            const next = new Set(prev);
            next.delete(product.id);
            return next;
          });
        } catch {
          failed.push(product.name);
        }
      }
    }

    await Promise.all(Array.from({ length: Math.min(CONCURRENCY, targets.length) }, worker));

    setIsBulkDeleting(false);
    setPendingBulkDelete(null);
    if (failed.length > 0) {
      setError(`No se pudieron borrar: ${failed.join(", ")}`);
    }
  }

  async function persistReorder(newOrder: Product[]) {
    if (!auth) return;
    try {
      await reorderProducts(auth.token, newOrder.map((p) => p.id));
    } catch {
      setError("No se pudo guardar el nuevo orden.");
      loadProducts(auth.token);
    }
  }

  function moveProduct(id: string, toIndex: number) {
    setProducts((prev) => {
      const fromIndex = prev.findIndex((p) => p.id === id);
      if (fromIndex === -1) return prev;
      const clampedToIndex = Math.min(Math.max(toIndex, 0), prev.length - 1);
      if (clampedToIndex === fromIndex) return prev;

      const next = [...prev];
      const [moved] = next.splice(fromIndex, 1);
      next.splice(clampedToIndex, 0, moved);
      persistReorder(next);
      return next;
    });
  }

  function handleDrop(targetId: string) {
    if (!draggedId || draggedId === targetId) {
      setDraggedId(null);
      return;
    }
    const toIndex = products.findIndex((p) => p.id === targetId);
    if (toIndex !== -1) moveProduct(draggedId, toIndex);
    setDraggedId(null);
  }

  function handleMoveToPosition(productId: string, rawValue: string) {
    const position = parseInt(rawValue, 10);
    if (Number.isNaN(position)) return;
    moveProduct(productId, position - 1);
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
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudo generar el catálogo.");
    } finally {
      setIsGenerating(false);
    }
  }

  if (authLoading || !auth) {
    return null;
  }

  const selectableCount = products.filter((p) => p.hasStock && p.hasSheet).length;
  const filteredProducts = products.filter((p) =>
    p.name.toLowerCase().includes(search.trim().toLowerCase())
  );

  return (
    <div
      className="w-full flex-1 px-4 py-10"
      style={{
        background:
          "radial-gradient(ellipse 80% 60% at 50% 0%, #f0dcae 0%, #d4ac78 45%, #9c6f47 100%)",
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
        className="mb-6 flex flex-col gap-4 rounded-xl border border-black/10 bg-[#ecdcc0] p-5 shadow-lg dark:border-white/10 dark:bg-zinc-900"
      >
        <div className="flex flex-wrap items-end gap-4">
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-zinc-700 dark:text-zinc-300">
              Fichas PDF (nuevo/s producto/s)
            </label>
            <label className="flex cursor-pointer items-center gap-2 rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-700 hover:bg-zinc-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800">
              <span className="rounded bg-[#c9a86a] px-2 py-0.5 text-xs font-medium text-zinc-900">
                Elegir archivos
              </span>
              <span className="truncate">
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
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-zinc-700 dark:text-zinc-300">
              Nombre (opcional, solo si elegís un único archivo)
            </label>
            <input
              type="text"
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              disabled={newFiles.length > 1}
              placeholder="Se toma del PDF si lo dejás vacío"
              className="rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-900 outline-none focus:border-zinc-500 disabled:opacity-50 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
            />
          </div>
          <button
            type="submit"
            disabled={newFiles.length === 0 || isCreating}
            className="rounded-md bg-[#8a5a35] px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-[#a06b41] disabled:opacity-50"
          >
            {isCreating
              ? `Subiendo ${uploadProgress?.done ?? 0}/${uploadProgress?.total ?? 0}...`
              : newFiles.length > 1
                ? `+ Cargar ${newFiles.length} productos`
                : "+ Nuevo producto"}
          </button>
        </div>

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

      {error && (
        <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-950 dark:text-red-300">
          {error}
        </p>
      )}

      {products.length > 0 && (
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
            onClick={() => handleBulkStockToggle(true)}
            className="rounded-md border border-white/20 px-3 py-2 text-xs font-medium whitespace-nowrap text-zinc-100 transition-colors hover:bg-white/10"
          >
            Marcar {search.trim() ? "filtrados" : "todos"}
          </button>
          <button
            type="button"
            onClick={() => handleBulkStockToggle(false)}
            className="rounded-md border border-white/20 px-3 py-2 text-xs font-medium whitespace-nowrap text-zinc-100 transition-colors hover:bg-white/10"
          >
            Desmarcar {search.trim() ? "filtrados" : "todos"}
          </button>
          <button
            type="button"
            onClick={requestBulkDeleteSelected}
            disabled={selectedIds.size === 0}
            className="rounded-md border border-red-400/30 px-3 py-2 text-xs font-medium whitespace-nowrap text-red-300 transition-colors hover:bg-red-500/10 disabled:opacity-40"
          >
            Borrar seleccionados ({selectedIds.size})
          </button>
          <button
            type="button"
            onClick={requestBulkDeleteAll}
            className="rounded-md border border-red-400/30 px-3 py-2 text-xs font-medium whitespace-nowrap text-red-300 transition-colors hover:bg-red-500/10"
          >
            Borrar {search.trim() ? "filtrados" : "todos"}
          </button>
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
      ) : products.length === 0 ? (
        <p className="text-zinc-200">Todavía no hay productos cargados.</p>
      ) : filteredProducts.length === 0 ? (
        <p className="mb-8 text-zinc-200">Ningún producto coincide con la búsqueda.</p>
      ) : (
        <ul className="mb-6 max-h-[520px] divide-y divide-zinc-200 overflow-y-auto rounded-xl border border-black/10 bg-[#ecdcc0] shadow-lg dark:divide-zinc-800 dark:border-white/10 dark:bg-zinc-900">
          {filteredProducts.map((product) => (
            <li
              key={product.id}
              onDragOver={(e) => e.preventDefault()}
              onDrop={() => handleDrop(product.id)}
              className={`flex items-center justify-between gap-4 px-4 py-3 ${
                draggedId === product.id ? "opacity-40" : ""
              }`}
            >
              <span
                draggable
                onDragStart={() => setDraggedId(product.id)}
                onDragEnd={() => setDraggedId(null)}
                title="Arrastrar para reordenar"
                className="cursor-grab select-none text-zinc-400 dark:text-zinc-600"
              >
                ⠿
              </span>
              <input
                type="checkbox"
                checked={selectedIds.has(product.id)}
                onChange={() => toggleSelectedForDeletion(product.id)}
                title="Seleccionar para borrar"
                className="h-4 w-4"
                style={{ accentColor: "#dc2626" }}
              />
              <input
                key={`${product.id}-${products.findIndex((p) => p.id === product.id)}`}
                type="number"
                min={1}
                max={products.length}
                defaultValue={products.findIndex((p) => p.id === product.id) + 1}
                onBlur={(e) => handleMoveToPosition(product.id, e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") e.currentTarget.blur();
                }}
                title="Posición en el orden"
                className="w-12 rounded border border-zinc-300 px-1 py-0.5 text-center text-xs text-zinc-700 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-300"
              />
              <label className="flex flex-1 items-center gap-3">
                <input
                  type="checkbox"
                  checked={product.hasStock}
                  onChange={() => handleToggleStock(product)}
                  className="h-4 w-4"
                  style={{ accentColor: "#c9a86a" }}
                />
                <span className="text-zinc-900 dark:text-zinc-50">{product.name}</span>
              </label>

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
                    <span className="text-xs text-emerald-600 dark:text-emerald-400">
                      Ficha cargada
                    </span>
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
          ))}
        </ul>
      )}

      <div className="rounded-xl border border-black/10 bg-[#ecdcc0] p-5 shadow-lg dark:border-white/10 dark:bg-zinc-900">
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

        {isGenerating && (
          <p className="mt-2 text-xs text-zinc-600 dark:text-zinc-400">
            Puede tardar un momento si el catálogo tiene muchos productos.
          </p>
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
