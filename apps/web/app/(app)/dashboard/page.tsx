"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import {
  Sprout,
  Baby,
  Heart,
  Activity,
  AlertOctagon,
  ChevronRight,
  Bell,
  Stethoscope,
  Syringe,
  Scale,
  PlusCircle,
} from "lucide-react";
import { api } from "@/lib/api/client";
import { PageHeader } from "@/components/ui/page-header";
import { KpiCard } from "@/components/ui/kpi-card";
import { Section } from "@/components/ui/section";
import { EmptyState } from "@/components/ui/empty-state";
import { TierBadge, StatusPill } from "@/components/ui/badges";
import { Skeleton } from "@/components/ui/skeleton";

// ---------------- types ----------------

type Kpi = {
  metric: string;
  value: number;
  deltaMonth: number | null;
  deltaYear: number | null;
  asOfDate: string;
};

type AnimalSummary = {
  id: string;
  codeName: string;
  primaryName: string | null;
  sex: 1 | 2;
  dob: string;
  status: number;
  performanceTier: 0 | 1 | 2 | 3 | 4 | 5;
  isBSired: boolean;
};

type Underperformer = {
  animalId: string;
  codeName: string;
  primaryName: string | null;
  tier: 0 | 1 | 2 | 3 | 4 | 5;
  flagCodes: string[];
};

type AlertRow = {
  id: string;
  topic: string;
  title: string;
  body: string;
  severity: number;
  createdAt: string;
};

// ---------------- page ----------------

