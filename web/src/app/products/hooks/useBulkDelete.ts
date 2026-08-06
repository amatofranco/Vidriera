import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { runWithConcurrency } from "@/lib/concurrency";
import { deleteProduct, type Product } from "@/lib/api";
import { bulkDeleteFailed } from "@/lib/messages";

export function useBulkDelete({
  auth,
  setProducts,
  setError,
}: {
  auth: AuthState | null;
  setProducts: React.Dispatch<React.SetStateAction<Product[]>>;
  setError: (message: string | null) => void;
}) {
  const [pendingBulkDelete, setPendingBulkDelete] = useState<{
    label: string;
    targets: Product[];
    onComplete?: () => void;
  } | null>(null);
  const [isBulkDeleting, setIsBulkDeleting] = useState(false);

  function requestBulkDelete(targets: Product[], label: string, onComplete?: () => void) {
    if (targets.length === 0) return;
    setPendingBulkDelete({ label, targets, onComplete });
  }

  async function handleConfirmBulkDelete() {
    if (!auth || !pendingBulkDelete) return;
    setIsBulkDeleting(true);
    setError(null);
    const { targets, onComplete } = pendingBulkDelete;
    const failed: string[] = [];

    await runWithConcurrency(targets, 4, async (product) => {
      try {
        await deleteProduct(auth.token, product.id);
        setProducts((prev) => prev.filter((p) => p.id !== product.id));
      } catch {
        failed.push(product.name);
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
