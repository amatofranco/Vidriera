import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { deleteItem, uploadSheet, type Item } from "@/lib/api";
import { MAX_ITEM_FILE_SIZE_BYTES, MAX_ITEM_FILE_SIZE_LABEL, formatFileSize } from "@/lib/file-size";
import { apiErrorMessage, fileTooLarge, itemDeleteFailed, sheetUploadFailed } from "@/lib/messages";

export function useItemActions({
  auth,
  setItems,
  setError,
}: {
  auth: AuthState | null;
  setItems: React.Dispatch<React.SetStateAction<Item[]>>;
  setError: (message: string | null) => void;
}) {
  const [confirmingDeleteId, setConfirmingDeleteId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  async function handleUploadSheet(item: Item, file: File) {
    if (!auth) return;
    if (file.size > MAX_ITEM_FILE_SIZE_BYTES) {
      setError(fileTooLarge(file.name, formatFileSize(file.size), MAX_ITEM_FILE_SIZE_LABEL));
      return;
    }
    try {
      await uploadSheet(auth.token, item.id, file);
      setItems((prev) =>
        prev.map((p) => (p.id === item.id ? { ...p, hasSheet: true } : p))
      );
    } catch (err) {
      setError(apiErrorMessage(err, sheetUploadFailed(item.name)));
    }
  }

  async function handleDeleteItem(item: Item) {
    if (!auth) return;
    setIsDeleting(true);
    setError(null);
    try {
      await deleteItem(auth.token, item.id);
      setItems((prev) => prev.filter((p) => p.id !== item.id));
    } catch (err) {
      setError(apiErrorMessage(err, itemDeleteFailed(item.name)));
    } finally {
      setIsDeleting(false);
      setConfirmingDeleteId(null);
    }
  }

  return {
    confirmingDeleteId,
    setConfirmingDeleteId,
    isDeleting,
    handleUploadSheet,
    handleDeleteItem,
  };
}
