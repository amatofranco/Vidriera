"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { Labels } from "@/lib/labels";
import { CompanyHeader } from "../products/components/CompanyHeader";
import { useCompanyLogo } from "../products/hooks/useCompanyLogo";
import { useOrdersData } from "./hooks/useOrdersData";
import { OrderRow } from "./components/OrderRow";

export default function OrdersPage() {
  const router = useRouter();
  const { auth, isLoading: authLoading, logout } = useAuth();

  useEffect(() => {
    if (!authLoading && !auth) {
      router.replace("/login");
    }
  }, [auth, authLoading, router]);

  const { logoUrl, setLogoUrl } = useCompanyLogo(auth);
  const { orders, isLoading, error } = useOrdersData(auth, logout);
  const [logoError, setLogoError] = useState<string | null>(null);

  if (authLoading || !auth) {
    return null;
  }

  if (isLoading) {
    return (
      <div
        className="flex w-full flex-1 items-center justify-center px-4 py-10"
        style={{
          backgroundImage:
            "radial-gradient(ellipse 80% 60% at 50% 0%, rgba(240,220,174,0.55) 0%, rgba(160,110,60,0.55) 45%, rgba(90,55,25,0.75) 100%), url('/login-bg.jpg')",
          backgroundSize: "100% 100%, 240%",
          backgroundPosition: "center, center 38%",
          backgroundRepeat: "no-repeat, no-repeat",
          backgroundAttachment: "fixed, fixed",
        }}
      >
        <div className="flex flex-col items-center justify-center gap-3 text-zinc-200">
          <span className="h-8 w-8 animate-spin rounded-full border-4 border-white/20 border-t-[#c9a86a]" />
          <p>{Labels.loadingOrders}</p>
        </div>
      </div>
    );
  }

  return (
    <div
      className="w-full flex-1 px-4 py-10"
      style={{
        backgroundImage:
          "radial-gradient(ellipse 80% 60% at 50% 0%, rgba(240,220,174,0.55) 0%, rgba(160,110,60,0.55) 45%, rgba(90,55,25,0.75) 100%), url('/login-bg.jpg')",
        backgroundSize: "100% 100%, 240%",
        backgroundPosition: "center, center 38%",
        backgroundRepeat: "no-repeat, no-repeat",
        backgroundAttachment: "fixed, fixed",
      }}
    >
      <div className="fixed top-4 left-4 z-10">
        <Image src="/vidriera-logo.png" alt={Labels.logoAlt} width={1000} height={245} className="h-11 w-auto" />
      </div>
      <div className="mx-auto w-full max-w-3xl">
        <CompanyHeader
          auth={auth}
          logoUrl={logoUrl}
          onLogoChanged={setLogoUrl}
          onError={setLogoError}
          logout={logout}
        />

        {(error || logoError) && (
          <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-950 dark:text-red-300">
            {error || logoError}
          </p>
        )}

        {orders.length === 0 ? (
          <p className="rounded-xl border border-black/10 bg-[#ecdcc0] px-4 py-6 text-center text-sm text-zinc-700 shadow-lg dark:border-white/10 dark:bg-zinc-900 dark:text-zinc-300">
            {Labels.noOrdersYet}
          </p>
        ) : (
          <div className="flex flex-col gap-3">
            {orders.map((order) => (
              <OrderRow key={order.id} order={order} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
