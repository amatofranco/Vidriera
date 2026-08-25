import type { AuthState } from "@/lib/auth-context";
import { updateName, type Item } from "@/lib/api";
import { Messages, apiErrorMessage } from "@/lib/messages";
import { updateItemFieldOptimistically } from "./optimisticItemUpdate";

export function useItemName({
  auth,
  setItems,
  setError,
}: {
  auth: AuthState | null;
  setItems: React.Dispatch<React.SetStateAction<Item[]>>;
  setError: (message: string) => void;
}) {
  async function handleUpdateName(item: Item, rawValue: string) {
    if (!auth) return;
    const trimmed = rawValue.trim();
    if (trimmed === "") {
      setError(Messages.nameCannotBeEmpty);
      return;
    }
    if (trimmed === item.name) return;

    await updateItemFieldOptimistically(
      setItems,
      item.id,
      (p) => ({ ...p, name: trimmed }),
      (p) => ({ ...p, name: item.name }),
      () => updateName(auth.token, item.id, trimmed),
      (err) => setError(apiErrorMessage(err, Messages.nameUpdateFailed))
    );
  }

  return { handleUpdateName };
}
