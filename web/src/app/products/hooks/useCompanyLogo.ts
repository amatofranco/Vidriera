import { useEffect, useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { fetchCompanyLogoUrl } from "@/lib/api";

export function useCompanyLogo(auth: AuthState | null) {
  const [logoUrl, setLogoUrl] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!auth) return;
    let cancelled = false;
    let objectUrl: string | null = null;

    fetchCompanyLogoUrl(auth.token).then((url) => {
      if (cancelled) return;
      objectUrl = url;
      setLogoUrl(url);
      setIsLoading(false);
    });

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [auth]);

  return { logoUrl, setLogoUrl, isLoading };
}
