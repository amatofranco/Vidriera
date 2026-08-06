import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import type { AuthState } from "@/lib/auth-context";
import { ApiError, getProducts, getSections, type Product, type Section } from "@/lib/api";
import { Messages, apiErrorMessage } from "@/lib/messages";

export function useProductsData(auth: AuthState | null, logout: () => void) {
  const router = useRouter();

  const [products, setProducts] = useState<Product[]>([]);
  const [sections, setSections] = useState<Section[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function loadProducts(token: string, options?: { silent?: boolean }) {
    const silent = options?.silent ?? false;
    if (!silent) setIsLoading(true);
    setError(null);
    try {
      const result = await getProducts(token);
      setProducts(result);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        logout();
        router.replace("/login");
        return;
      }
      setError(apiErrorMessage(err, Messages.productsLoadFailed));
    } finally {
      if (!silent) setIsLoading(false);
    }
  }

  async function loadSections(token: string) {
    try {
      const result = await getSections(token);
      setSections(result);
    } catch {
    }
  }

  useEffect(() => {
    if (!auth) return;
    // eslint-disable-next-line react-hooks/set-state-in-effect
    loadProducts(auth.token);
    loadSections(auth.token);
  }, [auth]);

  return {
    products,
    setProducts,
    sections,
    setSections,
    isLoading,
    error,
    setError,
    loadProducts,
    loadSections,
  };
}
