import { useEffect, useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import {
  generateCatalog,
  getCurrentCatalog,
  type CatalogGenerationProgress,
  type GenerateCatalogResult,
  type Product,
} from "@/lib/api";
import { Messages, apiErrorMessage } from "@/lib/messages";

export function useCatalogGeneration({
  auth,
  products,
  setError,
}: {
  auth: AuthState | null;
  products: Product[];
  setError: (message: string | null) => void;
}) {
  const [isGenerating, setIsGenerating] = useState(false);
  const [catalogResult, setCatalogResult] = useState<GenerateCatalogResult | null>(null);
  const [generationProgress, setGenerationProgress] = useState<CatalogGenerationProgress | null>(null);

  const selectableCount = products.filter((p) => p.hasStock && p.hasSheet).length;

  useEffect(() => {
    if (!auth) return;
    getCurrentCatalog(auth.token)
      .then((result) => setCatalogResult(result))
      .catch(() => {});
  }, [auth]);

  async function handleGenerateCatalog() {
    if (!auth) return;
    if (selectableCount === 0) return;

    setIsGenerating(true);
    setError(null);
    setCatalogResult(null);
    setGenerationProgress(null);
    try {
      const result = await generateCatalog(auth.token, setGenerationProgress);
      setCatalogResult(result);
    } catch (err) {
      setError(apiErrorMessage(err, Messages.catalogGenerationFailed));
    } finally {
      setIsGenerating(false);
      setGenerationProgress(null);
    }
  }

  return { isGenerating, catalogResult, generationProgress, selectableCount, handleGenerateCatalog };
}
