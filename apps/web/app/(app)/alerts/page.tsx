"use client";

import { useQuery } from "@tanstack/react-query";
import { Bell, AlertOctagon, Info, CheckCircle2 } from "lucide-react";
import { api } from "@/lib/api/client";
import { PageHeader } from "@/components/ui/page-header";
import { EmptyState } from "@/components/ui/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badges";

type Alert = {
  id: string;
  topic: string;
  title: string;
  body: string;
  severity: number; // 0 info, 1 normal, 2 critical
  createdAt: string;
  deliveredAt: string | null;
};

const SEVERITY_META: Record<number, {
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  ring: string;
  pillTone: "neutral" | "info" | "warning" | "danger";
}> = {
  0: { label: "Info",    icon: Info,           ring: "border-l-sky-400",     pillTone: "info" },
  1: { label: "Normal",  icon: CheckCircle2,   ring: "border-l-emerald-400", pillTone: "neutral" },
  2: { label: "Critical",icon: AlertOctagon,   ring: "border-l-rose-500",    pillTone: "danger" },
};

export default function AlertsPage() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["alerts"],
    queryFn: () => api<Alert[]>("/api/v1/alerts"),
    refetchInterval: 60_000,
  });

  return (
    <section className="space-y-5 animate-fade-in">
      <PageHeader
        icon={<Bell className="h-6 w-6" />}
        title="Alerts"
        description="In-app inbox. WhatsApp + Web Push fire in parallel outside quiet hours."
      />

      {isLoading && (
        <ul className="space-y-2">
          {Array.from({ length: 4 }).map((_, i) => (
            <li key={i}><Skeleton className="h-20 rounded-xl" /></li>
          ))}
        </ul>
      )}

      {error && (
        <p className="text-sm text-destructive">
          Unable to load alerts.{" "}
          {error instanceof Error ? error.message : ""}
        </p>
      )}

      {data && data.length === 0 && (
        <EmptyState
          icon={<Bell className="h-5 w-5" />}
          title="No alerts yet"
          description="As you record events, the system raises notifications here for tier changes, inbreeding blocks, calvings, and overdue tasks."
        />
      )}

      <ul className="space-y-2">
        {(data ?? []).map((a) => {
          const meta = SEVERITY_META[a.severity] ?? SEVERITY_META[1];
          const Icon = meta.icon;
          return (
            <li
              key={a.id}
              className={`flex gap-3 rounded-xl border border-l-4 bg-card p-3 shadow-xs ${meta.ring}`}
            >
              <span className="mt-0.5 shrink-0 text-muted-foreground">
                <Icon className="h-5 w-5" />
              </span>
              <div className="min-w-0 flex-1 space-y-1">
                <div className="flex flex-wrap items-center gap-2">
                  <h3 className="text-sm font-semibold leading-tight">{a.title}</h3>
                  <Badge tone={meta.pillTone}>{meta.label}</Badge>
                  <span className="rounded-full bg-muted px-2 py-0.5 font-mono text-[10px] uppercase tracking-wide text-muted-foreground">
                    {a.topic}
                  </span>
                </div>
                <p className="text-sm text-muted-foreground">{a.body}</p>
                <time
                  dateTime={a.createdAt}
                  className="block text-[11px] text-muted-foreground"
                >
                  {new Date(a.createdAt).toLocaleString()}
                </time>
              </div>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
