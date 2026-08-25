"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { Labels } from "@/lib/labels";
import { CompanyHeader } from "../items/components/CompanyHeader";
import { useCompanyLogo } from "../items/hooks/useCompanyLogo";
import { useImportPrices } from "./hooks/useImportPrices";
import { ImportPricesPanel } from "./components/ImportPricesPanel";

export default function ImportPricesPage() {
  const router = useRouter();
  const { auth, isLoading: authLoading, logout } = useAuth();

  useEffect(() => {
    if (!authLoading && !auth) {
      router.replace("/login");
    }
  }, [auth, authLoading, router]);

  const { logoUrl, setLogoUrl, isLoading: isLogoLoading } = useCompanyLogo(auth);
  const [error, setError] = useState<string | null>(null);
  const { isImporting, result, handleImport, handleDownloadTemplate } = useImportPrices({ auth, setError });

  if (authLoading || !auth) {
    return null;
  }

  if (isLogoLoading) {
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
        <span className="h-8 w-8 animate-spin rounded-full border-4 border-white/20 border-t-[#c9a86a]" />
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
          onError={setError}
          logout={logout}
        />

        {error && (
          <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-950 dark:text-red-300">
            {error}
          </p>
        )}

        <ImportPricesPanel
          isImporting={isImporting}
          result={result}
          onImport={handleImport}
          onDownloadTemplate={handleDownloadTemplate}
        />
      </div>
    </div>
  );
}
