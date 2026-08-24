import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { downloadPriceImportTemplate, importPrices, type ImportPricesResult } from "@/lib/api";
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

  async function handleDownloadTemplate() {
    if (!auth) return;
    try {
      const blob = await downloadPriceImportTemplate(auth.token);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "plantilla-precios.xlsx";
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      setError(apiErrorMessage(err, Messages.templateDownloadFailed));
    }
  }

  return { isImporting, result, handleImport, handleDownloadTemplate };
}
