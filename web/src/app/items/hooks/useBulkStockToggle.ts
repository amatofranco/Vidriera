import { useRef } from "react";
import type { AuthState } from "@/lib/auth-context";
import { runWithConcurrency } from "@/lib/concurrency";
import { updateStock, type Item } from "@/lib/api";
import { Messages, sectionStockUpdateFailed } from "@/lib/messages";
import { updateItemFieldOptimistically } from "./optimisticItemUpdate";

export function useBulkStockToggle({
  auth,
  items,
  setItems,
  setError,
  loadItems,
}: {
  auth: AuthState | null;
  items: Item[];
  setItems: React.Dispatch<React.SetStateAction<Item[]>>;
  setError: (message: string) => void;
  loadItems: (token: string, options?: { silent?: boolean }) => Promise<void>;
}) {
  const pendingWrites = useRef(new Set<Promise<unknown>>());

  function track<T>(promise: Promise<T>): Promise<T> {
    pendingWrites.current.add(promise);
    promise.finally(() => pendingWrites.current.delete(promise));
    return promise;
  }

  async function waitForPendingStockWrites() {
    await Promise.all(Array.from(pendingWrites.current));
  }

  async function handleToggleStock(item: Item) {
    if (!auth) return;
    const nextValue = !item.hasStock;
    await track(
      updateItemFieldOptimistically(
        setItems,
        item.id,
        (p) => ({ ...p, hasStock: nextValue }),
        (p) => ({ ...p, hasStock: !nextValue }),
        () => updateStock(auth.token, item.id, nextValue),
        () => setError(Messages.stockUpdateFailed)
      )
    );
  }

  async function handleBulkStockToggle(nextValue: boolean, search: string) {
    if (!auth) return;
    const query = search.trim().toLowerCase();
    const targets = items.filter(
      (p) => p.name.toLowerCase().includes(query) && p.hasStock !== nextValue
    );
    if (targets.length === 0) return;

    const targetIds = new Set(targets.map((p) => p.id));
    setItems((prev) =>
      prev.map((p) => (targetIds.has(p.id) ? { ...p, hasStock: nextValue } : p))
    );
    try {
      await track(runWithConcurrency(targets, 4, (p) => updateStock(auth.token, p.id, nextValue)));
    } catch {
      setError(Messages.bulkStockUpdateFailed);
      loadItems(auth.token);
    }
  }

  async function handleToggleSectionStock(members: Item[], nextValue: boolean, sectionName: string) {
    if (!auth) return;
    const targets = members.filter((p) => p.hasStock !== nextValue);
    if (targets.length === 0) return;
    const targetIds = new Set(targets.map((p) => p.id));
    setItems((prev) => prev.map((p) => (targetIds.has(p.id) ? { ...p, hasStock: nextValue } : p)));
    try {
      await track(runWithConcurrency(targets, 4, (p) => updateStock(auth.token, p.id, nextValue)));
    } catch {
      setError(sectionStockUpdateFailed(sectionName));
      loadItems(auth.token, { silent: true });
    }
  }

  return { handleToggleStock, handleBulkStockToggle, handleToggleSectionStock, waitForPendingStockWrites };
}
