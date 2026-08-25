import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { runWithConcurrency } from "@/lib/concurrency";
import { assignItemSection, type Item } from "@/lib/api";
import { bulkAssignFailed } from "@/lib/messages";

export function useBulkAssign({
  auth,
  items,
  loadItems,
  setError,
}: {
  auth: AuthState | null;
  items: Item[];
  loadItems: (token: string, options?: { silent?: boolean }) => Promise<void>;
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

  function toggleBulkAssignSelected(itemId: string) {
    setBulkAssignSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(itemId)) next.delete(itemId);
      else next.add(itemId);
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

    await runWithConcurrency(targetIds, 4, async (itemId) => {
      try {
        await assignItemSection(auth.token, itemId, targetSectionId);
      } catch {
        failed.push(items.find((p) => p.id === itemId)?.name ?? itemId);
      }
    });
    await loadItems(auth.token, { silent: true });

    setIsApplyingBulkAssign(false);
    setIsBulkAssigningSection(false);
    setBulkAssignSelectedIds(new Set());
    setBulkAssignTargetId("");

    if (failed.length > 0) {
      setError(bulkAssignFailed(failed));
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
