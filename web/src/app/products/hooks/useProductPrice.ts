import type { AuthState } from "@/lib/auth-context";
import { updatePrice, type Product } from "@/lib/api";
import { Messages } from "@/lib/messages";
import { updateProductFieldOptimistically } from "./optimisticProductUpdate";

export function useProductPrice({
  auth,
  setProducts,
  setError,
}: {
  auth: AuthState | null;
  setProducts: React.Dispatch<React.SetStateAction<Product[]>>;
  setError: (message: string) => void;
}) {
  async function handleUpdatePrice(product: Product, rawValue: string) {
    if (!auth) return;
    const trimmed = rawValue.trim();
    const parsed = trimmed === "" ? null : Number(trimmed);
    const nextValue = parsed !== null && Number.isFinite(parsed) ? parsed : null;

    await updateProductFieldOptimistically(
      setProducts,
      product.id,
      (p) => ({ ...p, price: nextValue }),
      (p) => ({ ...p, price: product.price }),
      () => updatePrice(auth.token, product.id, nextValue),
      () => setError(Messages.priceUpdateFailed)
    );
  }

  return { handleUpdatePrice };
}
