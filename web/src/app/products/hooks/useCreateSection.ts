import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { runWithConcurrency } from "@/lib/concurrency";
import { MAX_FILE_SIZE_BYTES, MAX_FILE_SIZE_LABEL, formatFileSize } from "@/lib/file-size";
import { ApiError, createSection, type Section } from "@/lib/api";
import { Messages, bulkUploadFailed, fileTooLargeReason } from "@/lib/messages";

export function useCreateSection({
  auth,
  setSections,
  setError,
}: {
  auth: AuthState | null;
  setSections: React.Dispatch<React.SetStateAction<Section[]>>;
  setError: (message: string | null) => void;
}) {
  const [isCreatingSection, setIsCreatingSection] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<{ done: number; total: number } | null>(
    null
  );

  async function handleCreateSection(files: File[], name: string) {
    if (!auth || files.length === 0) return;
    setIsCreatingSection(true);
    setError(null);

    const nameOverride = files.length === 1 ? name || undefined : undefined;
    const failed: { name: string; message: string }[] = [];
    const oversized = files.filter((f) => f.size > MAX_FILE_SIZE_BYTES);
    const validFiles = files.filter((f) => f.size <= MAX_FILE_SIZE_BYTES);
    setUploadProgress({ done: 0, total: validFiles.length });

    for (const file of oversized) {
      failed.push({
        name: file.name,
        message: fileTooLargeReason(formatFileSize(file.size), MAX_FILE_SIZE_LABEL),
      });
    }

    await runWithConcurrency(validFiles, 4, async (file) => {
      try {
        const created = await createSection(auth.token, file, nameOverride);
        setSections((prev) => [...prev, created]);
      } catch (err) {
        failed.push({
          name: file.name,
          message: err instanceof ApiError ? err.message : Messages.unknownError,
        });
      }
      setUploadProgress((prev) => (prev ? { ...prev, done: prev.done + 1 } : prev));
    });

    setIsCreatingSection(false);
    setUploadProgress(null);

    if (failed.length > 0) {
      setError(bulkUploadFailed(files.length, failed));
    }
  }

  return { isCreatingSection, uploadProgress, handleCreateSection };
}
