"use client";

import Link from "next/link";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api/client";
import { cn } from "@/lib/utils";

type Sex = 1 | 2;
type Status = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10;
type Tier = 0 | 1 | 2 | 3 | 4 | 5;

const STATUS_LABEL: Record<number, string> = {
  1: "Active", 2: "Open", 3: "Exposed", 4: "Pregnant", 5: "Lactating",
  6: "Dry", 7: "Sold", 8: "Dead", 9: "Missing", 10: "Transferred",
};
const TIER_LABEL = ["—", "A", "B", "C", "D", "E"];
const TIER_CLASS = [
  "bg-muted text-muted-foreground",
  "tier-badge-A",
  "tier-badge-B",
  "tier-badge-C",
  "tier-badge-D",
  "tier-badge-E",
];

type Tab = "census" | "ranking" | "cull";

export default function ReportsPage() {
  const [tab, setTab] = useState<Tab>("census");

  return (
    <section className="space-y-4">
      <header>
        <h1 className="text-2xl font-bold">Reports</h1>
        <p className="text-xs text-muted-foreground">
          Live data, on-screen. Tap any row to open the animal profile.
        </p>
      </header>

      <nav className="flex gap-2 overflow-x-auto border-b text-sm">
        <TabButton active={tab === "census"} onClick={() => setTab("census")}>Herd Census</TabButton>
        <TabButton active={tab === "ranking"} onClick={() => setTab("ranking")}>Performance Ranking</TabButton>
        <TabButton active={tab === "cull"} onClick={() => setTab("cull")}>Cull Candidates</TabButton>
      </nav>

      {tab === "census" && <HerdCensus />}
      {tab === "ranking" && <PerformanceRanking />}
      {tab === "cull" && <CullCandidates />}
    </section>
  );
}

function TabButton({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      onClick={onClick}
      className={cn(
        "border-b-2 px-3 py-2 text-sm",
        active
          ? "border-primary font-medium text-primary"
          : "border-transparent text-muted-foreground hover:text-foreground",
      )}
    >
      {children}
    </button>
  );
}

// =============================================================
// Herd Census
// =============================================================

