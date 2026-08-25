import type { AuthState } from "@/lib/auth-context";
import { updateCode, type Item } from "@/lib/api";
import { Messages } from "@/lib/messages";
import { updateItemFieldOptimistically } from "./optimisticItemUpdate";

export function useItemCode({
  auth,
  setItems,
  setError,
}: {
  auth: AuthState | null;
  setItems: React.Dispatch<React.SetStateAction<Item[]>>;
  setError: (message: string) => void;
}) {
  async function handleUpdateCode(item: Item, rawValue: string) {
    if (!auth) return;
    const trimmed = rawValue.trim();
    const nextValue = trimmed === "" ? null : trimmed;

    await updateItemFieldOptimistically(
      setItems,
      item.id,
      (p) => ({ ...p, code: nextValue }),
      (p) => ({ ...p, code: item.code }),
      () => updateCode(auth.token, item.id, nextValue),
      () => setError(Messages.codeUpdateFailed)
    );
  }

  return { handleUpdateCode };
}
