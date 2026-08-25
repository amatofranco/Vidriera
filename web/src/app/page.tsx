"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";

export default function Home() {
  const router = useRouter();
  const { auth, isLoading } = useAuth();

  useEffect(() => {
    if (isLoading) return;
    router.replace(auth ? "/items" : "/login");
  }, [auth, isLoading, router]);

  return null;
}
