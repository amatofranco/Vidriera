import { useEffect, useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import {
  generateCatalog,
  getCurrentCatalog,
  type CatalogGenerationProgress,
  type GenerateCatalogResult,
  type Product,
} from "@/lib/api";
import { Messages, apiErrorMessage, missingPricesHint } from "@/lib/messages";

const SHOW_PRICES_STORAGE_PREFIX = "vidriera-admin-show-prices-";

function loadShowPrices(companyId: string): boolean {
  try {
    return localStorage.getItem(SHOW_PRICES_STORAGE_PREFIX + companyId) === "true";
  } catch {
    return false;
  }
}

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
  const [showPrices, setShowPrices] = useState(false);

  const selectableCount = products.filter((p) => p.hasStock && p.hasSheet).length;
  const missingPriceCount = products.filter((p) => p.hasStock && p.hasSheet && p.price == null).length;

  useEffect(() => {
    if (!auth) return;
    getCurrentCatalog(auth.token)
      .then((result) => setCatalogResult(result))
      .catch(() => {});
    /* eslint-disable-next-line react-hooks/set-state-in-effect */
    setShowPrices(loadShowPrices(auth.companyId));
  }, [auth]);

  function handleToggleShowPrices() {
    setShowPrices((prev) => {
      const next = !prev;
      if (auth) {
        try {
          localStorage.setItem(SHOW_PRICES_STORAGE_PREFIX + auth.companyId, String(next));
        } catch {
        }
      }
      return next;
    });
  }

  async function handleGenerateCatalog() {
    if (!auth) return;
    if (selectableCount === 0) return;
    if (showPrices && missingPriceCount > 0) {
      setError(missingPricesHint(missingPriceCount));
      return;
    }

    setIsGenerating(true);
    setError(null);
    setCatalogResult(null);
    setGenerationProgress(null);
    try {
      const result = await generateCatalog(auth.token, setGenerationProgress, showPrices);
      setCatalogResult(result);
    } catch (err) {
      setError(apiErrorMessage(err, Messages.catalogGenerationFailed));
    } finally {
      setIsGenerating(false);
      setGenerationProgress(null);
    }
  }

  return {
    isGenerating,
    catalogResult,
    generationProgress,
    selectableCount,
    showPrices,
    handleToggleShowPrices,
    missingPriceCount,
    handleGenerateCatalog,
  };
}
