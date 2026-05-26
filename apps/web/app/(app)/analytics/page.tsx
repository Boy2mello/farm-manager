"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  BarChart3,
  Sprout,
  Heart,
  Baby,
  Activity,
  AlertOctagon,
  GitBranch,
  CalendarRange,
  Lightbulb,
  Crown,
  TrendingUp,
  TrendingDown,
  ChevronRight,
} from "lucide-react";
import {
  PieChart,
  Pie,
  Cell,
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
  LineChart,
  Line,
} from "recharts";
import { api } from "@/lib/api/client";
import { cn } from "@/lib/utils";
import { PageHeader } from "@/components/ui/page-header";
import { Section } from "@/components/ui/section";
import { KpiCard } from "@/components/ui/kpi-card";
import { EmptyState } from "@/components/ui/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badges";

const tooltipStyle = {
  background: "hsl(var(--popover))",
  border: "1px solid hsl(var(--border))",
  borderRadius: 8,
  fontSize: 12,
  padding: "6px 10px",
  color: "hsl(var(--popover-foreground))",
};

type Tab = "overview" | "bulls" | "bloodlines" | "years" | "insights";

const TABS: Array<{ key: Tab; label: string; icon: React.ComponentType<{ className?: string }>; sub: string }> = [
  { key: "overview", label: "Overview", icon: BarChart3, sub: "Composition + fertility + watchlist" },
  { key: "bulls", label: "Bulls", icon: Crown, sub: "Sire performance + daughter quality" },
  { key: "bloodlines", label: "Bloodlines", icon: GitBranch, sub: "Per-sire descendant outcomes" },
  { key: "years", label: "Years", icon: CalendarRange, sub: "Year-by-year story of the herd" },
  { key: "insights", label: "Insights", icon: Lightbulb, sub: "Proactive observations to act on" },
];

export default function AnalyticsPage() {
  const [tab, setTab] = useState<Tab>("overview");

  return (
    <div className="space-y-5 animate-fade-in">
      <PageHeader
        icon={<BarChart3 className="h-6 w-6" />}
        title="Herd intelligence"
        description="Operational analytics — decisions, not dashboards."
      />

      <nav className="grid grid-cols-2 gap-2 rounded-lg border bg-card p-2 sm:grid-cols-5">
        {TABS.map(({ key, label, icon: Icon, sub }) => {
          const active = tab === key;
          return (
            <button
              key={key}
              type="button"
              onClick={() => setTab(key)}
              className={cn(
                "flex flex-col items-start gap-0.5 rounded-md p-2.5 text-left transition",
                active ? "bg-primary text-primary-foreground shadow-xs" : "text-muted-foreground hover:bg-accent hover:text-foreground",
              )}
            >
              <span className="flex items-center gap-1.5 text-sm font-semibold">
                <Icon className="h-4 w-4" /> {label}
              </span>
              <span className={cn("text-[10px] opacity-80", active && "text-primary-foreground/80")}>
                {sub}
              </span>
            </button>
          );
        })}
      </nav>

      {tab === "overview" && <OverviewTab />}
      {tab === "bulls" && <BullsTab />}
      {tab === "bloodlines" && <BloodlinesTab />}
      {tab === "years" && <YearsTab />}
      {tab === "insights" && <InsightsTab />}
    </div>
  );
}

// =====================================================================
// OVERVIEW
// =====================================================================

type HerdIntel = {
  asOfDate: string;
  liveTotal: number;
  breedingCows: number;
  heifers: number;
  calves: number;
  bulls: number;
  steersAndOther: number;
  cowsConfirmedPregnant: number;
  cowsOpen: number;
  cowsLactating: number;
  pregnancyRatePctNow: number;
  replacementRatePctYtd: number;
  meanCpy: number;
  meanCalvingIntervalMonths: number;
  calfMortalityRatePct: number;
  topPerformerCount: number;
  watchListCount: number;
  ageingOutCount: number;
  firstTimeMothersThisYear: number;
};

