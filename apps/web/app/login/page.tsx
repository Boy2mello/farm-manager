"use client";

import Link from "next/link";
import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { ArrowRight, Sprout, ShieldCheck } from "lucide-react";
import { api, setAccessToken } from "@/lib/api/client";

const schema = z.object({
  email: z.string().email("Enter a valid email"),
  password: z.string().min(1, "Password is required"),
});
type FormValues = z.infer<typeof schema>;

type LoginResponse = {
  accessToken: string;
  accessExpiresAt: string;
  refreshToken: string;
  refreshExpiresAt: string;
};

export default function LoginPage() {
  const router = useRouter();
  const search = useSearchParams();
  const next = search.get("next") ?? "/dashboard";
  const reason = search.get("reason");
  const [banner, setBanner] = useState<{ tone: "error" | "info"; text: string } | null>(
    reason === "expired"
      ? { tone: "info", text: "Your session expired. Please sign in again." }
      : null,
  );
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  async function onSubmit(values: FormValues) {
    setBanner(null);
    try {
      const res = await api<LoginResponse>("/api/v1/auth/login", {
        method: "POST",
        body: JSON.stringify(values),
      });
      setAccessToken(res.accessToken);
      const safeNext = next.startsWith("/") && !next.startsWith("//") ? next : "/dashboard";
      router.push(safeNext);
    } catch (e) {
      const msg = e instanceof Error ? e.message : "Sign-in failed";
      setBanner({ tone: "error", text: msg });
    }
  }

  return (
    <main className="grid min-h-screen lg:grid-cols-[1fr_minmax(420px,440px)]">
      <aside className="relative hidden overflow-hidden bg-gradient-to-br from-emerald-600 via-emerald-700 to-emerald-900 p-10 text-emerald-50 lg:flex lg:flex-col lg:justify-between">
        <div
          aria-hidden
          className="absolute inset-0 opacity-[0.08] [background-image:radial-gradient(white_1px,transparent_1px)] [background-size:24px_24px]"
        />
        <Link href="/" className="relative z-10 inline-flex items-center gap-2 font-semibold">
          <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-emerald-50 text-emerald-800">
            <Sprout className="h-5 w-5" />
          </span>
          Farm Manager
        </Link>

        <div className="relative z-10 space-y-6">
          <h2 className="text-3xl font-bold leading-tight">
            Capture once.<br />Compute everything.
          </h2>
          <p className="max-w-md text-base text-emerald-100">
            Mobile-first livestock management. Every event you log fans out into
            calving intervals, performance tiers, inbreeding blocks and
            vaccination reminders — automatically.
          </p>
          <ul className="space-y-3 text-sm">
            <Bullet>WhatsApp + Web Push notifications</Bullet>
            <Bullet>Auto-flagging of under-performing cows</Bullet>
            <Bullet>Offline capture in the kraal, syncs on reconnect</Bullet>
          </ul>
        </div>

        <p className="relative z-10 inline-flex items-center gap-1.5 text-xs text-emerald-100/80">
          <ShieldCheck className="h-3.5 w-3.5" />
          Your herd is private. Multi-tenant isolation by default.
        </p>
      </aside>

      <section className="flex items-center justify-center p-6 lg:p-12">
        <div className="w-full max-w-sm space-y-6">
          <Link href="/" className="inline-flex items-center gap-2 font-semibold lg:hidden">
            <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground">
              <Sprout className="h-4 w-4" />
            </span>
            Farm Manager
          </Link>

          <div className="space-y-2">
            <h1 className="text-3xl font-bold tracking-tight">Welcome back</h1>
            <p className="text-sm text-muted-foreground">
              Sign in to view your herd.
            </p>
          </div>

          {banner && (
            <div
              role="status"
              className={
                banner.tone === "error"
                  ? "rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive"
                  : "rounded-md border border-sky-300/40 bg-sky-50 px-3 py-2 text-sm text-sky-800 dark:bg-sky-950/40 dark:text-sky-100"
              }
            >
              {banner.text}
            </div>
          )}

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-1.5">
              <label htmlFor="email" className="text-sm font-medium">
                Email
              </label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                inputMode="email"
                placeholder="you@example.com"
                className="w-full rounded-md border bg-background px-3 py-2.5 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                {...register("email")}
              />
              {errors.email && (
                <p className="text-xs text-destructive">{errors.email.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <label htmlFor="password" className="text-sm font-medium">
                Password
              </label>
              <input
                id="password"
                type="password"
                autoComplete="current-password"
                className="w-full rounded-md border bg-background px-3 py-2.5 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                {...register("password")}
              />
              {errors.password && (
                <p className="text-xs text-destructive">{errors.password.message}</p>
              )}
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="inline-flex h-11 w-full items-center justify-center gap-2 rounded-md bg-primary text-sm font-medium text-primary-foreground shadow-xs hover:bg-primary/90 disabled:opacity-50"
            >
              {isSubmitting ? "Signing in…" : (<>Sign in <ArrowRight className="h-4 w-4" /></>)}
            </button>
          </form>

          <p className="text-center text-xs text-muted-foreground">
            By signing in you accept that Farm Manager will store farm data
            against your organisation only.
          </p>
        </div>
      </section>
    </main>
  );
}

function Bullet({ children }: { children: React.ReactNode }) {
  return (
    <li className="flex items-start gap-2">
      <span className="mt-1 h-1.5 w-1.5 shrink-0 rounded-full bg-emerald-300" />
      <span className="text-emerald-50/95">{children}</span>
    </li>
  );
}
