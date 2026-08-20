import type { AuthState } from "@/lib/auth-context";
import { updatePrice, type Product } from "@/lib/api";
import { Messages } from "@/lib/messages";

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
    const previous = product.price;

    setProducts((prev) => prev.map((p) => (p.id === product.id ? { ...p, price: nextValue } : p)));
    try {
      await updatePrice(auth.token, product.id, nextValue);
    } catch {
      setProducts((prev) => prev.map((p) => (p.id === product.id ? { ...p, price: previous } : p)));
      setError(Messages.priceUpdateFailed);
    }
  }

  return { handleUpdatePrice };
}
