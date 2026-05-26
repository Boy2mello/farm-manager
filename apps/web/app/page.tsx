import Link from "next/link";
import { Sprout, ShieldCheck, Smartphone, BarChart3, ArrowRight } from "lucide-react";

export default function HomePage() {
  return (
    <main className="relative min-h-screen overflow-hidden bg-gradient-to-b from-emerald-50/40 via-background to-background dark:from-emerald-950/20">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10 [background:radial-gradient(circle_at_top_right,hsl(var(--primary)/0.12),transparent_55%),radial-gradient(circle_at_bottom_left,hsl(var(--info)/0.08),transparent_50%)]"
      />

      <header className="container flex h-16 items-center justify-between">
        <Link href="/" className="flex items-center gap-2 font-semibold">
          <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-primary-foreground shadow-xs">
            <Sprout className="h-5 w-5" />
          </span>
          Farm Manager
        </Link>
        <Link
          href="/login"
          className="rounded-md border bg-card px-4 py-2 text-sm font-medium hover:bg-accent"
        >
          Sign in
        </Link>
      </header>

      <section className="container grid items-center gap-12 py-12 lg:grid-cols-2 lg:py-24">
        <div className="space-y-6">
          <span className="inline-flex items-center gap-1.5 rounded-full bg-primary/10 px-3 py-1 text-xs font-medium text-primary">
            <ShieldCheck className="h-3.5 w-3.5" />
            Private to your farm
          </span>
          <h1 className="text-4xl font-bold tracking-tight sm:text-5xl lg:text-6xl">
            Run your herd from your phone.
          </h1>
          <p className="max-w-prose text-base text-muted-foreground sm:text-lg">
            Farm Manager replaces the spreadsheet with a calm, mobile-first
            cockpit. Capture a calving in the kraal, see why a cow is dropping
            tier, and never miss a vaccination — all from one screen.
          </p>
          <div className="flex flex-wrap gap-3">
            <Link
              href="/login"
              className="inline-flex items-center gap-2 rounded-md bg-primary px-5 py-3 text-sm font-medium text-primary-foreground shadow-xs hover:bg-primary/90"
            >
              Sign in
              <ArrowRight className="h-4 w-4" />
            </Link>
            <a
              href="https://github.com/Boy2mello/farm-manager"
              target="_blank"
              rel="noreferrer noopener"
              className="inline-flex items-center gap-2 rounded-md border bg-card px-5 py-3 text-sm font-medium hover:bg-accent"
            >
              View the project
            </a>
          </div>
          <p className="text-xs text-muted-foreground">
            Your herd is private. Sign in to view your animals — you only ever
            see your own sub-herd.
          </p>
        </div>

        <div className="relative">
          <div className="rounded-2xl border bg-card p-2 shadow-lg">
            <div className="aspect-[5/6] overflow-hidden rounded-xl bg-gradient-to-br from-emerald-500 via-emerald-600 to-emerald-800 p-6">
              <div className="grid h-full grid-cols-2 gap-3">
                <MockKpi label="Live cattle" value="42" tone="bg-white/95" />
                <MockKpi label="Pregnant" value="14" tone="bg-white/95" />
                <MockKpi label="Tier A" value="5" tone="bg-emerald-200/95 text-emerald-900" />
                <MockKpi label="Tier E" value="3" tone="bg-rose-200/95 text-rose-900" />
                <div className="col-span-2 rounded-xl bg-white/95 p-3 text-emerald-950 shadow">
                  <p className="text-[10px] font-medium uppercase tracking-wide text-emerald-800/70">
                    Today
                  </p>
                  <ul className="mt-1 space-y-1 text-xs">
                    <li className="flex items-center justify-between">
                      <span>Preg-check · Baizani</span>
                      <span className="rounded bg-sky-100 px-1.5 py-0.5 text-[10px] text-sky-700">due</span>
                    </li>
                    <li className="flex items-center justify-between">
                      <span>Vaccination · 4 calves</span>
                      <span className="rounded bg-amber-100 px-1.5 py-0.5 text-[10px] text-amber-700">overdue</span>
                    </li>
                    <li className="flex items-center justify-between">
                      <span>Calving · Lapi</span>
                      <span className="rounded bg-pink-100 px-1.5 py-0.5 text-[10px] text-pink-700">in 3d</span>
                    </li>
                  </ul>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="container grid gap-4 py-12 sm:grid-cols-3">
        <Feature
          icon={<Smartphone className="h-5 w-5" />}
          title="Mobile-first"
          body="Designed for one-handed use in the field. Works offline, syncs when the signal returns."
        />
        <Feature
          icon={<BarChart3 className="h-5 w-5" />}
          title="Insight, not data entry"
          body="Tier engine, inbreeding blocks, calving calendar — all computed, never typed."
        />
        <Feature
          icon={<ShieldCheck className="h-5 w-5" />}
          title="Private to your herd"
          body="Multi-tenant by default. Your data never crosses farms."
        />
      </section>

      <footer className="container border-t py-6 text-xs text-muted-foreground">
        © {new Date().getFullYear()} Farm Manager. Built for Tumi's herd; ready for yours.
      </footer>
    </main>
  );
}

function MockKpi({ label, value, tone }: { label: string; value: string; tone: string }) {
  return (
    <div className={`rounded-xl ${tone} p-3 shadow`}>
      <p className="text-[10px] font-medium uppercase tracking-wide opacity-70">{label}</p>
      <p className="text-2xl font-bold tabular-nums">{value}</p>
    </div>
  );
}

function Feature({ icon, title, body }: { icon: React.ReactNode; title: string; body: string }) {
  return (
    <div className="rounded-xl border bg-card p-5">
      <span className="inline-flex h-9 w-9 items-center justify-center rounded-lg bg-primary/10 text-primary">
        {icon}
      </span>
      <h3 className="mt-3 font-semibold">{title}</h3>
      <p className="mt-1 text-sm text-muted-foreground">{body}</p>
    </div>
  );
}
