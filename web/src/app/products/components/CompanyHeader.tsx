"use client";

import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { fetchCompanyLogoUrl, uploadCompanyLogo } from "@/lib/api";
import { Messages, apiErrorMessage } from "@/lib/messages";

export function CompanyHeader({
  auth,
  logoUrl,
  onLogoChanged,
  onError,
  logout,
}: {
  auth: AuthState;
  logoUrl: string | null;
  onLogoChanged: (url: string | null) => void;
  onError: (message: string) => void;
  logout: () => void;
}) {
  const [isUploadingLogo, setIsUploadingLogo] = useState(false);

  async function handleLogoChange(file: File) {
    setIsUploadingLogo(true);
    try {
      await uploadCompanyLogo(auth.token, file);
      const url = await fetchCompanyLogoUrl(auth.token);
      onLogoChanged(url);
    } catch (err) {
      onError(apiErrorMessage(err, Messages.logoUploadFailed));
    } finally {
      setIsUploadingLogo(false);
    }
  }

  return (
    <header className="mb-6">
      {logoUrl && (
        // eslint-disable-next-line @next/next/no-img-element -- imagen autenticada vía blob URL, no un asset estático de Next
        <img
          src={logoUrl}
          alt={`Banner de ${auth.companyName}`}
          className="mb-4 h-40 w-full rounded-xl bg-white object-contain opacity-85 shadow-lg"
        />
      )}
      <div className="flex items-center justify-between rounded-xl border border-white/10 bg-black/25 px-5 py-4 backdrop-blur-sm">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-[#c9a86a] text-sm font-semibold text-zinc-900">
            {auth.name.charAt(0).toUpperCase()}
          </div>
          <div className="flex flex-col leading-tight">
            <span className="text-sm font-semibold text-zinc-50">{auth.name}</span>
            <span className="text-xs text-zinc-300">{auth.companyName}</span>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <label className="cursor-pointer rounded-md border border-white/15 px-3 py-1.5 text-xs font-medium text-[#e4c98a] transition-colors hover:bg-white/10">
            {isUploadingLogo ? "Subiendo..." : logoUrl ? "Cambiar banner" : "Subir banner"}
            <input
              type="file"
              accept="image/*"
              className="hidden"
              disabled={isUploadingLogo}
              onChange={(e) => {
                const file = e.target.files?.[0];
                if (file) handleLogoChange(file);
              }}
            />
          </label>
          <button
            onClick={logout}
            className="rounded-md border border-white/15 px-3 py-1.5 text-xs font-medium text-zinc-100 transition-colors hover:border-red-400/40 hover:bg-red-500/10 hover:text-red-300"
          >
            Salir
          </button>
        </div>
      </div>
    </header>
  );
}