type HerdCensusRow = {
  animalId: string;
  codeName: string;
  primaryName: string | null;
  sex: Sex;
  dob: string;
  status: Status;
  performanceTier: Tier;
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

function HerdCensus() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["report", "herd-census"],
    queryFn: () => api<HerdCensusReport>("/api/v1/reports/herd-census"),
  });

  if (isLoading) return <Loading />;
  if (error) return <ErrorState error={error} />;
  if (!data) return null;

  return (
    <article className="space-y-3">
      <KpiRow>
        <Kpi label="Live animals" value={data.totalLive} />
        <Kpi label="Females" value={data.females} />
        <Kpi label="Males" value={data.males} />
        <Kpi label="B-sired" value={data.bSired} />
      </KpiRow>

      <div className="overflow-x-auto rounded-md border bg-card">
        <table className="min-w-full divide-y divide-border text-sm">
          <thead className="bg-muted/40 text-left text-[11px] uppercase tracking-wide text-muted-foreground">
            <tr>
              <th className="px-3 py-2">Code-name</th>
              <th className="px-3 py-2">Name</th>
              <th className="px-3 py-2">Sex</th>
              <th className="px-3 py-2">DOB</th>
              <th className="px-3 py-2">Status</th>
              <th className="px-3 py-2">Tier</th>
              <th className="px-3 py-2">Dam</th>
              <th className="px-3 py-2">Sire</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {data.rows.map((r) => (
              <tr key={r.animalId} className="hover:bg-accent/40">
                <td className="px-3 py-2">
                  <Link href={`/animals/${r.animalId}`} className="font-mono text-xs">
                    {r.codeName}
                  </Link>
                  {r.isBSired && (
                    <span className="ml-2 rounded bg-yellow-200 px-1 text-[10px] font-bold uppercase text-yellow-900">
                      B
                    </span>
                  )}
                </td>
                <td className="px-3 py-2">{r.primaryName ?? "—"}</td>
                <td className="px-3 py-2">{r.sex === 1 ? "F" : "M"}</td>
                <td className="px-3 py-2 text-xs text-muted-foreground">{r.dob}</td>
                <td className="px-3 py-2 text-xs">{STATUS_LABEL[r.status] ?? r.status}</td>
                <td className="px-3 py-2">
                  <span className={cn("tier-badge", TIER_CLASS[r.performanceTier])}>{TIER_LABEL[r.performanceTier]}</span>
                </td>
                <td className="px-3 py-2 text-xs text-muted-foreground">{r.damName ?? "—"}</td>
                <td className="px-3 py-2 text-xs text-muted-foreground">{r.sireName ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <AsOf date={data.asOfDate} />
    </article>
  );
}

// =============================================================
// Performance Ranking
// =============================================================

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
  status: Status;
  tier: Tier;
  reason: string | null;
};
type RankingReport = {
  asOfDate: string;
  tierCounts: Record<string, number>;
  rows: RankingRow[];
};

function PerformanceRanking() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["report", "performance-ranking"],
    queryFn: () => api<RankingReport>("/api/v1/reports/performance-ranking"),
  });

  if (isLoading) return <Loading />;
  if (error) return <ErrorState error={error} />;
  if (!data) return null;

  const tiers = ["A", "B", "C", "D", "E", "None"] as const;
  const grouped = new Map<string, RankingRow[]>();
  for (const r of data.rows) {
    const key = TIER_LABEL[r.tier];
    if (!grouped.has(key)) grouped.set(key, []);
    grouped.get(key)!.push(r);
  }

  return (
    <article className="space-y-3">
      <KpiRow>
        {(["A", "B", "C", "D", "E"] as const).map((t) => (
          <Kpi key={t} label={`Tier ${t}`} value={data.tierCounts[t] ?? 0} accent={`tier-badge-${t}`} />
        ))}
      </KpiRow>

      {tiers.map((tierKey) => {
        const cows = grouped.get(tierKey === "None" ? "—" : tierKey);
        if (!cows || cows.length === 0) return null;
        const isUnranked = tierKey === "None";
        return (
          <section key={tierKey} className="rounded-md border bg-card">
            <header className="flex items-center justify-between border-b bg-muted/40 px-3 py-2">
              <span className="flex items-center gap-2">
                <span className={cn("tier-badge", isUnranked ? TIER_CLASS[0] : `tier-badge-${tierKey}`)}>
                  {isUnranked ? "—" : tierKey}
                </span>
                <span className="text-sm font-medium">
                  {isUnranked
                    ? "Unranked / first-time mothers"
                    : tierKey === "A"
                      ? "Top performers — keep, prioritise breeding"
                      : tierKey === "B"
                        ? "Good — keep"
                        : tierKey === "C"
                          ? "Average — monitor next interval"
                          : tierKey === "D"
                            ? "Watch — cull if next interval > 18 mo"
                            : "Cull candidates — confirm pregnant or sell"}
                </span>
              </span>
              <span className="text-xs text-muted-foreground">{cows.length}</span>
            </header>

            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-border text-sm">
                <thead className="text-left text-[11px] uppercase tracking-wide text-muted-foreground">
                  <tr>
                    <th className="px-3 py-2">Code-name</th>
                    <th className="px-3 py-2">Name</th>
                    <th className="px-3 py-2 text-right">Age</th>
                    <th className="px-3 py-2 text-right">Calves</th>
                    <th className="px-3 py-2 text-right">CPY</th>
                    <th className="px-3 py-2 text-right">Interval (mo)</th>
                    <th className="px-3 py-2">Last calving</th>
                    <th className="px-3 py-2">Status</th>
                    <th className="px-3 py-2">Reason</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {cows.map((r) => (
                    <tr key={r.animalId} className="hover:bg-accent/40">
                      <td className="px-3 py-2">
                        <Link href={`/animals/${r.animalId}`} className="font-mono text-xs">
                          {r.codeName}
                        </Link>
                      </td>
                      <td className="px-3 py-2">{r.primaryName ?? "—"}</td>
                      <td className="px-3 py-2 text-right">{r.ageYears.toFixed(1)}</td>
                      <td className="px-3 py-2 text-right">
                        {r.calfCount}{" "}
                        <span className="text-xs text-muted-foreground">({r.calvesAlive} alive)</span>
                      </td>
                      <td className="px-3 py-2 text-right">{r.calvesPerYear?.toFixed(2) ?? "—"}</td>
                      <td className="px-3 py-2 text-right">
                        {r.avgCalvingIntervalDays != null ? (Number(r.avgCalvingIntervalDays) / 30.4375).toFixed(1) : "—"}
                      </td>
                      <td className="px-3 py-2 text-xs text-muted-foreground">{r.lastCalvingDate ?? "—"}</td>
                      <td className="px-3 py-2 text-xs">{STATUS_LABEL[r.status] ?? r.status}</td>
                      <td className="px-3 py-2 max-w-xs text-xs text-muted-foreground">{r.reason ?? "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        );
      })}
      <AsOf date={data.asOfDate} />
    </article>
  );
}

// =============================================================
// Cull Candidates
// =============================================================

type CullRow = {
  animalId: string;
  codeName: string;
  primaryName: string | null;
  dob: string;
  ageYears: number;
  calfCount: number;
  calvesAlive: number;
  calvesPerYear: number | null;
  status: Status;
  reason: string | null;
  activeFlags: string[];
};
type CullReport = { asOfDate: string; count: number; rows: CullRow[] };

function CullCandidates() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["report", "cull-candidates"],
    queryFn: () => api<CullReport>("/api/v1/reports/cull-candidates"),
  });

  if (isLoading) return <Loading />;
  if (error) return <ErrorState error={error} />;
  if (!data) return null;

  if (data.count === 0) {
    return (
      <article className="rounded-md border bg-card p-4 text-sm">
        <p className="font-semibold">No cull candidates today.</p>
        <p className="text-muted-foreground">
          Tier E is empty — the nightly tier engine hasn't flagged any cow for cull.
        </p>
      </article>
    );
  }

  return (
    <article className="space-y-3">
      <KpiRow>
        <Kpi label="Tier E cows" value={data.count} accent="tier-badge-E" />
      </KpiRow>

      <ul className="space-y-2">
        {data.rows.map((r) => (
          <li key={r.animalId} className="rounded-md border bg-card p-3">
            <Link href={`/animals/${r.animalId}`} className="block space-y-2">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="code-name-badge">{r.codeName}</span>
                  <span className="tier-badge tier-badge-E">E</span>
                  <span className="font-semibold">{r.primaryName ?? "—"}</span>
                </div>
                <span className="text-xs text-muted-foreground">
                  {STATUS_LABEL[r.status]} · age {r.ageYears.toFixed(1)}
                </span>
              </div>
              <div className="grid grid-cols-3 gap-2 text-xs text-muted-foreground">
                <span>
                  Calves: <strong className="text-foreground">{r.calfCount}</strong> ({r.calvesAlive} alive)
                </span>
                <span>
                  CPY: <strong className="text-foreground">{r.calvesPerYear?.toFixed(2) ?? "—"}</strong>
                </span>
                <span>
                  DOB: <strong className="text-foreground">{r.dob}</strong>
                </span>
              </div>
              {r.activeFlags.length > 0 && (
                <ul className="flex flex-wrap gap-1 text-[10px]">
                  {r.activeFlags.map((f) => (
                    <li key={f} className="rounded bg-destructive/10 px-1.5 py-0.5 font-mono text-destructive">
                      {f}
                    </li>
                  ))}
                </ul>
              )}
              {r.reason && <p className="text-xs italic text-muted-foreground">{r.reason}</p>}
            </Link>
          </li>
        ))}
      </ul>
      <AsOf date={data.asOfDate} />
    </article>
  );
}

// =============================================================
// Shared bits
// =============================================================

function KpiRow({ children }: { children: React.ReactNode }) {
  return <div className="grid gap-2 sm:grid-cols-4 lg:grid-cols-6">{children}</div>;
}

function Kpi({ label, value, accent }: { label: string; value: number; accent?: string }) {
  return (
    <article className="rounded-md border bg-card p-3">
      <p className="text-[10px] uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-0.5 text-2xl font-bold">
        {accent ? (
          <span className={cn("inline-flex h-8 w-12 items-center justify-center rounded text-white", accent)}>
            {value}
          </span>
        ) : (
          value
        )}
      </p>
    </article>
  );
}

function Loading() {
  return <p className="text-sm text-muted-foreground">Loading…</p>;
}

function ErrorState({ error }: { error: unknown }) {
  const msg = error instanceof Error ? error.message : "Failed to load report.";
  return <p className="text-sm text-destructive">{msg}</p>;
}

function AsOf({ date }: { date: string }) {
  return <p className="text-xs text-muted-foreground">As of {date}</p>;
}
