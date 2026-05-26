"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search, Sprout, Cake, Plus } from "lucide-react";
import { api } from "@/lib/api/client";
import { cn } from "@/lib/utils";
import { PageHeader } from "@/components/ui/page-header";
import { TierBadge, StatusPill, SexChip } from "@/components/ui/badges";
import { EmptyState } from "@/components/ui/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";

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

type FilterKey =
  | "all"
  | "female"
  | "male"
  | "pregnant"
  | "open"
  | "lactating"
  | "b-sired"
  | "tier-e";

const FILTERS: Array<{ key: FilterKey; label: string }> = [
  { key: "all", label: "All" },
  { key: "female", label: "Females" },
  { key: "male", label: "Males" },
  { key: "pregnant", label: "Pregnant" },
  { key: "open", label: "Open" },
  { key: "lactating", label: "Lactating" },
  { key: "b-sired", label: "(B)-sired" },
  { key: "tier-e", label: "Tier E" },
];

export default function AnimalsPage() {
  const [filter, setFilter] = useState<FilterKey>("all");
  const [search, setSearch] = useState("");

  const { data, isLoading, error } = useQuery({
    queryKey: ["animals"],
    queryFn: () => api<AnimalSummary[]>("/api/v1/animals"),
  });

  const filtered = useMemo(() => {
    if (!data) return [];
    const q = search.trim().toLowerCase();
    return data.filter((a) => {
      if (q) {
        const haystack = `${a.codeName} ${a.primaryName ?? ""}`.toLowerCase();
        if (!haystack.includes(q)) return false;
      }
      switch (filter) {
        case "female":
          return a.sex === 1;
        case "male":
          return a.sex === 2;
        case "pregnant":
          return a.status === 4;
        case "open":
          return a.status === 2;
        case "lactating":
          return a.status === 5;
        case "b-sired":
          return a.isBSired;
        case "tier-e":
          return a.performanceTier === 5;
        default:
          return true;
      }
    });
  }, [data, search, filter]);

  const counts = useMemo(() => {
    const c: Record<FilterKey, number> = {
      all: 0,
      female: 0,
      male: 0,
      pregnant: 0,
      open: 0,
      lactating: 0,
      "b-sired": 0,
      "tier-e": 0,
    };
    (data ?? []).forEach((a) => {
      c.all++;
      if (a.sex === 1) c.female++;
      if (a.sex === 2) c.male++;
      if (a.status === 4) c.pregnant++;
      if (a.status === 2) c.open++;
      if (a.status === 5) c.lactating++;
      if (a.isBSired) c["b-sired"]++;
      if (a.performanceTier === 5) c["tier-e"]++;
    });
    return c;
  }, [data]);

  return (
    <section className="space-y-5 animate-fade-in">
      <PageHeader
        icon={<Sprout className="h-6 w-6" />}
        title="Your herd"
        description={
          data ? `${data.length} animals · your sub-herd only` : "Loading your animals…"
        }
        actions={
          <Link href="/animals/new">
            <Button leading={<Plus className="h-4 w-4" />}>Register animal</Button>
          </Link>
        }
      />

      <div className="space-y-3">
        <div className="relative">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by name or code (e.g. Mantabole or L-2026-005)"
            className="w-full rounded-md border bg-card py-2.5 pl-9 pr-3 text-sm shadow-xs"
          />
        </div>

        <div className="-mx-1 flex gap-1.5 overflow-x-auto px-1 pb-1">
          {FILTERS.map(({ key, label }) => {
            const active = filter === key;
            return (
              <button
                key={key}
                onClick={() => setFilter(key)}
                className={cn(
                  "flex shrink-0 items-center gap-1.5 whitespace-nowrap rounded-full border px-3 py-1.5 text-xs transition",
                  active
                    ? "border-primary bg-primary text-primary-foreground shadow-xs"
                    : "border-border bg-card text-muted-foreground hover:bg-accent",
                )}
              >
                {label}
                <span
                  className={cn(
                    "rounded-full px-1.5 text-[10px] tabular-nums",
                    active ? "bg-primary-foreground/20" : "bg-muted",
                  )}
                >
                  {counts[key]}
                </span>
              </button>
            );
          })}
        </div>
      </div>

      {isLoading && <SkeletonGrid />}
      {error && (
        <p className="text-destructive">
          Unable to load animals.{" "}
          {error instanceof Error ? error.message : ""}
        </p>
      )}
      {data && filtered.length === 0 && !isLoading && (
        <EmptyState
          icon={<Sprout className="h-5 w-5" />}
          title="No animals match"
          description="Try a different filter or search term — or register a new animal."
          action={
            <Link href="/animals/new">
              <Button leading={<Plus className="h-4 w-4" />} size="sm">Register animal</Button>
            </Link>
          }
        />
      )}

      <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        {filtered.map((a) => (
          <AnimalCard key={a.id} animal={a} />
        ))}
      </ul>
    </section>
  );
}

function AnimalCard({ animal }: { animal: AnimalSummary }) {
  const gradient = useMemo(() => gradientFor(animal.codeName), [animal.codeName]);

  return (
    <li>
      <Link
        href={`/animals/${animal.id}`}
        className="group block overflow-hidden rounded-xl border bg-card shadow-xs transition hover:-translate-y-0.5 hover:shadow-md"
      >
        <div className="relative h-32 w-full" style={{ background: gradient }}>
          <div
            className={cn(
              "absolute inset-x-0 top-0 h-1.5",
              animal.sex === 1 ? "bg-pink-400" : "bg-sky-500",
            )}
          />
          <span className="absolute right-2 top-3 shadow">
            <TierBadge tier={animal.performanceTier} />
          </span>
          {animal.isBSired && (
            <span
              className="absolute left-2 top-3 rounded bg-yellow-300/95 px-1.5 py-0.5 text-[10px] font-bold uppercase text-yellow-900 shadow"
              title="(B)-sired — Boshomane bloodline"
            >
              B
            </span>
          )}
          <span className="code-name-badge absolute bottom-2 left-2 text-[11px]">
            {animal.codeName}
          </span>
          <span className="absolute inset-0 flex items-center justify-center text-white/40 transition group-hover:text-white/70">
            <Sprout className="h-12 w-12" />
          </span>
        </div>

        <div className="space-y-2 p-3">
          <div className="flex items-center justify-between gap-2">
            <p className="truncate font-semibold leading-tight">
              {animal.primaryName ?? "(unnamed)"}
            </p>
            <SexChip sex={animal.sex} />
          </div>
          <div className="flex items-center justify-between gap-2 text-xs">
            <StatusPill status={animal.status} />
            <span className="inline-flex items-center gap-1 text-muted-foreground">
              <Cake className="h-3 w-3" />
              {animal.dob}
            </span>
          </div>
        </div>
      </Link>
    </li>
  );
}

function gradientFor(seed: string): string {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0;
  const hue1 = h % 360;
  const hue2 = (hue1 + 35) % 360;
  return `linear-gradient(135deg, hsl(${hue1} 70% 55%), hsl(${hue2} 65% 38%))`;
}

function SkeletonGrid() {
  return (
    <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {Array.from({ length: 8 }).map((_, i) => (
        <li key={i} className="overflow-hidden rounded-xl border bg-card">
          <Skeleton className="h-32 w-full" />
          <div className="space-y-2 p-3">
            <Skeleton className="h-4 w-2/3" />
            <Skeleton className="h-3 w-1/2" />
          </div>
        </li>
      ))}
    </ul>
  );
}
