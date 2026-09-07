import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { downloadAvailabilityImportTemplate, importAvailability, type ImportAvailabilityResult } from "@/lib/api";
import { Messages, apiErrorMessage } from "@/lib/messages";

export function useImportAvailability({
  auth,
  setError,
}: {
  auth: AuthState | null;
  setError: (message: string | null) => void;
}) {
  const [isImporting, setIsImporting] = useState(false);
  const [result, setResult] = useState<ImportAvailabilityResult | null>(null);

  async function handleImport(file: File) {
    if (!auth) return;
    setIsImporting(true);
    setError(null);
    setResult(null);
    try {
      const importResult = await importAvailability(auth.token, file);
      setResult(importResult);
    } catch (err) {
      setError(apiErrorMessage(err, Messages.availabilityImportFailed));
    } finally {
      setIsImporting(false);
    }
  }

  async function handleDownloadTemplate() {
    if (!auth) return;
    try {
      const blob = await downloadAvailabilityImportTemplate(auth.token);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "plantilla-disponibilidad.xlsx";
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      setError(apiErrorMessage(err, Messages.templateDownloadFailed));
    }
  }

  return { isImporting, result, handleImport, handleDownloadTemplate };
}
