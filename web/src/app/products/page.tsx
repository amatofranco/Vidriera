"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import {
  ApiError,
  createProduct,
  deleteProduct,
  generateCatalog,
  getProducts,
  reorderProducts,
  updateStock,
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

  const [newFile, setNewFile] = useState<File | null>(null);
  const [newName, setNewName] = useState("");
  const [isCreating, setIsCreating] = useState(false);
  const newFileInputRef = useRef<HTMLInputElement>(null);

  const [isGenerating, setIsGenerating] = useState(false);
  const [catalogResult, setCatalogResult] = useState<GenerateCatalogResult | null>(null);

  const [confirmingDeleteId, setConfirmingDeleteId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const [search, setSearch] = useState("");
  const [draggedId, setDraggedId] = useState<string | null>(null);

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

  async function handleCreateProduct(e: React.FormEvent) {
    e.preventDefault();
    if (!auth || !newFile) return;
    setIsCreating(true);
    setError(null);
    try {
      const created = await createProduct(auth.token, newFile, newName.trim() || undefined);
      setProducts((prev) => [...prev, created]);
      setNewFile(null);
      setNewName("");
      if (newFileInputRef.current) newFileInputRef.current.value = "";
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudo crear el producto.");
    } finally {
      setIsCreating(false);
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

  async function persistReorder(newOrder: Product[]) {
    if (!auth) return;
    try {
      await reorderProducts(auth.token, newOrder.map((p) => p.id));
    } catch {
      setError("No se pudo guardar el nuevo orden.");
      loadProducts(auth.token);
    }
  }

  function handleDrop(targetId: string) {
    if (!draggedId || draggedId === targetId) {
      setDraggedId(null);
      return;
    }

    setProducts((prev) => {
      const fromIndex = prev.findIndex((p) => p.id === draggedId);
      const toIndex = prev.findIndex((p) => p.id === targetId);
      if (fromIndex === -1 || toIndex === -1) return prev;

      const next = [...prev];
      const [moved] = next.splice(fromIndex, 1);
      next.splice(toIndex, 0, moved);
      persistReorder(next);
      return next;
    });
    setDraggedId(null);
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
    <div className="mx-auto w-full max-w-3xl flex-1 px-4 py-8">
      <header className="mb-8 flex items-center justify-between">
        <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-50">Vidriera</h1>
        <div className="flex items-center gap-4 text-sm text-zinc-600 dark:text-zinc-400">
          <span>{auth.name}</span>
          <button onClick={logout} className="underline hover:text-zinc-900 dark:hover:text-zinc-50">
            Salir
          </button>
        </div>
      </header>

      <form
        onSubmit={handleCreateProduct}
        className="mb-8 flex flex-wrap items-end gap-3 rounded-lg border border-zinc-200 p-4 dark:border-zinc-800"
      >
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-zinc-700 dark:text-zinc-300">
            Ficha PDF (nuevo producto)
          </label>
          <label className="flex cursor-pointer items-center gap-2 rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-700 hover:bg-zinc-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800">
            <span className="rounded bg-zinc-200 px-2 py-0.5 text-xs font-medium text-zinc-800 dark:bg-zinc-700 dark:text-zinc-100">
              Elegir archivo
            </span>
            <span className="truncate">{newFile ? newFile.name : "Ningún archivo elegido"}</span>
            <input
              ref={newFileInputRef}
              type="file"
              accept="application/pdf"
              required
              onChange={(e) => setNewFile(e.target.files?.[0] ?? null)}
              className="hidden"
            />
          </label>
        </div>
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-zinc-700 dark:text-zinc-300">
            Nombre (opcional, por defecto el del archivo)
          </label>
          <input
            type="text"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            placeholder="Se toma del PDF si lo dejás vacío"
            className="rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-900 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
          />
        </div>
        <button
          type="submit"
          disabled={!newFile || isCreating}
          className="rounded-md bg-zinc-900 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-zinc-700 disabled:opacity-50 dark:bg-zinc-50 dark:text-zinc-900 dark:hover:bg-zinc-300"
        >
          {isCreating ? "Subiendo..." : "+ Nuevo producto"}
        </button>
      </form>

      {error && (
        <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-950 dark:text-red-300">
          {error}
        </p>
      )}

      {products.length > 0 && (
        <input
          type="text"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Buscar por nombre..."
          className="mb-3 w-full rounded-md border border-zinc-300 px-3 py-2 text-sm text-zinc-900 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
        />
      )}

      {isLoading ? (
        <p className="text-zinc-500">Cargando productos...</p>
      ) : products.length === 0 ? (
        <p className="text-zinc-500">Todavía no hay productos cargados.</p>
      ) : filteredProducts.length === 0 ? (
        <p className="mb-8 text-zinc-500">Ningún producto coincide con la búsqueda.</p>
      ) : (
        <ul className="mb-8 divide-y divide-zinc-200 rounded-lg border border-zinc-200 dark:divide-zinc-800 dark:border-zinc-800">
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
              <label className="flex flex-1 items-center gap-3">
                <input
                  type="checkbox"
                  checked={product.hasStock}
                  onChange={() => handleToggleStock(product)}
                  className="h-4 w-4"
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

      <div className="rounded-lg border border-zinc-200 p-4 dark:border-zinc-800">
        <button
          onClick={handleGenerateCatalog}
          disabled={selectableCount === 0 || isGenerating}
          className="rounded-md bg-zinc-900 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-zinc-700 disabled:opacity-50 dark:bg-zinc-50 dark:text-zinc-900 dark:hover:bg-zinc-300"
        >
          {isGenerating
            ? "Generando..."
            : `Generar catálogo (${selectableCount} con stock y ficha)`}
        </button>

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
  );
}