function OverviewTab() {
  const { data, isLoading } = useQuery({
    queryKey: ["analytics", "intel", "herd"],
    queryFn: () => api<HerdIntel>("/api/v1/analytics/intelligence/herd"),
  });

  if (isLoading || !data) return <Skeleton className="h-96 rounded-xl" />;

  const composition = [
    { name: "Breeding cows", value: data.breedingCows, fill: "#10b981" },
    { name: "Heifers", value: data.heifers, fill: "#f59e0b" },
    { name: "Calves", value: data.calves, fill: "#ec4899" },
    { name: "Bulls", value: data.bulls, fill: "#0ea5e9" },
    { name: "Other", value: data.steersAndOther, fill: "#94a3b8" },
  ];
  const breedingStatus = [
    { name: "Pregnant", value: data.cowsConfirmedPregnant, fill: "#ec4899" },
    { name: "Open", value: data.cowsOpen, fill: "#0ea5e9" },
    { name: "Lactating", value: data.cowsLactating, fill: "#f59e0b" },
  ];

  return (
    <div className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <KpiCard
          label="Live herd"
          value={data.liveTotal}
          hint="Excludes sold / dead"
          icon={<Sprout className="h-5 w-5" />}
          tone="info"
        />
        <KpiCard
          label="Pregnancy rate"
          value={`${data.pregnancyRatePctNow.toFixed(1)}%`}
          hint={`${data.cowsConfirmedPregnant} of ${data.breedingCows} cows confirmed`}
          icon={<Heart className="h-5 w-5" />}
          tone="success"
        />
        <KpiCard
          label="Mean CPY"
          value={data.meanCpy.toFixed(2)}
          hint="Calves per productive year (target ≥ 0.7)"
          icon={<Baby className="h-5 w-5" />}
        />
        <KpiCard
          label="Calving interval"
          value={`${data.meanCalvingIntervalMonths.toFixed(1)} mo`}
          hint="Target 12–14 mo"
          icon={<Activity className="h-5 w-5" />}
          tone={data.meanCalvingIntervalMonths > 16 ? "warning" : "default"}
        />
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <Section title="Herd composition" icon={<Sprout className="h-4 w-4" />}>
          <div className="h-64">
            <ResponsiveContainer>
              <PieChart>
                <Pie
                  data={composition}
                  dataKey="value"
                  nameKey="name"
                  innerRadius={50}
                  outerRadius={85}
                  paddingAngle={2}
                  label={(p) => `${p.name}: ${p.value}`}
                  labelLine={false}
                  style={{ fontSize: 11 }}
                >
                  {composition.map((s, i) => (
                    <Cell key={i} fill={s.fill} stroke="hsl(var(--card))" strokeWidth={2} />
                  ))}
                </Pie>
                <Tooltip contentStyle={tooltipStyle} />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </Section>

        <Section title="Breeding status today" icon={<Heart className="h-4 w-4" />}>
          <div className="h-64">
            <ResponsiveContainer>
              <BarChart data={breedingStatus} margin={{ top: 4, right: 4, left: -16, bottom: 4 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
                <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
                <Tooltip contentStyle={tooltipStyle} />
                <Bar dataKey="value" radius={[6, 6, 0, 0]}>
                  {breedingStatus.map((s, i) => (
                    <Cell key={i} fill={s.fill} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </Section>
      </div>

      <Section title="Strength signals" icon={<TrendingUp className="h-4 w-4" />}>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <SignalTile icon={<Crown className="h-4 w-4" />} tone="success" label="Top performers (tier A)" value={data.topPerformerCount} />
          <SignalTile icon={<AlertOctagon className="h-4 w-4" />} tone="danger" label="Watch list (D + E)" value={data.watchListCount} />
          <SignalTile icon={<TrendingDown className="h-4 w-4" />} tone="warning" label="Ageing out (>10 yr)" value={data.ageingOutCount} />
          <SignalTile icon={<Baby className="h-4 w-4" />} tone="info" label="First-time mothers this year" value={data.firstTimeMothersThisYear} />
        </div>
      </Section>

      <Section title="Productivity ratios">
        <dl className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          <Stat label="Replacement rate YTD" value={`${data.replacementRatePctYtd.toFixed(1)}%`} hint="Heifers + births vs breeding cows" />
          <Stat label="Calf mortality (YTD)" value={`${data.calfMortalityRatePct.toFixed(1)}%`} hint="Live births that died" />
          <Stat label="Breeding cows" value={data.breedingCows} hint="Female, age ≥ 2 yr, in-herd" />
        </dl>
      </Section>
    </div>
  );
}

function SignalTile({
  icon,
  tone,
  label,
  value,
}: {
  icon: React.ReactNode;
  tone: "success" | "warning" | "danger" | "info";
  label: string;
  value: number;
}) {
  const TONE: Record<typeof tone, string> = {
    success: "bg-emerald-50 text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-200 border-emerald-200/60",
    warning: "bg-amber-50 text-amber-800 dark:bg-amber-950/40 dark:text-amber-200 border-amber-200/60",
    danger: "bg-rose-50 text-rose-800 dark:bg-rose-950/40 dark:text-rose-200 border-rose-200/60",
    info: "bg-sky-50 text-sky-800 dark:bg-sky-950/40 dark:text-sky-200 border-sky-200/60",
  };
  return (
    <article className={cn("flex items-center gap-3 rounded-lg border p-3", TONE[tone])}>
      <span className="flex h-9 w-9 items-center justify-center rounded-md bg-card/70">{icon}</span>
      <div className="min-w-0">
        <p className="text-[10px] uppercase tracking-wide opacity-80">{label}</p>
        <p className="text-2xl font-bold tabular-nums">{value}</p>
      </div>
    </article>
  );
}

function Stat({ label, value, hint }: { label: string; value: React.ReactNode; hint?: string }) {
  return (
    <div className="rounded-lg border bg-muted/30 p-3">
      <p className="text-[10px] uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-1 text-xl font-bold tabular-nums">{value}</p>
      {hint && <p className="mt-1 text-[10px] text-muted-foreground">{hint}</p>}
    </div>
  );
}

// =====================================================================
// BULLS
// =====================================================================

type BullRow = {
  bull: { id: string; codeName: string; primaryName: string | null };
  dob: string;
  ageYears: number;
  offspringTotal: number;
  offspringAlive: number;
  offspringLost: number;
  calfSurvivalPct: number;
  daughtersBreedingAge: number;
  meanDaughterTier: number;
  daughtersTierAB: number;
  daughtersTierDE: number;
  lastSiredYear: number;
  verdict: string | null;
};

function BullsTab() {
  const { data, isLoading } = useQuery({
    queryKey: ["analytics", "intel", "bulls"],
    queryFn: () => api<BullRow[]>("/api/v1/analytics/intelligence/bulls"),
  });

  if (isLoading) return <Skeleton className="h-72 rounded-xl" />;
  if (!data || data.length === 0) {
    return (
      <Section>
        <EmptyState
          icon={<Crown className="h-5 w-5" />}
          title="No sires on file yet"
          description="As calvings are recorded with a known sire, each bull's report card appears here."
        />
      </Section>
    );
  }

  const totalSurviving = data.reduce((s, r) => s + r.offspringAlive, 0);

  return (
    <div className="space-y-4">
      <Section title="What this is" description="Each row is a bull, scored on the offspring he sired and how those daughters tier up.">
        <p className="text-xs text-muted-foreground">
          {data.length} bull{data.length === 1 ? "" : "s"} have sired{" "}
          <strong className="text-foreground">{totalSurviving}</strong> surviving offspring on file.
        </p>
      </Section>

      <ul className="grid gap-3 lg:grid-cols-2">
        {data.map((r) => (
          <li key={r.bull.id}>
            <Link
              href={`/animals/${r.bull.id}`}
              className="block rounded-xl border bg-card p-4 transition hover:shadow-md"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <h3 className="font-semibold leading-tight">{r.bull.primaryName ?? "(unnamed bull)"}</h3>
                  <p className="font-mono text-[11px] text-muted-foreground">
                    {r.bull.codeName} · {r.ageYears} yr · last sired {r.lastSiredYear}
                  </p>
                </div>
                {r.calfSurvivalPct >= 90 ? (
                  <Badge tone="success">strong sire</Badge>
                ) : r.calfSurvivalPct < 70 && r.offspringTotal >= 3 ? (
                  <Badge tone="danger">survival concern</Badge>
                ) : r.ageYears >= 7 ? (
                  <Badge tone="warning">retire soon</Badge>
                ) : (
                  <Badge tone="neutral">active</Badge>
                )}
              </div>

              <div className="mt-3 grid grid-cols-3 gap-2 text-center">
                <Mini label="Calves" value={`${r.offspringAlive}/${r.offspringTotal}`} />
                <Mini label="Survival" value={`${r.calfSurvivalPct.toFixed(0)}%`} />
                <Mini label="Daughters" value={r.daughtersBreedingAge} hint="of breeding age" />
              </div>

              <div className="mt-2 grid grid-cols-2 gap-2 text-xs">
                <span className="rounded-md bg-emerald-50 px-2 py-1 text-emerald-700 dark:bg-emerald-950/30 dark:text-emerald-300">
                  Tier A/B daughters: <strong>{r.daughtersTierAB}</strong>
                </span>
                <span className="rounded-md bg-rose-50 px-2 py-1 text-rose-700 dark:bg-rose-950/30 dark:text-rose-300">
                  Tier D/E daughters: <strong>{r.daughtersTierDE}</strong>
                </span>
              </div>

              {r.verdict && (
                <p className="mt-3 line-clamp-2 text-xs italic text-muted-foreground">{r.verdict}</p>
              )}
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}

function Mini({ label, value, hint }: { label: string; value: React.ReactNode; hint?: string }) {
  return (
    <div className="rounded-md bg-muted/40 p-2">
      <p className="text-[10px] uppercase text-muted-foreground">{label}</p>
      <p className="font-semibold tabular-nums">{value}</p>
      {hint && <p className="text-[9px] text-muted-foreground">{hint}</p>}
    </div>
  );
}

// =====================================================================
// BLOODLINES
// =====================================================================

type BloodlineRow = {
  sire: { id: string; codeName: string; primaryName: string | null };
  descendants: number;
  breedingAgeDescendants: number;
  meanTier: number;
  meanCpy: number;
  survivalPct: number;
};

function BloodlinesTab() {
  const { data, isLoading } = useQuery({
    queryKey: ["analytics", "intel", "bloodlines"],
    queryFn: () => api<BloodlineRow[]>("/api/v1/analytics/intelligence/bloodlines"),
  });

  if (isLoading) return <Skeleton className="h-72 rounded-xl" />;
  if (!data || data.length === 0) {
    return (
      <Section>
        <EmptyState
          icon={<GitBranch className="h-5 w-5" />}
          title="No bloodlines tracked yet"
          description="Once enough sires accumulate offspring, bloodline trends appear here."
        />
      </Section>
    );
  }

  return (
    <Section
      title="Paternal bloodlines"
      description="Mean descendant tier — lower number = better (1 = tier A; 5 = tier E)."
      icon={<GitBranch className="h-4 w-4" />}
      bodyClassName="p-0"
    >
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-border text-sm">
          <thead className="bg-muted/30 text-left text-[11px] uppercase tracking-wide text-muted-foreground">
            <tr>
              <th className="px-3 py-2">Sire line</th>
              <th className="px-3 py-2 text-right">Descendants</th>
              <th className="px-3 py-2 text-right">Breeding-age</th>
              <th className="px-3 py-2 text-right">Mean tier</th>
              <th className="px-3 py-2 text-right">Mean CPY</th>
              <th className="px-3 py-2 text-right">Survival</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {data.map((r) => (
              <tr key={r.sire.id} className="hover:bg-accent/40">
                <td className="px-3 py-2">
                  <Link href={`/animals/${r.sire.id}`} className="flex flex-col">
                    <span className="font-medium">{r.sire.primaryName ?? "(unnamed)"}</span>
                    <span className="font-mono text-[10px] text-muted-foreground">{r.sire.codeName}</span>
                  </Link>
                </td>
                <td className="px-3 py-2 text-right tabular-nums">{r.descendants}</td>
                <td className="px-3 py-2 text-right tabular-nums">{r.breedingAgeDescendants}</td>
                <td className="px-3 py-2 text-right tabular-nums">
                  {r.meanTier === 0 ? "—" : (
                    <span className={cn(
                      "rounded px-2 py-0.5 font-semibold",
                      r.meanTier <= 2 ? "bg-emerald-100 text-emerald-800" :
                      r.meanTier <= 3 ? "bg-amber-100 text-amber-800" :
                      "bg-rose-100 text-rose-800",
                    )}>
                      {r.meanTier.toFixed(2)}
                    </span>
                  )}
                </td>
                <td className="px-3 py-2 text-right tabular-nums">{r.meanCpy === 0 ? "—" : r.meanCpy.toFixed(2)}</td>
                <td className="px-3 py-2 text-right tabular-nums">{r.survivalPct.toFixed(0)}%</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Section>
  );
}

// =====================================================================
// YEARS
// =====================================================================

type YearRow = {
  year: number;
  calvesBorn: number;
  liveBirths: number;
  stillbirths: number;
  calfDeaths: number;
  calfSurvivalPct: number;
  cowsBredOrConfirmed: number;
  cowsThatCalved: number;
  pregnancySuccessPct: number;
  calvingRatePct: number;
  avgCalvingIntervalMonths: number;
  cowsSkippedYear: number;
  firstTimeCalvers: number;
  sales: number;
  deaths: number;
  netGrowth: number;
  topBull: { id: string; codeName: string; primaryName: string | null } | null;
  topBullSurvivingCalves: number;
  standoutCow: { id: string; codeName: string; primaryName: string | null } | null;
  narrative: string;
};

type YearReport = {
  years: YearRow[];
  bestYear: YearRow | null;
  worstYear: YearRow | null;
  bestYearLabel: number | null;
  worstYearLabel: number | null;
  narrative: string;
};

function YearsTab() {
  const { data, isLoading } = useQuery({
    queryKey: ["analytics", "intel", "years"],
    queryFn: () => api<YearReport>("/api/v1/analytics/intelligence/years"),
  });

  const [selectedYear, setSelectedYear] = useState<number | null>(null);

  const sorted = useMemo(() => (data?.years ?? []).slice().sort((a, b) => a.year - b.year), [data]);
  const focused = selectedYear
    ? sorted.find((r) => r.year === selectedYear) ?? null
    : sorted.at(-1) ?? null;

  if (isLoading) return <Skeleton className="h-96 rounded-xl" />;
  if (!data || data.years.length === 0) {
    return (
      <Section>
        <EmptyState
          icon={<CalendarRange className="h-5 w-5" />}
          title="No calving years on file yet"
          description="Record calvings and the year-by-year story builds itself."
        />
      </Section>
    );
  }

  return (
    <div className="space-y-4">
      <Section title="The story so far" icon={<CalendarRange className="h-4 w-4" />}>
        <p className="text-sm">{data.narrative}</p>
      </Section>

      <div className="grid gap-4 lg:grid-cols-2">
        <Section title="Live births per year">
          <div className="h-56">
            <ResponsiveContainer>
              <BarChart data={sorted} margin={{ top: 4, right: 4, left: -16, bottom: 4 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
                <XAxis dataKey="year" tick={{ fontSize: 11 }} />
                <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
                <Tooltip contentStyle={tooltipStyle} />
                <Bar dataKey="liveBirths" name="Live births" fill="#10b981" radius={[6, 6, 0, 0]} />
                <Bar dataKey="stillbirths" name="Stillbirths" fill="#f43f5e" radius={[6, 6, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </Section>

        <Section title="Calf survival % over time">
          <div className="h-56">
            <ResponsiveContainer>
              <LineChart data={sorted} margin={{ top: 4, right: 4, left: -16, bottom: 4 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
                <XAxis dataKey="year" tick={{ fontSize: 11 }} />
                <YAxis domain={[0, 100]} tick={{ fontSize: 11 }} unit="%" />
                <Tooltip contentStyle={tooltipStyle} />
                <Line type="monotone" dataKey="calfSurvivalPct" stroke="#10b981" strokeWidth={2} dot={{ r: 4 }} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </Section>
      </div>

      <div className="flex flex-wrap gap-2">
        {sorted.map((r) => {
          const active = (selectedYear ?? sorted.at(-1)?.year) === r.year;
          const isBest = data.bestYearLabel === r.year;
          const isWorst = data.worstYearLabel === r.year;
          return (
            <button
              key={r.year}
              type="button"
              onClick={() => setSelectedYear(r.year)}
              className={cn(
                "flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-xs transition",
                active ? "border-primary bg-primary text-primary-foreground shadow-xs" : "bg-card hover:bg-accent",
              )}
            >
              <span className="font-medium">{r.year}</span>
              <span className={cn("rounded-full px-1.5 text-[10px]", active ? "bg-primary-foreground/20" : "bg-muted")}>
                {r.calvesBorn}
              </span>
              {isBest && <Badge tone="success" size="sm">best</Badge>}
              {isWorst && <Badge tone="danger" size="sm">worst</Badge>}
            </button>
          );
        })}
      </div>

      {focused && <YearDetail row={focused} />}
    </div>
  );
}

function YearDetail({ row }: { row: YearRow }) {
  return (
    <Section title={`Year profile — ${row.year}`} icon={<CalendarRange className="h-4 w-4" />}>
      <p className="mb-4 text-sm italic text-muted-foreground">{row.narrative}</p>

      <div className="grid gap-3 sm:grid-cols-3">
        <Stat label="Live births" value={row.liveBirths} hint={`${row.stillbirths} stillborn`} />
        <Stat label="Calf survival" value={`${row.calfSurvivalPct.toFixed(1)}%`} hint={`${row.calfDeaths} calf loss${row.calfDeaths === 1 ? "" : "es"}`} />
        <Stat label="Net growth" value={row.netGrowth} hint={`+${row.liveBirths} births − ${row.sales} sales − ${row.deaths} deaths`} />
        <Stat label="Cows bred" value={row.cowsBredOrConfirmed} hint="Distinct cows serviced" />
        <Stat label="Pregnancy success" value={`${row.pregnancySuccessPct.toFixed(1)}%`} hint="Calved / bred" />
        <Stat label="Skipped year" value={row.cowsSkippedYear} hint="Cows that calved before and didn't this year or last" />
        <Stat label="First-time mothers" value={row.firstTimeCalvers} hint="Heifers calving for the first time" />
        <Stat label="Avg interval" value={`${row.avgCalvingIntervalMonths.toFixed(1)} mo`} hint="Among repeat calvers this year" />
        <Stat label="Calving rate" value={`${row.calvingRatePct.toFixed(1)}%`} hint="Births / breeding-age cows" />
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-2">
        {row.topBull && (
          <Link
            href={`/animals/${row.topBull.id}`}
            className="flex items-center justify-between gap-2 rounded-lg border bg-card p-3 hover:bg-accent/40"
          >
            <span>
              <span className="block text-[10px] uppercase tracking-wide text-muted-foreground">Top bull</span>
              <span className="block font-semibold">{row.topBull.primaryName ?? row.topBull.codeName}</span>
              <span className="text-xs text-muted-foreground">
                {row.topBullSurvivingCalves} surviving calf{row.topBullSurvivingCalves === 1 ? "" : "ren"}
              </span>
            </span>
            <ChevronRight className="h-4 w-4 text-muted-foreground" />
          </Link>
        )}
        {row.standoutCow && (
          <Link
            href={`/animals/${row.standoutCow.id}`}
            className="flex items-center justify-between gap-2 rounded-lg border bg-card p-3 hover:bg-accent/40"
          >
            <span>
              <span className="block text-[10px] uppercase tracking-wide text-muted-foreground">Standout cow</span>
              <span className="block font-semibold">{row.standoutCow.primaryName ?? row.standoutCow.codeName}</span>
              <span className="text-xs text-muted-foreground">Most contributed calves this year</span>
            </span>
            <ChevronRight className="h-4 w-4 text-muted-foreground" />
          </Link>
        )}
      </div>
    </Section>
  );
}

// =====================================================================
// INSIGHTS
// =====================================================================

type Insight = {
  code: string;
  severity: "info" | "success" | "warning" | "danger";
  title: string;
  body: string;
  subject: { id: string; codeName: string; primaryName: string | null } | null;
};

const SEV_CLASS: Record<Insight["severity"], string> = {
  info: "border-l-sky-400 bg-sky-50/50 dark:bg-sky-950/20",
  success: "border-l-emerald-400 bg-emerald-50/50 dark:bg-emerald-950/20",
  warning: "border-l-amber-400 bg-amber-50/50 dark:bg-amber-950/20",
  danger: "border-l-rose-500 bg-rose-50/50 dark:bg-rose-950/20",
};

function InsightsTab() {
  const { data, isLoading } = useQuery({
    queryKey: ["analytics", "intel", "insights"],
    queryFn: () => api<Insight[]>("/api/v1/analytics/intelligence/insights"),
  });

  if (isLoading) return <Skeleton className="h-72 rounded-xl" />;
  if (!data || data.length === 0) {
    return (
      <Section>
        <EmptyState
          icon={<Lightbulb className="h-5 w-5" />}
          title="No insights to surface right now"
          description="The rule engine has nothing to flag — herd looks healthy."
        />
      </Section>
    );
  }

  return (
    <ul className="space-y-2">
      {data.map((i, idx) => (
        <li
          key={idx}
          className={cn("rounded-xl border border-l-4 bg-card p-3 shadow-xs", SEV_CLASS[i.severity])}
        >
          <div className="flex flex-wrap items-baseline justify-between gap-2">
            <h3 className="text-sm font-semibold">{i.title}</h3>
            <Badge
              tone={
                i.severity === "success"
                  ? "success"
                  : i.severity === "warning"
                    ? "warning"
                    : i.severity === "danger"
                      ? "danger"
                      : "info"
              }
            >
              {i.code.replace(/_/g, " ")}
            </Badge>
          </div>
          <p className="mt-1 text-sm text-muted-foreground">{i.body}</p>
          {i.subject && (
            <Link
              href={`/animals/${i.subject.id}`}
              className="mt-2 inline-flex items-center gap-1 text-xs font-medium text-primary hover:underline"
            >
              Open {i.subject.primaryName ?? i.subject.codeName}{" "}
              <ChevronRight className="h-3 w-3" />
            </Link>
          )}
        </li>
      ))}
    </ul>
  );
}
