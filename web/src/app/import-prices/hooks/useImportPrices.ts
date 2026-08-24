import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { importPrices, type ImportPricesResult } from "@/lib/api";
import { Messages, apiErrorMessage } from "@/lib/messages";

export function useImportPrices({
  auth,
  setError,
}: {
  auth: AuthState | null;
  setError: (message: string | null) => void;
}) {
  const [isImporting, setIsImporting] = useState(false);
  const [result, setResult] = useState<ImportPricesResult | null>(null);

  async function handleImport(file: File) {
    if (!auth) return;
    setIsImporting(true);
    setError(null);
    setResult(null);
    try {
      const importResult = await importPrices(auth.token, file);
      setResult(importResult);
    } catch (err) {
      setError(apiErrorMessage(err, Messages.priceImportFailed));
    } finally {
      setIsImporting(false);
    }
  }

  return { isImporting, result, handleImport };
}
