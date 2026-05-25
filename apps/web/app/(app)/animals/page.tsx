"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api/client";
import { cn } from "@/lib/utils";

type AnimalSummary = {
  id: string;
  codeName: string;
  primaryName: string | null;
  sex: number; // 1=F, 2=M
  dob: string;
  status: number;
  performanceTier: number; // 0=None, 1=A … 5=E
  isBSired: boolean;
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

export default function AnimalsPage() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["animals"],
    queryFn: () => api<AnimalSummary[]>("/api/v1/animals"),
  });

  return (
    <section className="space-y-4">
      <header className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Herd</h1>
        <Link
          href="/animals/new"
          className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground"
        >
          Register
        </Link>
      </header>

      {isLoading && <p className="text-muted-foreground">Loading…</p>}
      {error && (
        <p className="text-destructive">
          Unable to load animals.{" "}
          {error instanceof Error ? error.message : ""}
        </p>
      )}

      <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {(data ?? []).map((a) => (
          <li key={a.id}>
            <Link
              href={`/animals/${a.id}`}
              className="flex items-start gap-3 rounded-lg border bg-card p-3 hover:bg-accent"
            >
              <div className="relative h-16 w-16 shrink-0 rounded-md bg-muted">
                <span className="code-name-badge absolute right-1 top-1">
                  {a.codeName}
                </span>
              </div>
              <div className="flex-1 space-y-1">
                <div className="flex items-center gap-2">
                  <p className="font-semibold leading-tight">
                    {a.primaryName ?? "—"}
                  </p>
                  <span
                    className={cn("tier-badge", TIER_CLASS[a.performanceTier])}
                  >
                    {TIER_LABEL[a.performanceTier]}
                  </span>
                  {a.isBSired && (
                    <span
                      title="(B)-sired — Boshomane bloodline"
                      className="rounded bg-yellow-200 px-1.5 text-[10px] font-bold uppercase text-yellow-900"
                    >
                      B
                    </span>
                  )}
                </div>
                <p className="text-xs text-muted-foreground">
                  {a.sex === 1 ? "Female" : "Male"} · {a.dob}
                </p>
              </div>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
