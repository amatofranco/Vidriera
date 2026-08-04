import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { runWithConcurrency } from "@/lib/concurrency";
import { MAX_FILE_SIZE_BYTES, MAX_FILE_SIZE_LABEL, formatFileSize } from "@/lib/file-size";
import { ApiError, createProduct, type Product } from "@/lib/api";

export function useCreateProduct({
  auth,
  setProducts,
  setError,
}: {
  auth: AuthState | null;
  setProducts: React.Dispatch<React.SetStateAction<Product[]>>;
  setError: (message: string | null) => void;
}) {
  const [isCreating, setIsCreating] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<{ done: number; total: number } | null>(
    null
  );

  async function handleCreateProduct(files: File[], name: string) {
    if (!auth || files.length === 0) return;
    setIsCreating(true);
    setError(null);

    // The name override only makes sense for a single file; for a bulk pick every
    // product falls back to its own filename (the backend's existing default).
    const nameOverride = files.length === 1 ? name || undefined : undefined;
    const failed: { name: string; message: string }[] = [];
    const oversized = files.filter((f) => f.size > MAX_FILE_SIZE_BYTES);
    const validFiles = files.filter((f) => f.size <= MAX_FILE_SIZE_BYTES);
    setUploadProgress({ done: 0, total: validFiles.length });

    for (const file of oversized) {
      failed.push({
        name: file.name,
        message: `Pesa ${formatFileSize(file.size)}, supera el máximo de ${MAX_FILE_SIZE_LABEL}.`,
      });
    }

    await runWithConcurrency(validFiles, 4, async (file) => {
      try {
        const created = await createProduct(auth.token, file, nameOverride);
        setProducts((prev) => [...prev, created]);
      } catch (err) {
        failed.push({
          name: file.name,
          message: err instanceof ApiError ? err.message : "Error desconocido",
        });
      }
      setUploadProgress((prev) => (prev ? { ...prev, done: prev.done + 1 } : prev));
    });

    setIsCreating(false);
    setUploadProgress(null);

    if (failed.length > 0) {
      setError(
        `${failed.length} de ${files.length} archivo(s) no se pudieron subir: ${failed
          .map((f) => `${f.name} (${f.message})`)
          .join(", ")}`
      );
    }
  }

  return { isCreating, uploadProgress, handleCreateProduct };
}
