"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import type { AuthState } from "@/lib/auth-context";
import { fetchCompanyLogoUrl, uploadCompanyLogo } from "@/lib/api";
import { Labels } from "@/lib/labels";
import { Messages, apiErrorMessage } from "@/lib/messages";

const NAV_LINKS = [
  { href: "/products", label: Labels.productsNavLabel },
  { href: "/orders", label: Labels.ordersNavLabel },
  { href: "/import-prices", label: Labels.importPricesNavLabel },
];

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
  const pathname = usePathname();

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
        // eslint-disable-next-line @next/next/no-img-element
        <img
          src={logoUrl}
          alt={Labels.bannerAlt(auth.companyName)}
          className="mb-4 h-40 w-full rounded-xl bg-white object-contain opacity-85 shadow-lg"
        />
      )}
      <div className="rounded-xl border border-white/10 bg-black/25 backdrop-blur-sm">
        <div className="flex items-center justify-between px-5 py-4">
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
              {isUploadingLogo ? Labels.uploadingBanner : logoUrl ? Labels.changeBanner : Labels.uploadBanner}
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
              {Labels.logout}
            </button>
          </div>
        </div>
        <nav className="flex items-center gap-5 border-t border-white/10 px-5">
          {NAV_LINKS.map((link) => {
            const isActive = pathname === link.href;
            return (
              <Link
                key={link.href}
                href={link.href}
                className={`border-b-2 py-2.5 text-xs font-medium transition-colors ${
                  isActive
                    ? "border-[#c9a86a] text-[#e4c98a]"
                    : "border-transparent text-zinc-300 hover:text-zinc-100"
                }`}
              >
                {link.label}
              </Link>
            );
          })}
        </nav>
      </div>
    </header>
  );
}
