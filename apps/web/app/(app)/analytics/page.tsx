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
  CartesianGrid,
} from "recharts";
import {
  BarChart3,
  Sprout,
  Heart,
  Baby,
  Activity,
  AlertOctagon,
  ChevronRight,
  TrendingUp,
} from "lucide-react";
import { api } from "@/lib/api/client";
import { PageHeader } from "@/components/ui/page-header";
import { Section } from "@/components/ui/section";
import { KpiCard } from "@/components/ui/kpi-card";
import { EmptyState } from "@/components/ui/empty-state";
import { TierBadge } from "@/components/ui/badges";
import { Skeleton } from "@/components/ui/skeleton";

type Kpi = {
  metric: string;
  value: number;
  deltaMonth: number | null;
  deltaYear: number | null;
  asOfDate: string;
};

type Underperformer = {
  animalId: string;
  codeName: string;
  primaryName: string | null;
  tier: 0 | 1 | 2 | 3 | 4 | 5;
  flagCodes: string[];
};

const TIER_COLOURS = ["#94a3b8", "#10b981", "#84cc16", "#f59e0b", "#f97316", "#f43f5e"];

const tooltipStyle = {
  background: "hsl(var(--popover))",
  border: "1px solid hsl(var(--border))",
  borderRadius: 8,
  fontSize: 12,
  padding: "6px 10px",
  color: "hsl(var(--popover-foreground))",
};

export default function AnalyticsPage() {
  const { data: kpis, isLoading: loadingKpis } = useQuery({
    queryKey: ["analytics", "kpis"],
    queryFn: () => api<Kpi[]>("/api/v1/analytics/kpis"),
  });

  const { data: underperformers } = useQuery({
    queryKey: ["analytics", "underperformers"],
    queryFn: () => api<Underperformer[]>("/api/v1/analytics/underperformers"),
  });

  const kByMetric = new Map((kpis ?? []).map((k) => [k.metric, k]));
  const tierData = (["tier_a", "tier_b", "tier_c", "tier_d", "tier_e"] as const).map(
    (key, i) => ({
      name: `Tier ${["A", "B", "C", "D", "E"][i]}`,
      value: Number(kByMetric.get(key)?.value ?? 0),
      fill: TIER_COLOURS[i + 1],
    }),
  );

  return (
    <div className="space-y-5 animate-fade-in">
      <PageHeader
        icon={<BarChart3 className="h-6 w-6" />}
        title="Herd analytics"
        description={
          kpis?.[0]?.asOfDate
            ? `Nightly snapshot · as of ${kpis[0].asOfDate}`
            : "Live herd KPIs computed from the nightly snapshot."
        }
      />

      {/* KPI strip */}
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {loadingKpis ? (
          Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-32 rounded-xl" />
          ))
        ) : (
          <>
            <KpiCard
              label="Live cattle"
              value={Math.round(Number(kByMetric.get("live_cattle")?.value ?? 0))}
              delta={kByMetric.get("live_cattle")?.deltaYear ?? null}
              deltaLabel="YoY"
              icon={<Sprout className="h-5 w-5" />}
              tone="info"
            />
            <KpiCard
              label="Pregnant cows"
              value={Math.round(Number(kByMetric.get("confirmed_pregnancies")?.value ?? 0))}
              delta={kByMetric.get("confirmed_pregnancies")?.deltaYear ?? null}
              deltaLabel="YoY"
              icon={<Heart className="h-5 w-5" />}
              tone="success"
            />
            <KpiCard
              label="Calves YTD"
              value={Math.round(Number(kByMetric.get("calves_ytd")?.value ?? 0))}
              delta={kByMetric.get("calves_ytd")?.deltaYear ?? null}
              deltaLabel="YoY"
              icon={<Baby className="h-5 w-5" />}
            />
            <KpiCard
              label="Avg calving interval"
              value={`${Number(kByMetric.get("avg_calving_interval_months")?.value ?? 0).toFixed(1)} mo`}
              hint="Target 12–14 mo"
              delta={kByMetric.get("avg_calving_interval_months")?.deltaYear ?? null}
              deltaLabel="YoY"
              deltaPositiveIsBad
              icon={<Activity className="h-5 w-5" />}
              tone="warning"
            />
          </>
        )}
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <Section title="Tier distribution" icon={<TrendingUp className="h-4 w-4" />}>
          <div className="h-64">
            <ResponsiveContainer>
              <PieChart>
                <Pie
                  data={tierData}
                  dataKey="value"
                  nameKey="name"
                  innerRadius={50}
                  outerRadius={85}
                  paddingAngle={2}
                  label={(p) => `${p.name}: ${p.value}`}
                  labelLine={false}
                  style={{ fontSize: 11 }}
                >
                  {tierData.map((entry, i) => (
                    <Cell key={i} fill={entry.fill} stroke="hsl(var(--card))" strokeWidth={2} />
                  ))}
                </Pie>
                <Tooltip contentStyle={tooltipStyle} />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </Section>

        <Section title="Tier counts" icon={<BarChart3 className="h-4 w-4" />}>
          <div className="h-64">
            <ResponsiveContainer>
              <BarChart data={tierData} margin={{ top: 4, right: 4, left: -16, bottom: 4 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
                <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
                <Tooltip contentStyle={tooltipStyle} />
                <Bar dataKey="value" radius={[6, 6, 0, 0]}>
                  {tierData.map((entry, i) => (
                    <Cell key={i} fill={entry.fill} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </Section>
      </div>

      <Section
        title="Underperformers"
        description={
          underperformers && underperformers.length > 0
            ? `${underperformers.length} animal${underperformers.length === 1 ? "" : "s"} flagged`
            : "Nobody flagged today"
        }
        icon={<AlertOctagon className="h-4 w-4" />}
      >
        {!underperformers || underperformers.length === 0 ? (
          <EmptyState
            icon={<Activity className="h-5 w-5" />}
            title="Nobody flagged today"
            description="The tier engine and flag catalogue agree — your herd is performing within thresholds."
          />
        ) : (
          <ul className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
            {underperformers.map((u) => (
              <li key={u.animalId}>
                <Link
                  href={`/animals/${u.animalId}`}
                  className="flex items-center justify-between gap-3 rounded-md border p-3 transition hover:bg-accent/40"
                >
                  <div className="flex min-w-0 items-center gap-2">
                    <TierBadge tier={u.tier} size="sm" />
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium">{u.primaryName ?? "—"}</p>
                      <p className="truncate font-mono text-[11px] text-muted-foreground">{u.codeName}</p>
                    </div>
                  </div>
                  <ChevronRight className="h-4 w-4 text-muted-foreground" />
                </Link>
              </li>
            ))}
          </ul>
        )}
      </Section>
    </div>
  );
}
