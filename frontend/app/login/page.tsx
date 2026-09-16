"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/components/auth/AuthProvider";

import { api } from "@/lib/api";
import { apiRoutes } from "@/lib/api-routes";
import { LoginResponse } from "@/types/auth";

export default function LoginPage() {
  const router = useRouter();
  const { login } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const [loading, setLoading] = useState(false);

  const [error, setError] = useState("");

  async function handleSubmit(event: React.SubmitEvent<HTMLFormElement>) {
    event.preventDefault();

    setError("");

    if (!email || !password) {
      setError("Please enter email and password.");
      return;
    }

    try {
      setLoading(true);

      const result = await api.post<LoginResponse>(apiRoutes.auth.login, {
        email,
        password,
      });

      login(result.user, result.accessToken);

      if (result.user.role === "Admin") {
        router.push("/admin/services");
      } else {
        router.push("/services");
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : "Login failed.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="flex min-h-[calc(100vh-73px)] items-center justify-center px-4 py-10 sm:px-6 lg:px-8">
      <div className="grid w-full max-w-5xl overflow-hidden rounded-3xl border border-(--border) bg-white shadow-[0_24px_70px_rgba(23,32,42,0.08)] lg:grid-cols-[0.9fr_1.1fr]">
        <section className="relative hidden overflow-hidden bg-(--brand) p-10 text-white lg:flex lg:flex-col lg:justify-between">
          <div className="absolute -right-20 -top-20 h-56 w-56 rounded-full border-26 border-white/10" />
          <div className="absolute -bottom-24 -left-16 h-64 w-64 rounded-full border-32 border-white/10" />

          <div className="relative">
            <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-white text-sm font-bold text-(--brand)">
              SB
            </div>
            <p className="mt-10 text-sm font-bold uppercase tracking-[0.18em] text-[#bfe4d9]">
              Service Booking
            </p>
            <h2 className="mt-4 max-w-sm text-4xl font-bold leading-tight tracking-tight">
              Make time for what matters.
            </h2>
            <p className="mt-5 max-w-sm text-sm leading-6 text-[#d8eee8]">
              Book trusted services in a few simple steps and keep every
              appointment in one place.
            </p>
          </div>

          <p className="relative text-sm text-[#bfe4d9]">
            Simple scheduling. Better days.
          </p>
        </section>

        <form onSubmit={handleSubmit} className="space-y-7 p-6 sm:p-10 lg:p-14">
          <div>
            <p className="text-sm font-bold uppercase tracking-[0.16em] text-(--brand)">
              Welcome back
            </p>
            <h1 className="mt-3 text-3xl font-bold tracking-tight text-foreground sm:text-4xl">
              Sign in to continue
            </h1>
            <p className="mt-3 text-sm leading-6 text-(--muted)">
              Access your bookings and find your next available service.
            </p>
          </div>

          {error && (
            <div
              className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700"
              role="alert"
            >
              <p className="font-semibold">Unable to sign in</p>
              <p className="mt-1">{error}</p>
            </div>
          )}

          <div className="space-y-5">
            <div>
              <label
                htmlFor="email"
                className="mb-2 block text-sm font-semibold text-foreground"
              >
                Email address
              </label>
              <input
                id="email"
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                className="w-full rounded-xl border border-(--border) bg-white px-4 py-3 text-sm text-foreground outline-none transition placeholder:text-gray-400 focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
                placeholder="you@example.com"
                autoComplete="email"
              />
            </div>

            <div>
              <label
                htmlFor="password"
                className="mb-2 block text-sm font-semibold text-foreground"
              >
                Password
              </label>
              <input
                id="password"
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                className="w-full rounded-xl border border-(--border) bg-white px-4 py-3 text-sm text-foreground outline-none transition placeholder:text-gray-400 focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
                placeholder="Enter your password"
                autoComplete="current-password"
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={loading}
            className="flex w-full items-center justify-center gap-2 rounded-xl bg-(--brand) px-4 py-3.5 text-sm font-bold text-white transition hover:bg-(--brand-dark) disabled:cursor-not-allowed disabled:opacity-50"
          >
            {loading && (
              <span className="h-4 w-4 animate-spin rounded-full border-2 border-white/40 border-t-white" />
            )}
            {loading ? "Signing in..." : "Sign in"}
          </button>

          <p className="text-center text-xs leading-5 text-gray-400">
            Your account is protected and your booking details stay private.
          </p>
        </form>
      </div>
    </main>
  );
}
