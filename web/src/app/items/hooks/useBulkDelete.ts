import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { runWithConcurrency } from "@/lib/concurrency";
import { deleteItem, type Item } from "@/lib/api";
import { bulkDeleteFailed } from "@/lib/messages";

export function useBulkDelete({
  auth,
  setItems,
  setError,
}: {
  auth: AuthState | null;
  setItems: React.Dispatch<React.SetStateAction<Item[]>>;
  setError: (message: string | null) => void;
}) {
  const [pendingBulkDelete, setPendingBulkDelete] = useState<{
    label: string;
    targets: Item[];
    onComplete?: () => void;
  } | null>(null);
  const [isBulkDeleting, setIsBulkDeleting] = useState(false);

  function requestBulkDelete(targets: Item[], label: string, onComplete?: () => void) {
    if (targets.length === 0) return;
    setPendingBulkDelete({ label, targets, onComplete });
  }

  async function handleConfirmBulkDelete() {
    if (!auth || !pendingBulkDelete) return;
    setIsBulkDeleting(true);
    setError(null);
    const { targets, onComplete } = pendingBulkDelete;
    const failed: string[] = [];

    await runWithConcurrency(targets, 4, async (item) => {
      try {
        await deleteItem(auth.token, item.id);
        setItems((prev) => prev.filter((p) => p.id !== item.id));
      } catch {
        failed.push(item.name);
      }
    });

    setIsBulkDeleting(false);
    setPendingBulkDelete(null);
    onComplete?.();
    if (failed.length > 0) {
      setError(bulkDeleteFailed(failed));
    }
  }

  return {
    pendingBulkDelete,
    setPendingBulkDelete,
    isBulkDeleting,
    requestBulkDelete,
    handleConfirmBulkDelete,
  };
}
