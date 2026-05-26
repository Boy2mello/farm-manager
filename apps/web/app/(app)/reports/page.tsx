"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
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
  Legend,
  CartesianGrid,
} from "recharts";
import {
  FileText,
  Users,
  Trophy,
  AlertOctagon,
  Search,
  ArrowRight,
  Printer,
} from "lucide-react";
import { api } from "@/lib/api/client";
import { PageHeader } from "@/components/ui/page-header";
import { Section } from "@/components/ui/section";
import { EmptyState } from "@/components/ui/empty-state";
import {
  TierBadge,
  StatusPill,
  SexChip,
  STATUS_LABELS,
} from "@/components/ui/badges";
import { Skeleton } from "@/components/ui/skeleton";

type Tab = "census" | "ranking" | "cull";

const TABS: Array<{ key: Tab; label: string; icon: React.ComponentType<{ className?: string }> }> = [
  { key: "census", label: "Herd Census", icon: Users },
  { key: "ranking", label: "Performance Ranking", icon: Trophy },
  { key: "cull", label: "Cull Candidates", icon: AlertOctagon },
];

export default function ReportsPage() {
  const [tab, setTab] = useState<Tab>("census");

  return (
    <div className="space-y-5 animate-fade-in">
      <PageHeader
        icon={<FileText className="h-6 w-6" />}
        title="Reports"
        description="Live, on-screen reports built from your real data. Drill into any row."
        actions={
          <button
            type="button"
            onClick={() => window.print()}
            className="inline-flex items-center gap-1.5 rounded-md border bg-card px-3 py-1.5 text-sm font-medium hover:bg-accent"
          >
            <Printer className="h-4 w-4" />
            Print
          </button>
        }
      />

      <div className="flex gap-1 overflow-x-auto rounded-lg border bg-card p-1">
        {TABS.map(({ key, label, icon: Icon }) => {
          const active = tab === key;
          return (
            <button
              key={key}
              type="button"
              onClick={() => setTab(key)}
              className={`flex flex-1 items-center justify-center gap-1.5 whitespace-nowrap rounded-md px-3 py-2 text-sm font-medium transition ${
                active
                  ? "bg-primary text-primary-foreground shadow-xs"
                  : "text-muted-foreground hover:bg-accent"
              }`}
            >
              <Icon className="h-4 w-4" />
              {label}
            </button>
          );
        })}
      </div>

      {tab === "census" && <HerdCensus />}
      {tab === "ranking" && <PerformanceRanking />}
      {tab === "cull" && <CullCandidates />}
    </div>
  );
}

// =====================================================================
// HERD CENSUS
// =====================================================================

type HerdCensusRow = {
  animalId: string;
  codeName: string;
  primaryName: string | null;
  sex: 1 | 2;
  dob: string;
  status: number;
  performanceTier: 0 | 1 | 2 | 3 | 4 | 5;
  isBSired: boolean;
  damName: string | null;
  sireName: string | null;
};
type HerdCensusReport = {
  asOfDate: string;
  totalLive: number;
  females: number;
  males: number;
  bSired: number;
  rows: HerdCensusRow[];
};

const STATUS_COLOURS: Record<number, string> = {
  1: "#10b981",
  2: "#0ea5e9",
  3: "#a855f7",
  4: "#ec4899",
  5: "#f59e0b",
  6: "#64748b",
};

const TIER_COLOURS = ["#94a3b8", "#10b981", "#84cc16", "#f59e0b", "#f97316", "#f43f5e"];

