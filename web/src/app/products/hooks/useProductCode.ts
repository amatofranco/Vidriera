import type { AuthState } from "@/lib/auth-context";
import { updateCode, type Product } from "@/lib/api";
import { Messages } from "@/lib/messages";
import { updateProductFieldOptimistically } from "./optimisticProductUpdate";

export function useProductCode({
  auth,
  setProducts,
  setError,
}: {
  auth: AuthState | null;
  setProducts: React.Dispatch<React.SetStateAction<Product[]>>;
  setError: (message: string) => void;
}) {
  async function handleUpdateCode(product: Product, rawValue: string) {
    if (!auth) return;
    const trimmed = rawValue.trim();
    const nextValue = trimmed === "" ? null : trimmed;

    await updateProductFieldOptimistically(
      setProducts,
      product.id,
      (p) => ({ ...p, code: nextValue }),
      (p) => ({ ...p, code: product.code }),
      () => updateCode(auth.token, product.id, nextValue),
      () => setError(Messages.codeUpdateFailed)
    );
  }

  return { handleUpdateCode };
}
