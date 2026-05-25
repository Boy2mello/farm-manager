"use client";

import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api/client";

type Alert = {
  id: string;
  topic: string;
  title: string;
  body: string;
  severity: number;
  createdAt: string;
  deliveredAt: string | null;
};

const SEVERITY_LABEL = ["Info", "Normal", "Critical"];

export default function AlertsPage() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["alerts"],
    queryFn: () => api<Alert[]>("/api/v1/alerts"),
    refetchInterval: 60_000,
  });

  return (
    <section className="space-y-3">
      <header className="flex items-baseline justify-between">
        <h1 className="text-2xl font-bold">Alerts</h1>
        <span className="text-xs text-muted-foreground">
          In-app inbox · WhatsApp + Push fire in parallel outside quiet hours.
        </span>
      </header>

      {isLoading && <p className="text-muted-foreground">Loading…</p>}
      {error && (
        <p className="text-destructive">
          Unable to load alerts.{" "}
          {error instanceof Error ? error.message : ""}
        </p>
      )}

      <ul className="space-y-2">
        {(data ?? []).map((a) => (
          <li
            key={a.id}
            className="rounded-md border bg-card p-3 shadow-sm"
            data-severity={SEVERITY_LABEL[a.severity] ?? "Normal"}
          >
            <div className="flex items-center justify-between text-xs text-muted-foreground">
              <span className="font-mono uppercase tracking-wide">{a.topic}</span>
              <time dateTime={a.createdAt}>{new Date(a.createdAt).toLocaleString()}</time>
            </div>
            <h2 className="mt-1 font-semibold">{a.title}</h2>
            <p className="text-sm text-muted-foreground">{a.body}</p>
          </li>
        ))}
        {data?.length === 0 && (
          <li className="text-sm text-muted-foreground">No alerts yet.</li>
        )}
      </ul>
    </section>
  );
}
