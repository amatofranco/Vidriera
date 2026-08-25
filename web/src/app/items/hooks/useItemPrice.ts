import type { AuthState } from "@/lib/auth-context";
import { updatePrice, type Item } from "@/lib/api";
import { Messages } from "@/lib/messages";
import { updateItemFieldOptimistically } from "./optimisticItemUpdate";

export function useItemPrice({
  auth,
  setItems,
  setError,
}: {
  auth: AuthState | null;
  setItems: React.Dispatch<React.SetStateAction<Item[]>>;
  setError: (message: string) => void;
}) {
  async function handleUpdatePrice(item: Item, rawValue: string) {
    if (!auth) return;
    const trimmed = rawValue.trim();
    const parsed = trimmed === "" ? null : Number(trimmed);
    const nextValue = parsed !== null && Number.isFinite(parsed) ? parsed : null;

    await updateItemFieldOptimistically(
      setItems,
      item.id,
      (p) => ({ ...p, price: nextValue }),
      (p) => ({ ...p, price: item.price }),
      () => updatePrice(auth.token, item.id, nextValue),
      () => setError(Messages.priceUpdateFailed)
    );
  }

  return { handleUpdatePrice };
}
