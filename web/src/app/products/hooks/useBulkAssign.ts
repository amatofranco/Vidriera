import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { runWithConcurrency } from "@/lib/concurrency";
import { assignProductSection, type Product } from "@/lib/api";

export function useBulkAssign({
  auth,
  products,
  loadProducts,
  setError,
}: {
  auth: AuthState | null;
  products: Product[];
  loadProducts: (token: string, options?: { silent?: boolean }) => Promise<void>;
  setError: (message: string | null) => void;
}) {
  const [isBulkAssigningSection, setIsBulkAssigningSection] = useState(false);
  const [bulkAssignSelectedIds, setBulkAssignSelectedIds] = useState<Set<string>>(new Set());
  const [bulkAssignTargetId, setBulkAssignTargetId] = useState("");
  const [isApplyingBulkAssign, setIsApplyingBulkAssign] = useState(false);

  function toggleBulkAssignMode() {
    setIsBulkAssigningSection((prev) => !prev);
    setBulkAssignSelectedIds(new Set());
    setBulkAssignTargetId("");
  }

  function toggleBulkAssignSelected(productId: string) {
    setBulkAssignSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(productId)) next.delete(productId);
      else next.add(productId);
      return next;
    });
  }

  function selectForBulkAssign(ids: string[], selected: boolean) {
    setBulkAssignSelectedIds((prev) => {
      const next = new Set(prev);
      ids.forEach((id) => (selected ? next.add(id) : next.delete(id)));
      return next;
    });
  }

  async function handleApplyBulkAssign() {
    if (!auth || bulkAssignSelectedIds.size === 0) return;
    setIsApplyingBulkAssign(true);
    setError(null);

    const targetSectionId = bulkAssignTargetId || null;
    const targetIds = Array.from(bulkAssignSelectedIds);
    const failed: string[] = [];

    await runWithConcurrency(targetIds, 4, async (productId) => {
      try {
        await assignProductSection(auth.token, productId, targetSectionId);
      } catch {
        failed.push(products.find((p) => p.id === productId)?.name ?? productId);
      }
    });
    await loadProducts(auth.token, { silent: true });

    setIsApplyingBulkAssign(false);
    setIsBulkAssigningSection(false);
    setBulkAssignSelectedIds(new Set());
    setBulkAssignTargetId("");

    if (failed.length > 0) {
      setError(`${failed.length} producto(s) no se pudieron asociar: ${failed.join(", ")}`);
    }
  }

  return {
    isBulkAssigningSection,
    bulkAssignSelectedIds,
    bulkAssignTargetId,
    setBulkAssignTargetId,
    isApplyingBulkAssign,
    toggleBulkAssignMode,
    toggleBulkAssignSelected,
    selectForBulkAssign,
    handleApplyBulkAssign,
  };
}
