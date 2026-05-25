"use client";

import Link from "next/link";
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
} from "recharts";
import { api } from "@/lib/api/client";
import { cn } from "@/lib/utils";

type Kpi = { metric: string; value: number; deltaMonth: number | null; deltaYear: number | null; asOfDate: string };
type Underperformer = { animalId: string; codeName: string; primaryName: string | null; tier: number; flagCodes: string[] };

const KPI_DEFINITIONS: Array<{ key: string; label: string; format: "int" | "pct" | "month" }> = [
  { key: "live_cattle", label: "Live cattle", format: "int" },
  { key: "confirmed_pregnancies", label: "Pregnant", format: "int" },
  { key: "calves_ytd", label: "Calves YTD", format: "int" },
  { key: "calving_rate", label: "Calving rate", format: "pct" },
  { key: "mortality_rate_ytd", label: "Mortality", format: "pct" },
  { key: "avg_calving_interval_months", label: "Interval", format: "month" },
];

const TIER_LABEL = ["—", "A", "B", "C", "D", "E"];
const TIER_COLOURS: Record<string, string> = {
  tier_a: "hsl(142 70% 45%)",
  tier_b: "hsl(95 60% 50%)",
  tier_c: "hsl(45 90% 55%)",
  tier_d: "hsl(25 90% 55%)",
  tier_e: "hsl(0 75% 55%)",
};

export default function AnalyticsPage() {
  const { data: kpis } = useQuery({
    queryKey: ["analytics", "kpis"],
    queryFn: () => api<Kpi[]>("/api/v1/analytics/kpis"),
  });

  const { data: underperformers } = useQuery({
    queryKey: ["analytics", "underperformers"],
    queryFn: () => api<Underperformer[]>("/api/v1/analytics/underperformers"),
  });

  const kpiByMetric = new Map((kpis ?? []).map((k) => [k.metric, k]));

  const tierData = ["tier_a", "tier_b", "tier_c", "tier_d", "tier_e"].map((key) => ({
    name: key.replace("tier_", "Tier ").toUpperCase(),
    value: Number(kpiByMetric.get(key)?.value ?? 0),
    fill: TIER_COLOURS[key],
  }));

  return (
    <section className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold">Herd analytics</h1>
        <p className="text-xs text-muted-foreground">
          Nightly snapshot · last computed {kpis?.[0]?.asOfDate ?? "—"}
        </p>
      </header>

      {/* KPI strip — always visible (spec §9.2). */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
        {KPI_DEFINITIONS.map((d) => {
          const k = kpiByMetric.get(d.key);
          return (
            <article key={d.key} className="rounded-lg border bg-card p-3">
              <p className="text-[10px] uppercase tracking-wide text-muted-foreground">
                {d.label}
              </p>
              <p className="mt-1 text-2xl font-bold">{format(k?.value ?? 0, d.format)}</p>
              <p className={cn("text-xs", deltaColor(k?.deltaYear ?? null))}>
                {deltaText(k?.deltaYear ?? null, d.format)} YoY
              </p>
            </article>
          );
        })}
      </div>

      {/* Tier donut + bar */}
      <div className="grid gap-4 lg:grid-cols-2">
        <article className="rounded-lg border bg-card p-3">
          <h2 className="mb-2 font-semibold">Tier distribution</h2>
          <div className="h-56">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie data={tierData} dataKey="value" nameKey="name" innerRadius={50} outerRadius={80}>
                  {tierData.map((entry, i) => (
                    <Cell key={i} fill={entry.fill} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </article>

        <article className="rounded-lg border bg-card p-3">
          <h2 className="mb-2 font-semibold">Tier counts</h2>
          <div className="h-56">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={tierData}>
                <XAxis dataKey="name" />
                <YAxis allowDecimals={false} />
                <Tooltip />
                <Bar dataKey="value">
                  {tierData.map((entry, i) => (
                    <Cell key={i} fill={entry.fill} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </article>
      </div>

      {/* Underperformer carousel (spec §9.2) */}
      <section>
        <h2 className="mb-2 font-semibold">Underperformers</h2>
        {underperformers?.length === 0 && (
          <p className="text-sm text-muted-foreground">
            Nobody flagged today. Great work.
          </p>
        )}
        <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {(underperformers ?? []).map((u) => (
            <li key={u.animalId} className="rounded-md border bg-card p-3">
              <Link href={`/animals/${u.animalId}`} className="block space-y-2">
                <div className="flex items-center gap-2">
                  <span className="code-name-badge">{u.codeName}</span>
                  <span className={cn("tier-badge", `tier-badge-${TIER_LABEL[u.tier]}`)}>
                    {TIER_LABEL[u.tier]}
                  </span>
                </div>
                <p className="font-semibold">{u.primaryName ?? "—"}</p>
                <ul className="flex flex-wrap gap-1 text-[10px]">
                  {u.flagCodes.map((f) => (
                    <li
                      key={f}
                      className="rounded bg-destructive/10 px-1.5 py-0.5 font-mono text-destructive"
                    >
                      {f}
                    </li>
                  ))}
                </ul>
              </Link>
            </li>
          ))}
        </ul>
      </section>
    </section>
  );
}

function format(v: number, fmt: "int" | "pct" | "month"): string {
  if (fmt === "pct") return `${(v * 100).toFixed(1)}%`;
  if (fmt === "month") return `${v.toFixed(1)} mo`;
  return Math.round(v).toString();
}

function deltaText(d: number | null, fmt: "int" | "pct" | "month") {
  if (d === null) return "—";
  const sign = d > 0 ? "+" : "";
  if (fmt === "pct") return `${sign}${(d * 100).toFixed(1)}%`;
  if (fmt === "month") return `${sign}${d.toFixed(1)} mo`;
  return `${sign}${Math.round(d)}`;
}

function deltaColor(d: number | null) {
  if (d === null) return "text-muted-foreground";
  if (d > 0) return "text-green-600";
  if (d < 0) return "text-destructive";
  return "text-muted-foreground";
}
