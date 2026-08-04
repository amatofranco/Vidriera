import type { AuthState } from "@/lib/auth-context";
import { updateStock, type Product } from "@/lib/api";

export function useBulkStockToggle({
  auth,
  products,
  setProducts,
  setError,
  loadProducts,
}: {
  auth: AuthState | null;
  products: Product[];
  setProducts: React.Dispatch<React.SetStateAction<Product[]>>;
  setError: (message: string) => void;
  loadProducts: (token: string, options?: { silent?: boolean }) => Promise<void>;
}) {
  async function handleToggleStock(product: Product) {
    if (!auth) return;
    const nextValue = !product.hasStock;
    setProducts((prev) =>
      prev.map((p) => (p.id === product.id ? { ...p, hasStock: nextValue } : p))
    );
    try {
      await updateStock(auth.token, product.id, nextValue);
    } catch {
      setProducts((prev) =>
        prev.map((p) => (p.id === product.id ? { ...p, hasStock: !nextValue } : p))
      );
      setError("No se pudo actualizar el stock, intentá de nuevo.");
    }
  }

  // Operates on whatever the search box currently shows, so a filtered subset can be
  // bulk-toggled without touching the rest of a long (e.g. 200-product) list.
  async function handleBulkStockToggle(nextValue: boolean, search: string) {
    if (!auth) return;
    const query = search.trim().toLowerCase();
    const targets = products.filter(
      (p) => p.name.toLowerCase().includes(query) && p.hasStock !== nextValue
    );
    if (targets.length === 0) return;

    const targetIds = new Set(targets.map((p) => p.id));
    setProducts((prev) =>
      prev.map((p) => (targetIds.has(p.id) ? { ...p, hasStock: nextValue } : p))
    );
    try {
      await Promise.all(targets.map((p) => updateStock(auth.token, p.id, nextValue)));
    } catch {
      setError("No se pudo actualizar el stock de todos los productos, revisá la lista.");
      loadProducts(auth.token);
    }
  }

  async function handleToggleSectionStock(members: Product[], nextValue: boolean, sectionName: string) {
    if (!auth) return;
    const targets = members.filter((p) => p.hasStock !== nextValue);
    if (targets.length === 0) return;
    const targetIds = new Set(targets.map((p) => p.id));
    setProducts((prev) => prev.map((p) => (targetIds.has(p.id) ? { ...p, hasStock: nextValue } : p)));
    try {
      await Promise.all(targets.map((p) => updateStock(auth.token, p.id, nextValue)));
    } catch {
      setError(`No se pudo actualizar el stock de todos los productos de "${sectionName}".`);
      loadProducts(auth.token, { silent: true });
    }
  }

  return { handleToggleStock, handleBulkStockToggle, handleToggleSectionStock };
}
