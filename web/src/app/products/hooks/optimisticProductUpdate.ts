import type { Product } from "@/lib/api";

export async function updateProductFieldOptimistically(
  setProducts: React.Dispatch<React.SetStateAction<Product[]>>,
  productId: string,
  apply: (product: Product) => Product,
  revert: (product: Product) => Product,
  request: () => Promise<void>,
  onError: (err: unknown) => void
) {
  setProducts((prev) => prev.map((p) => (p.id === productId ? apply(p) : p)));
  try {
    await request();
  } catch (err) {
    setProducts((prev) => prev.map((p) => (p.id === productId ? revert(p) : p)));
    onError(err);
  }
}
