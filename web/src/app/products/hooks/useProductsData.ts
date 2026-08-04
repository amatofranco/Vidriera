import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import type { AuthState } from "@/lib/auth-context";
import {
  ApiError,
  getCatalogHistory,
  getProducts,
  getSections,
  type CatalogHistoryItem,
  type Product,
  type Section,
} from "@/lib/api";

// Owns the three lists the rest of the page reads from (products, sections, catalog
// history) plus their loading/error state -- kept separate from reorder/bulk-assign/etc.
// concerns, which just call the load*/set* functions this returns to refresh after an
// action of their own.
export function useProductsData(auth: AuthState | null, logout: () => void) {
  const router = useRouter();

  const [products, setProducts] = useState<Product[]>([]);
  const [sections, setSections] = useState<Section[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [catalogHistory, setCatalogHistory] = useState<CatalogHistoryItem[]>([]);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);

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

  return {
    products,
    setProducts,
    sections,
    setSections,
    isLoading,
    error,
    setError,
    catalogHistory,
    isLoadingHistory,
    loadProducts,
    loadSections,
    loadCatalogHistory,
  };
}