export default function DashboardPage() {
  const { data: kpis, isLoading: kpisLoading } = useQuery({
    queryKey: ["analytics", "kpis"],
    queryFn: () => api<Kpi[]>("/api/v1/analytics/kpis"),
  });

  const { data: animals } = useQuery({
    queryKey: ["animals"],
    queryFn: () => api<AnimalSummary[]>("/api/v1/animals"),
  });

  const { data: underperformers } = useQuery({
    queryKey: ["analytics", "underperformers"],
    queryFn: () => api<Underperformer[]>("/api/v1/analytics/underperformers"),
  });

  const { data: alerts } = useQuery({
    queryKey: ["alerts"],
    queryFn: () => api<AlertRow[]>("/api/v1/alerts"),
  });

  const kByMetric = new Map((kpis ?? []).map((k) => [k.metric, k]));
  const live = kByMetric.get("live_cattle");
  const pregnant = kByMetric.get("confirmed_pregnancies");
  const calvesYtd = kByMetric.get("calves_ytd");
  const watchList = kByMetric.get("watch_list");

  // Upcoming "Today" panel — derived from animals + the calving-calendar logic.
  // We highlight animals with status PregnantConfirmed (4) and tier-E cows.
  const pregnantList = (animals ?? []).filter((a) => a.status === 4);
  const tierEList = (animals ?? []).filter((a) => a.performanceTier === 5);

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        icon={<Sprout className="h-6 w-6" />}
        title="Dashboard"
        description="Today's view of your herd. Tap any tile to drill in."
      />

      {/* KPI row */}
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {kpisLoading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-32 rounded-xl" />
          ))
        ) : (
          <>
            <KpiCard
              label="Live cattle"
              value={Math.round(Number(live?.value ?? 0))}
              hint="Total animals on the farm"
              delta={live?.deltaYear ?? null}
              deltaLabel="YoY"
              icon={<Sprout className="h-5 w-5" />}
              tone="info"
            />
            <KpiCard
              label="Pregnant cows"
              value={Math.round(Number(pregnant?.value ?? 0))}
              hint="Confirmed pregnancies"
              delta={pregnant?.deltaYear ?? null}
              deltaLabel="YoY"
              icon={<Heart className="h-5 w-5" />}
              tone="success"
            />
            <KpiCard
              label="Calves YTD"
              value={Math.round(Number(calvesYtd?.value ?? 0))}
              hint="Born this year"
              delta={calvesYtd?.deltaYear ?? null}
              deltaLabel="YoY"
              icon={<Baby className="h-5 w-5" />}
              tone="default"
            />
            <KpiCard
              label="Watch list"
              value={Math.round(Number(watchList?.value ?? 0))}
              hint="Tier D + Tier E"
              delta={watchList?.deltaYear ?? null}
              deltaLabel="YoY"
              deltaPositiveIsBad
              icon={<AlertOctagon className="h-5 w-5" />}
              tone={(watchList?.value ?? 0) > 0 ? "warning" : "default"}
            />
          </>
        )}
      </div>

      {/* Quick capture row */}
      <Section
        title="Quick capture"
        description="Common field events — one tap to record."
        icon={<PlusCircle className="h-4 w-4" />}
      >
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <QuickAction href="/capture/calving" icon={<Baby className="h-5 w-5" />} label="Calving" tone="bg-pink-100 text-pink-700 dark:bg-pink-950/40 dark:text-pink-300" />
          <QuickAction href="#" icon={<Heart className="h-5 w-5" />} label="Mating" tone="bg-rose-100 text-rose-700 dark:bg-rose-950/40 dark:text-rose-300" disabled />
          <QuickAction href="#" icon={<Syringe className="h-5 w-5" />} label="Treatment" tone="bg-sky-100 text-sky-700 dark:bg-sky-950/40 dark:text-sky-300" disabled />
          <QuickAction href="#" icon={<Scale className="h-5 w-5" />} label="Weighing" tone="bg-amber-100 text-amber-700 dark:bg-amber-950/40 dark:text-amber-300" disabled />
        </div>
      </Section>

      <div className="grid gap-4 lg:grid-cols-3">
        {/* Pregnant cows panel */}
        <Section
          title="Confirmed pregnancies"
          description={`${pregnantList.length} cow${pregnantList.length === 1 ? "" : "s"} on the calving calendar`}
          icon={<Heart className="h-4 w-4" />}
          actions={
            <Link
              href="/animals"
              className="inline-flex items-center text-xs font-medium text-primary hover:underline"
            >
              View all <ChevronRight className="h-3 w-3" />
            </Link>
          }
          bodyClassName="p-0"
        >
          {pregnantList.length === 0 ? (
            <div className="p-4">
              <EmptyState
                icon={<Heart className="h-5 w-5" />}
                title="No pregnancies yet"
                description="Once a service is confirmed, the cow appears here."
              />
            </div>
          ) : (
            <ul className="divide-y divide-border">
              {pregnantList.slice(0, 8).map((a) => (
                <li key={a.id}>
                  <Link
                    href={`/animals/${a.id}`}
                    className="flex items-center justify-between gap-3 px-4 py-2.5 transition hover:bg-accent/50"
                  >
                    <div className="flex min-w-0 items-center gap-2">
                      <TierBadge tier={a.performanceTier} size="sm" />
                      <div className="min-w-0">
                        <p className="truncate text-sm font-medium">
                          {a.primaryName ?? "—"}
                        </p>
                        <p className="truncate font-mono text-[11px] text-muted-foreground">
                          {a.codeName}
                        </p>
                      </div>
                    </div>
                    <StatusPill status={a.status} />
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </Section>

        {/* Attention list */}
        <Section
          title="Needs your attention"
          description={tierEList.length > 0 ? "Tier E — decide cull or confirm pregnant" : "Nobody in the danger zone today"}
          icon={<AlertOctagon className="h-4 w-4" />}
          actions={
            <Link
              href="/reports"
              className="inline-flex items-center text-xs font-medium text-primary hover:underline"
            >
              Open report <ChevronRight className="h-3 w-3" />
            </Link>
          }
          bodyClassName="p-0"
        >
          {underperformers && underperformers.length > 0 ? (
            <ul className="divide-y divide-border">
              {underperformers.slice(0, 6).map((u) => (
                <li key={u.animalId}>
                  <Link
                    href={`/animals/${u.animalId}`}
                    className="flex items-center justify-between gap-3 px-4 py-2.5 transition hover:bg-accent/50"
                  >
                    <div className="flex min-w-0 items-center gap-2">
                      <TierBadge tier={u.tier} size="sm" />
                      <div className="min-w-0">
                        <p className="truncate text-sm font-medium">
                          {u.primaryName ?? "—"}
                        </p>
                        <p className="truncate font-mono text-[11px] text-muted-foreground">
                          {u.codeName}
                        </p>
                      </div>
                    </div>
                    <span className="shrink-0 text-[10px] text-muted-foreground">
                      {u.flagCodes[0]?.replace(/_/g, " ") ?? ""}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          ) : (
            <div className="p-4">
              <EmptyState
                icon={<Activity className="h-5 w-5" />}
                title="No underperformers today"
                description="The tier engine has nothing flagged. Keep doing what you're doing."
              />
            </div>
          )}
        </Section>

        {/* Recent activity */}
        <Section
          title="Recent activity"
          description={`Latest ${Math.min(alerts?.length ?? 0, 6)} events`}
          icon={<Bell className="h-4 w-4" />}
          actions={
            <Link
              href="/alerts"
              className="inline-flex items-center text-xs font-medium text-primary hover:underline"
            >
              View all <ChevronRight className="h-3 w-3" />
            </Link>
          }
          bodyClassName="p-0"
        >
          {alerts && alerts.length > 0 ? (
            <ul className="divide-y divide-border">
              {alerts.slice(0, 6).map((a) => (
                <li key={a.id} className="px-4 py-2.5">
                  <p className="text-sm font-medium leading-tight">{a.title}</p>
                  <p className="text-xs text-muted-foreground">
                    {new Date(a.createdAt).toLocaleString()} · {a.topic}
                  </p>
                </li>
              ))}
            </ul>
          ) : (
            <div className="p-4">
              <EmptyState
                icon={<Bell className="h-5 w-5" />}
                title="Nothing recent"
                description="Activity will appear here as you record events."
              />
            </div>
          )}
        </Section>
      </div>

      {/* Vet / health prompt */}
      <Section
        title="Health & vet upcoming"
        description="Vaccinations, treatments and pregnancy checks due in the next 30 days."
        icon={<Stethoscope className="h-4 w-4" />}
      >
        <EmptyState
          icon={<Stethoscope className="h-5 w-5" />}
          title="Schedule is empty"
          description="When a vaccination or treatment is recorded with a 'next due' interval, it shows up here."
        />
      </Section>
    </div>
  );
}

function QuickAction({
  href,
  icon,
  label,
  tone,
  disabled,
}: {
  href: string;
  icon: React.ReactNode;
  label: string;
  tone: string;
  disabled?: boolean;
}) {
  const inner = (
    <div
      className={`flex h-24 flex-col items-center justify-center gap-1 rounded-xl border text-sm font-medium transition ${
        disabled
          ? "cursor-not-allowed border-dashed bg-muted/30 text-muted-foreground/60"
          : "bg-card hover:bg-accent/40"
      }`}
    >
      <span
        className={`flex h-10 w-10 items-center justify-center rounded-full ${disabled ? "bg-muted text-muted-foreground/60" : tone}`}
      >
        {icon}
      </span>
      {label}
      {disabled && (
        <span className="text-[10px] uppercase tracking-wide text-muted-foreground/60">
          coming soon
        </span>
      )}
    </div>
  );
  if (disabled) return <div aria-disabled>{inner}</div>;
  return <Link href={href}>{inner}</Link>;
}