function HerdCensus() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["report", "herd-census"],
    queryFn: () => api<HerdCensusReport>("/api/v1/reports/herd-census"),
  });

  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<number | null>(null);

  const filtered = useMemo(() => {
    if (!data) return [];
    const q = search.trim().toLowerCase();
    return data.rows.filter((r) => {
      if (q && !`${r.codeName} ${r.primaryName ?? ""}`.toLowerCase().includes(q)) return false;
      if (statusFilter !== null && r.status !== statusFilter) return false;
      return true;
    });
  }, [data, search, statusFilter]);

  const statusBreakdown = useMemo(() => {
    if (!data) return [];
    const m = new Map<number, number>();
    data.rows.forEach((r) => m.set(r.status, (m.get(r.status) ?? 0) + 1));
    return Array.from(m.entries()).map(([status, count]) => ({
      name: STATUS_LABELS[status] ?? "?",
      value: count,
      fill: STATUS_COLOURS[status] ?? "#94a3b8",
    }));
  }, [data]);

  const tierBreakdown = useMemo(() => {
    if (!data) return [];
    const m = new Map<number, number>();
    data.rows.forEach((r) => m.set(r.performanceTier, (m.get(r.performanceTier) ?? 0) + 1));
    return [0, 1, 2, 3, 4, 5].map((t) => ({
      tier: t === 0 ? "Unranked" : `Tier ${["—", "A", "B", "C", "D", "E"][t]}`,
      count: m.get(t) ?? 0,
      fill: TIER_COLOURS[t],
    }));
  }, [data]);

  const ageBuckets = useMemo(() => {
    if (!data) return [];
    const buckets = [
      { label: "< 1y", min: 0, max: 1, count: 0 },
      { label: "1–2y", min: 1, max: 2, count: 0 },
      { label: "2–4y", min: 2, max: 4, count: 0 },
      { label: "4–6y", min: 4, max: 6, count: 0 },
      { label: "6y+", min: 6, max: 999, count: 0 },
    ];
    const today = new Date();
    data.rows.forEach((r) => {
      const dob = new Date(r.dob);
      const years = (today.getTime() - dob.getTime()) / (365.25 * 24 * 3600 * 1000);
      const b = buckets.find((x) => years >= x.min && years < x.max);
      if (b) b.count++;
    });
    return buckets.map((b) => ({ name: b.label, count: b.count }));
  }, [data]);

  if (isLoading) return <ReportSkeleton />;
  if (error) return <p className="text-sm text-destructive">Unable to load report.</p>;
  if (!data) return null;

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <StatTile label="Live animals" value={data.totalLive} colour="bg-emerald-500" />
        <StatTile label="Females" value={data.females} colour="bg-pink-500" />
        <StatTile label="Males" value={data.males} colour="bg-sky-500" />
        <StatTile label="(B)-sired" value={data.bSired} colour="bg-amber-500" />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <Section title="By status" icon={<Users className="h-4 w-4" />}>
          <div className="h-56">
            <ResponsiveContainer>
              <PieChart>
                <Pie
                  data={statusBreakdown}
                  dataKey="value"
                  nameKey="name"
                  innerRadius={45}
                  outerRadius={75}
                  paddingAngle={2}
                >
                  {statusBreakdown.map((s, i) => (
                    <Cell key={i} fill={s.fill} stroke="hsl(var(--card))" strokeWidth={2} />
                  ))}
                </Pie>
                <Tooltip
                  contentStyle={tooltipStyle}
                  formatter={(v: number) => [`${v} animals`, ""]}
                />
                <Legend wrapperStyle={{ fontSize: 11 }} iconType="circle" />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </Section>

        <Section title="By tier" icon={<Trophy className="h-4 w-4" />}>
          <div className="h-56">
            <ResponsiveContainer>
              <BarChart data={tierBreakdown} margin={{ top: 4, right: 4, left: -16, bottom: 4 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
                <XAxis dataKey="tier" tick={{ fontSize: 11 }} />
                <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
                <Tooltip contentStyle={tooltipStyle} />
                <Bar dataKey="count" radius={[6, 6, 0, 0]}>
                  {tierBreakdown.map((entry, i) => (
                    <Cell key={i} fill={entry.fill} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </Section>

        <Section title="Age distribution" icon={<Users className="h-4 w-4" />}>
          <div className="h-56">
            <ResponsiveContainer>
              <BarChart data={ageBuckets} margin={{ top: 4, right: 4, left: -16, bottom: 4 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
                <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
                <Tooltip contentStyle={tooltipStyle} />
                <Bar dataKey="count" fill="hsl(var(--primary))" radius={[6, 6, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </Section>
      </div>

      <Section
        title={`Animals — ${filtered.length}`}
        description="Tap any row to open the profile"
        icon={<Users className="h-4 w-4" />}
        bodyClassName="p-0"
      >
        <div className="flex flex-wrap items-center gap-2 border-b p-3">
          <div className="relative min-w-[200px] flex-1">
            <Search className="pointer-events-none absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search name or code"
              className="w-full rounded-md border bg-background py-2 pl-8 pr-3 text-sm"
            />
          </div>
          <div className="flex gap-1.5 text-xs">
            {([null, 1, 4, 5, 2] as Array<number | null>).map((s) => (
              <button
                key={s ?? "all"}
                onClick={() => setStatusFilter(s)}
                className={`rounded-full border px-2.5 py-1 ${
                  statusFilter === s
                    ? "border-primary bg-primary text-primary-foreground"
                    : "bg-background hover:bg-accent"
                }`}
              >
                {s === null ? "All" : STATUS_LABELS[s] ?? s}
              </button>
            ))}
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-border text-sm">
            <thead className="bg-muted/30 text-left text-[11px] uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-3 py-2">Code · Name</th>
                <th className="px-3 py-2">Sex</th>
                <th className="px-3 py-2">DOB</th>
                <th className="px-3 py-2">Status</th>
                <th className="px-3 py-2">Tier</th>
                <th className="px-3 py-2">Dam</th>
                <th className="px-3 py-2">Sire</th>
                <th />
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {filtered.map((r) => (
                <tr key={r.animalId} className="group transition hover:bg-accent/40">
                  <td className="px-3 py-2">
                    <Link href={`/animals/${r.animalId}`} className="flex flex-col">
                      <span className="font-medium">{r.primaryName ?? "—"}</span>
                      <span className="font-mono text-[11px] text-muted-foreground">{r.codeName}</span>
                    </Link>
                    {r.isBSired && (
                      <span className="ml-1 rounded bg-yellow-200 px-1 text-[10px] font-bold uppercase text-yellow-900">
                        B
                      </span>
                    )}
                  </td>
                  <td className="px-3 py-2">
                    <SexChip sex={r.sex} />
                  </td>
                  <td className="px-3 py-2 text-xs text-muted-foreground">{r.dob}</td>
                  <td className="px-3 py-2">
                    <StatusPill status={r.status} />
                  </td>
                  <td className="px-3 py-2">
                    <TierBadge tier={r.performanceTier} size="sm" />
                  </td>
                  <td className="px-3 py-2 text-xs text-muted-foreground">{r.damName ?? "—"}</td>
                  <td className="px-3 py-2 text-xs text-muted-foreground">{r.sireName ?? "—"}</td>
                  <td className="px-3 py-2 text-right">
                    <Link
                      href={`/animals/${r.animalId}`}
                      className="text-primary opacity-60 transition group-hover:opacity-100"
                      aria-label="Open animal"
                    >
                      <ArrowRight className="h-3.5 w-3.5" />
                    </Link>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={8} className="px-3 py-6 text-center text-sm text-muted-foreground">
                    No animals match.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Section>

      <AsOf date={data.asOfDate} />
    </div>
  );
}

// =====================================================================
// PERFORMANCE RANKING
// =====================================================================

type RankingRow = {
  animalId: string;
  codeName: string;
  primaryName: string | null;
  dob: string;
  ageYears: number;
  calfCount: number;
  calvesAlive: number;
  calvesPerYear: number | null;
  avgCalvingIntervalDays: number | null;
  lastCalvingDate: string | null;
  status: number;
  tier: 0 | 1 | 2 | 3 | 4 | 5;
  reason: string | null;
};
type RankingReport = {
  asOfDate: string;
  tierCounts: Record<string, number>;
  rows: RankingRow[];
};

const TIER_DESCRIPTIONS: Record<string, { action: string; tone: string; advice: string }> = {
  A: { action: "Keep · prioritise breeding", tone: "bg-emerald-50 text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-100", advice: "Top performers. Breed every cycle." },
  B: { action: "Keep", tone: "bg-lime-50 text-lime-800 dark:bg-lime-950/40 dark:text-lime-100", advice: "Solid contributors." },
  C: { action: "Monitor", tone: "bg-amber-50 text-amber-800 dark:bg-amber-950/40 dark:text-amber-100", advice: "Average — watch next calving interval." },
  D: { action: "Watch — borderline cull", tone: "bg-orange-50 text-orange-800 dark:bg-orange-950/40 dark:text-orange-100", advice: "Cull if next interval > 18 months." },
  E: { action: "Cull candidate", tone: "bg-rose-50 text-rose-800 dark:bg-rose-950/40 dark:text-rose-100", advice: "Confirm pregnant or sell now." },
  "—": { action: "Unranked / first-time mothers", tone: "bg-muted text-muted-foreground", advice: "Tier deferred for one cycle." },
};

function PerformanceRanking() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["report", "performance-ranking"],
    queryFn: () => api<RankingReport>("/api/v1/reports/performance-ranking"),
  });

  if (isLoading) return <ReportSkeleton />;
  if (error) return <p className="text-sm text-destructive">Unable to load report.</p>;
  if (!data) return null;

  const tierLabels = ["A", "B", "C", "D", "E", "None"] as const;
  const tierIndexFor = (t: number) => ["—", "A", "B", "C", "D", "E"][t];
  const grouped = new Map<string, RankingRow[]>();
  for (const r of data.rows) {
    const key = tierIndexFor(r.tier);
    if (!grouped.has(key)) grouped.set(key, []);
    grouped.get(key)!.push(r);
  }

  const donutData = [1, 2, 3, 4, 5].map((t) => {
    const label = ["—", "A", "B", "C", "D", "E"][t];
    return {
      name: `Tier ${label}`,
      value: data.tierCounts[label] ?? 0,
      fill: TIER_COLOURS[t],
    };
  });

  return (
    <div className="space-y-4">
      <div className="grid gap-4 md:grid-cols-[1fr_2fr]">
        <Section title="Tier mix" icon={<Trophy className="h-4 w-4" />}>
          <div className="h-56">
            <ResponsiveContainer>
              <PieChart>
                <Pie
                  data={donutData}
                  dataKey="value"
                  nameKey="name"
                  innerRadius={45}
                  outerRadius={75}
                  paddingAngle={2}
                >
                  {donutData.map((entry, i) => (
                    <Cell key={i} fill={entry.fill} stroke="hsl(var(--card))" strokeWidth={2} />
                  ))}
                </Pie>
                <Tooltip contentStyle={tooltipStyle} />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </Section>

        <Section title="At a glance" icon={<Trophy className="h-4 w-4" />} bodyClassName="p-3">
          <div className="grid gap-2 sm:grid-cols-3">
            {(["A", "B", "C", "D", "E"] as const).map((t) => {
              const count = data.tierCounts[t] ?? 0;
              const meta = TIER_DESCRIPTIONS[t];
              const idx = ["—", "A", "B", "C", "D", "E"].indexOf(t) as 0 | 1 | 2 | 3 | 4 | 5;
              return (
                <div key={t} className={`flex items-center gap-3 rounded-lg px-3 py-2.5 ${meta.tone}`}>
                  <TierBadge tier={idx} />
                  <div className="min-w-0">
                    <p className="text-sm font-semibold">{count} cow{count === 1 ? "" : "s"}</p>
                    <p className="truncate text-xs opacity-80">{meta.action}</p>
                  </div>
                </div>
              );
            })}
          </div>
        </Section>
      </div>

      {tierLabels.map((labelKey) => {
        const k = labelKey === "None" ? "—" : labelKey;
        const cows = grouped.get(k);
        if (!cows || cows.length === 0) return null;
        const meta = TIER_DESCRIPTIONS[k];
        const idx = ["—", "A", "B", "C", "D", "E"].indexOf(k) as 0 | 1 | 2 | 3 | 4 | 5;
        return (
          <Section
            key={labelKey}
            title={
              <span className="flex items-center gap-2">
                <TierBadge tier={idx} />
                <span>{meta.action}</span>
                <span className="rounded-full bg-muted px-2 py-0.5 text-[10px] font-medium text-muted-foreground">
                  {cows.length}
                </span>
              </span>
            }
            description={meta.advice}
            bodyClassName="p-0"
          >
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-border text-sm">
                <thead className="bg-muted/30 text-left text-[11px] uppercase tracking-wide text-muted-foreground">
                  <tr>
                    <th className="px-3 py-2">Cow</th>
                    <th className="px-3 py-2 text-right">Age</th>
                    <th className="px-3 py-2 text-right">Calves</th>
                    <th className="px-3 py-2 text-right">CPY</th>
                    <th className="px-3 py-2 text-right">Interval (mo)</th>
                    <th className="px-3 py-2">Last calving</th>
                    <th className="px-3 py-2">Status</th>
                    <th className="px-3 py-2">Why</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {cows.map((r) => (
                    <tr key={r.animalId} className="hover:bg-accent/40">
                      <td className="px-3 py-2">
                        <Link href={`/animals/${r.animalId}`}>
                          <span className="font-medium">{r.primaryName ?? "—"}</span>
                          <span className="ml-2 font-mono text-[10px] text-muted-foreground">{r.codeName}</span>
                        </Link>
                      </td>
                      <td className="px-3 py-2 text-right tabular-nums">{r.ageYears.toFixed(1)}</td>
                      <td className="px-3 py-2 text-right tabular-nums">
                        {r.calfCount}{" "}
                        <span className="text-xs text-muted-foreground">({r.calvesAlive})</span>
                      </td>
                      <td className="px-3 py-2 text-right tabular-nums">
                        {r.calvesPerYear?.toFixed(2) ?? "—"}
                      </td>
                      <td className="px-3 py-2 text-right tabular-nums">
                        {r.avgCalvingIntervalDays != null
                          ? (Number(r.avgCalvingIntervalDays) / 30.4375).toFixed(1)
                          : "—"}
                      </td>
                      <td className="px-3 py-2 text-xs text-muted-foreground">{r.lastCalvingDate ?? "—"}</td>
                      <td className="px-3 py-2">
                        <StatusPill status={r.status} />
                      </td>
                      <td className="px-3 py-2 max-w-xs text-xs text-muted-foreground">{r.reason ?? "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Section>
        );
      })}

      <AsOf date={data.asOfDate} />
    </div>
  );
}

// =====================================================================
// CULL CANDIDATES
// =====================================================================

type CullRow = {
  animalId: string;
  codeName: string;
  primaryName: string | null;
  dob: string;
  ageYears: number;
  calfCount: number;
  calvesAlive: number;
  calvesPerYear: number | null;
  status: number;
  reason: string | null;
  activeFlags: string[];
};
type CullReport = { asOfDate: string; count: number; rows: CullRow[] };

function CullCandidates() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["report", "cull-candidates"],
    queryFn: () => api<CullReport>("/api/v1/reports/cull-candidates"),
  });

  if (isLoading) return <ReportSkeleton />;
  if (error) return <p className="text-sm text-destructive">Unable to load report.</p>;
  if (!data) return null;

  if (data.count === 0) {
    return (
      <Section title="Cull candidates" icon={<AlertOctagon className="h-4 w-4" />}>
        <EmptyState
          icon={<AlertOctagon className="h-5 w-5" />}
          title="No cull candidates today"
          description="Tier E is empty — nothing flagged for culling. The nightly tier engine will reassess overnight."
        />
      </Section>
    );
  }

  const flagFreq = new Map<string, number>();
  data.rows.forEach((r) => r.activeFlags.forEach((f) => flagFreq.set(f, (flagFreq.get(f) ?? 0) + 1)));
  const topFlag = Array.from(flagFreq.entries()).sort((a, b) => b[1] - a[1])[0];

  return (
    <div className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-3">
        <StatTile label="Tier E cows" value={data.count} colour="bg-rose-500" />
        <StatTile
          label="Most common reason"
          value={topFlag ? topFlag[0].replace(/_/g, " ") : "—"}
          colour="bg-amber-500"
          isText
        />
        <StatTile
          label="Avg calves alive"
          value={
            data.rows.length === 0
              ? 0
              : (
                  data.rows.reduce((s, r) => s + r.calvesAlive, 0) / data.rows.length
                ).toFixed(1)
          }
          colour="bg-slate-500"
        />
      </div>

      <ul className="grid gap-3 lg:grid-cols-2">
        {data.rows.map((r) => (
          <li key={r.animalId}>
            <Link
              href={`/animals/${r.animalId}`}
              className="block rounded-xl border bg-card p-4 transition hover:border-rose-300 hover:shadow-md"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="flex min-w-0 items-center gap-3">
                  <span className="tier-badge tier-badge-E h-8 w-8 text-sm">E</span>
                  <div className="min-w-0">
                    <p className="truncate font-semibold">{r.primaryName ?? "—"}</p>
                    <p className="truncate font-mono text-[11px] text-muted-foreground">{r.codeName}</p>
                  </div>
                </div>
                <StatusPill status={r.status} />
              </div>

              <div className="mt-3 grid grid-cols-3 gap-2 text-center">
                <Metric label="Age" value={`${r.ageYears.toFixed(1)}y`} />
                <Metric
                  label="Calves"
                  value={
                    <>
                      {r.calfCount}{" "}
                      <span className="text-xs text-muted-foreground">({r.calvesAlive})</span>
                    </>
                  }
                />
                <Metric label="CPY" value={r.calvesPerYear?.toFixed(2) ?? "—"} />
              </div>

              {r.activeFlags.length > 0 && (
                <ul className="mt-3 flex flex-wrap gap-1">
                  {r.activeFlags.map((f) => (
                    <li
                      key={f}
                      className="rounded-full bg-rose-100 px-2 py-0.5 text-[10px] font-medium text-rose-700 dark:bg-rose-950/40 dark:text-rose-300"
                    >
                      {f.replace(/_/g, " ")}
                    </li>
                  ))}
                </ul>
              )}

              {r.reason && (
                <p className="mt-3 line-clamp-2 text-xs italic text-muted-foreground">{r.reason}</p>
              )}
            </Link>
          </li>
        ))}
      </ul>

      <AsOf date={data.asOfDate} />
    </div>
  );
}

// =====================================================================
// Helpers
// =====================================================================

const tooltipStyle = {
  background: "hsl(var(--popover))",
  border: "1px solid hsl(var(--border))",
  borderRadius: 8,
  fontSize: 12,
  padding: "6px 10px",
  color: "hsl(var(--popover-foreground))",
};

function Metric({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="rounded-md bg-muted/40 p-2">
      <p className="text-[10px] uppercase text-muted-foreground">{label}</p>
      <p className="font-semibold tabular-nums">{value}</p>
    </div>
  );
}

function StatTile({
  label,
  value,
  colour,
  isText,
}: {
  label: string;
  value: number | string;
  colour: string;
  isText?: boolean;
}) {
  return (
    <article className="flex items-center gap-3 rounded-xl border bg-card p-3">
      <span className={`h-9 w-1.5 rounded-full ${colour}`} aria-hidden />
      <div className="min-w-0">
        <p className="text-[10px] font-medium uppercase tracking-wide text-muted-foreground">
          {label}
        </p>
        <p
          className={`mt-0.5 truncate ${
            isText ? "text-base font-semibold" : "text-2xl font-bold tabular-nums"
          }`}
        >
          {value}
        </p>
      </div>
    </article>
  );
}

function ReportSkeleton() {
  return (
    <div className="space-y-3">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} className="h-16 rounded-xl" />
        ))}
      </div>
      <div className="grid gap-3 lg:grid-cols-3">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-56 rounded-xl" />
        ))}
      </div>
      <Skeleton className="h-64 rounded-xl" />
    </div>
  );
}

function AsOf({ date }: { date: string }) {
  return (
    <p className="text-right text-xs text-muted-foreground">
      As of <time dateTime={date}>{date}</time>
    </p>
  );
}
