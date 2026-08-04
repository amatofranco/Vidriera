import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { generateCatalog, type GenerateCatalogResult, type Product } from "@/lib/api";
import { Messages, apiErrorMessage } from "@/lib/messages";

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
      if (isHistoryOpen) loadCatalogHistory(auth.token);
    } catch (err) {
      setError(apiErrorMessage(err, Messages.catalogGenerationFailed));
    } finally {
      setIsGenerating(false);
    }
  }

  return { isGenerating, catalogResult, selectableCount, handleGenerateCatalog };
}
