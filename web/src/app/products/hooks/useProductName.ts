import type { AuthState } from "@/lib/auth-context";
import { updateName, type Product } from "@/lib/api";
import { Messages, apiErrorMessage } from "@/lib/messages";
import { updateProductFieldOptimistically } from "./optimisticProductUpdate";

export function useProductName({
  auth,
  setProducts,
  setError,
}: {
  auth: AuthState | null;
  setProducts: React.Dispatch<React.SetStateAction<Product[]>>;
  setError: (message: string) => void;
}) {
  async function handleUpdateName(product: Product, rawValue: string) {
    if (!auth) return;
    const trimmed = rawValue.trim();
    if (trimmed === "") {
      setError(Messages.nameCannotBeEmpty);
      return;
    }
    if (trimmed === product.name) return;

    await updateProductFieldOptimistically(
      setProducts,
      product.id,
      (p) => ({ ...p, name: trimmed }),
      (p) => ({ ...p, name: product.name }),
      () => updateName(auth.token, product.id, trimmed),
      (err) => setError(apiErrorMessage(err, Messages.nameUpdateFailed))
    );
  }

  return { handleUpdateName };
}
