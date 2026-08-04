import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { MAX_FILE_SIZE_BYTES, MAX_FILE_SIZE_LABEL, formatFileSize } from "@/lib/file-size";
import { createSection, type Section } from "@/lib/api";
import { Messages, apiErrorMessage, fileTooLarge } from "@/lib/messages";

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

  async function handleCreateSection(files: File[], name: string) {
    const file = files[0];
    if (!auth || !file) return;
    if (file.size > MAX_FILE_SIZE_BYTES) {
      setError(fileTooLarge(file.name, formatFileSize(file.size), MAX_FILE_SIZE_LABEL));
      return;
    }
    setIsCreatingSection(true);
    setError(null);
    try {
      const created = await createSection(auth.token, file, name || undefined);
      setSections((prev) => [...prev, created]);
    } catch (err) {
      setError(apiErrorMessage(err, Messages.sectionCreateFailed));
    } finally {
      setIsCreatingSection(false);
    }
  }

  return { isCreatingSection, handleCreateSection };
}
