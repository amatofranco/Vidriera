import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { ApiError, generateCatalog, type GenerateCatalogResult, type Product } from "@/lib/api";

export function useCatalogGeneration({
  auth,
  products,
  setError,
  isHistoryOpen,
  loadCatalogHistory,
}: {
  auth: AuthState | null;
  products: Product[];
  setError: (message: string | null) => void;
  isHistoryOpen: boolean;
  loadCatalogHistory: (token: string) => Promise<void>;
}) {
  const [isGenerating, setIsGenerating] = useState(false);
  const [catalogResult, setCatalogResult] = useState<GenerateCatalogResult | null>(null);

  const selectableCount = products.filter((p) => p.hasStock && p.hasSheet).length;

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

  return { isGenerating, catalogResult, selectableCount, handleGenerateCatalog };
}
