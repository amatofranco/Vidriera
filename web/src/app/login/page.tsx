"use client";

import { useState, type FormEvent } from "react";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { login } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { Messages, apiErrorMessage } from "@/lib/messages";

export default function LoginPage() {
  const router = useRouter();
  const { setAuth } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      const result = await login(email, password);
      setAuth(result);
      router.replace("/products");
    } catch (err) {
      setError(apiErrorMessage(err, Messages.loginFailed));
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

      <form
        onSubmit={handleSubmit}
        className="w-full max-w-sm rounded-xl border border-white/10 bg-black/40 p-8 shadow-2xl backdrop-blur-md"
      >
        <div className="mb-7 flex justify-center">
          <Image
            src="/vidriera-logo.png"
            alt="Vidriera"
            width={1000}
            height={245}
            priority
            className="h-9 w-auto"
          />
        </div>

        <label className="mb-1 block text-sm font-medium text-zinc-300">Email</label>
        <input
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="mb-4 w-full rounded-md border border-white/15 bg-white/5 px-3 py-2 text-zinc-50 outline-none focus:border-[#c9a86a]"
        />

        <label className="mb-1 block text-sm font-medium text-zinc-300">Contraseña</label>
        <input
          type="password"
          required
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className="mb-6 w-full rounded-md border border-white/15 bg-white/5 px-3 py-2 text-zinc-50 outline-none focus:border-[#c9a86a]"
        />

        {error && <p className="mb-4 text-sm text-red-400">{error}</p>}

        <button
          type="submit"
          disabled={isSubmitting}
          className="flex w-full items-center justify-center gap-2 rounded-md bg-[#c9a86a] px-4 py-2 font-medium text-zinc-900 transition-colors hover:bg-[#d4b57a] disabled:opacity-50"
        >
          {isSubmitting && (
            <span className="h-4 w-4 animate-spin rounded-full border-2 border-zinc-900/30 border-t-zinc-900" />
          )}
          {isSubmitting ? "Ingresando..." : "Ingresar"}
        </button>
      </form>
    </div>
  );
}
