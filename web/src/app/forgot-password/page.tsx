"use client";

import { useState, type FormEvent } from "react";
import Image from "next/image";
import Link from "next/link";
import { requestPasswordReset } from "@/lib/api";
import { Labels } from "@/lib/labels";
import { Messages, apiErrorMessage } from "@/lib/messages";

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSent, setIsSent] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await requestPasswordReset(email);
      setIsSent(true);
    } catch (err) {
      setError(apiErrorMessage(err, Messages.forgotPasswordFailed));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="relative flex flex-1 items-center justify-center overflow-hidden px-4">
      <div
        className="absolute inset-0 -z-10 bg-center bg-no-repeat"
        style={{ backgroundImage: "url(/login-bg.jpg)", backgroundSize: "100% 100%" }}
      />
      <div className="absolute inset-0 -z-10 bg-black/15" />

      <div className="w-full max-w-sm rounded-xl border border-white/10 bg-black/40 p-8 shadow-2xl backdrop-blur-md">
        <div className="mb-7 flex justify-center">
          <Image
            src="/vidriera-logo.png"
            alt={Labels.logoAlt}
            width={1000}
            height={245}
            priority
            className="h-9 w-auto"
          />
        </div>

        <h1 className="mb-4 text-center text-lg font-semibold text-zinc-50">{Labels.forgotPasswordTitle}</h1>

        {isSent ? (
          <>
            <p className="mb-6 text-center text-sm text-zinc-300">{Labels.forgotPasswordSent}</p>
            <Link
              href="/login"
              className="block text-center text-sm text-zinc-300 underline hover:text-zinc-50"
            >
              {Labels.backToLoginLink}
            </Link>
          </>
        ) : (
          <form onSubmit={handleSubmit}>
            <p className="mb-4 text-sm text-zinc-300">{Labels.forgotPasswordHint}</p>

            <label className="mb-1 block text-sm font-medium text-zinc-300">{Labels.emailFieldLabel}</label>
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="mb-4 w-full rounded-md border border-white/15 bg-white/5 px-3 py-2 text-zinc-50 outline-none focus:border-[#c9a86a]"
            />

            {error && <p className="mb-4 text-sm text-red-400">{error}</p>}

            <button
              type="submit"
              disabled={isSubmitting}
              className="mb-4 flex w-full items-center justify-center gap-2 rounded-md bg-[#c9a86a] px-4 py-2 font-medium text-zinc-900 transition-colors hover:bg-[#d4b57a] disabled:opacity-50"
            >
              {isSubmitting && (
                <span className="h-4 w-4 animate-spin rounded-full border-2 border-zinc-900/30 border-t-zinc-900" />
              )}
              {isSubmitting ? Labels.forgotPasswordSubmitting : Labels.forgotPasswordSubmit}
            </button>

            <Link
              href="/login"
              className="block text-center text-sm text-zinc-300 underline hover:text-zinc-50"
            >
              {Labels.backToLoginLink}
            </Link>
          </form>
        )}
      </div>
    </div>
  );
}
