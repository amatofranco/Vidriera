"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { Labels } from "@/lib/labels";
import { Messages, apiErrorMessage } from "@/lib/messages";
import {
  deleteCatalogCoverLogo,
  fetchCatalogCoverLogoUrl,
  getCatalogCoverSettings,
  setCatalogSubtitle,
  uploadCatalogCoverLogo,
} from "@/lib/api";
import { CompanyHeader } from "../items/components/CompanyHeader";
import { useCompanyLogo } from "../items/hooks/useCompanyLogo";

const COVER_LOGO_PREVIEW_HEIGHT = 140;

export default function CatalogCoverPage() {
  const router = useRouter();
  const { auth, isLoading: authLoading, logout } = useAuth();

  useEffect(() => {
    if (!authLoading && !auth) {
      router.replace("/login");
    }
  }, [auth, authLoading, router]);

  const { logoUrl, setLogoUrl, isLoading: isLogoLoading } = useCompanyLogo(auth);

  const [coverLogoUrl, setCoverLogoUrl] = useState<string | null>(null);
  const [subtitle, setSubtitle] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isUploadingCoverLogo, setIsUploadingCoverLogo] = useState(false);
  const [isSavingSubtitle, setIsSavingSubtitle] = useState(false);

  useEffect(() => {
    if (!auth) return;

    async function load(token: string) {
      setIsLoading(true);
      try {
        const [settings, url] = await Promise.all([
          getCatalogCoverSettings(token),
          fetchCatalogCoverLogoUrl(token),
        ]);
        setSubtitle(settings.catalogSubtitle ?? "");
        setCoverLogoUrl(url);
      } catch (err) {
        setError(apiErrorMessage(err, Messages.catalogCoverSettingsLoadFailed));
      } finally {
        setIsLoading(false);
      }
    }

    load(auth.token);
  }, [auth]);

  async function handleCoverLogoChange(file: File) {
    if (!auth) return;
    setIsUploadingCoverLogo(true);
    setError(null);
    try {
      await uploadCatalogCoverLogo(auth.token, file);
      const url = await fetchCatalogCoverLogoUrl(auth.token);
      setCoverLogoUrl(url);
    } catch (err) {
      setError(apiErrorMessage(err, Messages.coverLogoUploadFailed));
    } finally {
      setIsUploadingCoverLogo(false);
    }
  }

  async function handleRemoveCoverLogo() {
    if (!auth) return;
    setIsUploadingCoverLogo(true);
    setError(null);
    try {
      await deleteCatalogCoverLogo(auth.token);
      setCoverLogoUrl(null);
    } catch (err) {
      setError(apiErrorMessage(err, Messages.coverLogoDeleteFailed));
    } finally {
      setIsUploadingCoverLogo(false);
    }
  }

  async function handleSaveSubtitle() {
    if (!auth) return;
    setIsSavingSubtitle(true);
    setError(null);
    try {
      await setCatalogSubtitle(auth.token, subtitle.trim() || null);
    } catch (err) {
      setError(apiErrorMessage(err, Messages.catalogSubtitleSaveFailed));
    } finally {
      setIsSavingSubtitle(false);
    }
  }

  if (authLoading || !auth) {
    return null;
  }

  if (isLoading || isLogoLoading) {
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
      <div className="mx-auto w-full max-w-2xl">
        <CompanyHeader auth={auth} logoUrl={logoUrl} onLogoChanged={setLogoUrl} onError={setError} logout={logout} />

        <p className="mb-4 text-sm text-zinc-200">{Labels.catalogCoverHint}</p>

        {error && (
          <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-950 dark:text-red-300">
            {error}
          </p>
        )}

        <div className="mb-6 rounded-xl border border-black/10 bg-[#ecdcc0] p-4 shadow-lg dark:border-white/10 dark:bg-zinc-900">
          <span className="mb-2 block text-xs font-medium text-zinc-600 dark:text-zinc-400">
            {Labels.catalogCoverLogoLabel}
          </span>
          {coverLogoUrl && (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={coverLogoUrl}
              alt={Labels.catalogCoverLogoAlt}
              style={{ maxHeight: COVER_LOGO_PREVIEW_HEIGHT, backgroundColor: "#1c1c1e" }}
              className="mb-3 h-auto w-auto max-w-full rounded-md object-contain p-2"
            />
          )}
          <div className="flex items-center gap-2">
            <label className="cursor-pointer rounded-md border border-zinc-400 px-3 py-1.5 text-xs font-medium text-zinc-800 transition-colors hover:bg-black/5 dark:border-white/15 dark:text-[#e4c98a] dark:hover:bg-white/10">
              {isUploadingCoverLogo
                ? Labels.uploadingCoverLogo
                : coverLogoUrl
                  ? Labels.changeCoverLogoButton
                  : Labels.uploadCoverLogoButton}
              <input
                type="file"
                accept="image/*"
                className="hidden"
                disabled={isUploadingCoverLogo}
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) handleCoverLogoChange(file);
                }}
              />
            </label>
            {coverLogoUrl && (
              <button
                onClick={handleRemoveCoverLogo}
                disabled={isUploadingCoverLogo}
                className="text-xs text-red-600 underline hover:text-red-500 disabled:opacity-50 dark:text-red-400"
              >
                {Labels.removeCoverLogoButton}
              </button>
            )}
          </div>
        </div>

        <div className="rounded-xl border border-black/10 bg-[#ecdcc0] p-4 shadow-lg dark:border-white/10 dark:bg-zinc-900">
          <span className="mb-2 block text-xs font-medium text-zinc-600 dark:text-zinc-400">
            {Labels.catalogSubtitleLabel}
          </span>
          <div className="flex flex-wrap items-center gap-2">
            <input
              type="text"
              value={subtitle}
              onChange={(e) => setSubtitle(e.target.value)}
              placeholder={Labels.catalogSubtitlePlaceholder}
              maxLength={100}
              className="w-56 rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-900 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
            />
            <button
              onClick={handleSaveSubtitle}
              disabled={isSavingSubtitle}
              className="rounded-md bg-[#8a5a35] px-4 py-1.5 text-sm text-white transition-colors hover:bg-[#a06b41] disabled:opacity-50"
            >
              {isSavingSubtitle ? Labels.savingSubtitle : Labels.saveSubtitleButton}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
