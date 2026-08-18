import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import type { AuthState } from "@/lib/auth-context";
import { ApiError, getOrders, type Order } from "@/lib/api";
import { Messages, apiErrorMessage } from "@/lib/messages";

export function useOrdersData(auth: AuthState | null, logout: () => void) {
  const router = useRouter();

  const [orders, setOrders] = useState<Order[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!auth) return;

    async function loadOrders(token: string) {
      setIsLoading(true);
      setError(null);
      try {
        const result = await getOrders(token);
        setOrders(result);
      } catch (err) {
        if (err instanceof ApiError && err.status === 401) {
          logout();
          router.replace("/login");
          return;
        }
        setError(apiErrorMessage(err, Messages.ordersLoadFailed));
      } finally {
        setIsLoading(false);
      }
    }

    loadOrders(auth.token);
  }, [auth, logout, router]);

  return { orders, isLoading, error };
}
