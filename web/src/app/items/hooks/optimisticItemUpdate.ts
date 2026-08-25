import type { Item } from "@/lib/api";

export async function updateItemFieldOptimistically(
  setItems: React.Dispatch<React.SetStateAction<Item[]>>,
  itemId: string,
  apply: (item: Item) => Item,
  revert: (item: Item) => Item,
  request: () => Promise<void>,
  onError: (err: unknown) => void
) {
  setItems((prev) => prev.map((p) => (p.id === itemId ? apply(p) : p)));
  try {
    await request();
  } catch (err) {
    setItems((prev) => prev.map((p) => (p.id === itemId ? revert(p) : p)));
    onError(err);
  }
}
