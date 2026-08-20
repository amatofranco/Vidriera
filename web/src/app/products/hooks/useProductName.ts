import type { AuthState } from "@/lib/auth-context";
import { updateName, type Product } from "@/lib/api";
import { Messages, apiErrorMessage } from "@/lib/messages";

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

    const previous = product.name;
    setProducts((prev) => prev.map((p) => (p.id === product.id ? { ...p, name: trimmed } : p)));
    try {
      await updateName(auth.token, product.id, trimmed);
    } catch (err) {
      setProducts((prev) => prev.map((p) => (p.id === product.id ? { ...p, name: previous } : p)));
      setError(apiErrorMessage(err, Messages.nameUpdateFailed));
    }
  }

  return { handleUpdateName };
}
