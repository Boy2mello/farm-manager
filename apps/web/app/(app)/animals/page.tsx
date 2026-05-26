"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search, Sprout, Cake } from "lucide-react";
import { api } from "@/lib/api/client";
import { cn } from "@/lib/utils";

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

const TIER_LABEL = ["—", "A", "B", "C", "D", "E"];
const TIER_CLASS = [
  "bg-muted text-muted-foreground",
  "tier-badge-A",
  "tier-badge-B",
  "tier-badge-C",
  "tier-badge-D",
  "tier-badge-E",
];

const STATUS_LABEL: Record<number, string> = {
  1: "Active",
  2: "Open",
  3: "Exposed",
  4: "Pregnant",
  5: "Lactating",
  6: "Dry",
  7: "Sold",
  8: "Dead",
  9: "Missing",
  10: "Transferred",
};

const STATUS_TONE: Record<number, string> = {
  1: "bg-emerald-100 text-emerald-700",
  2: "bg-sky-100 text-sky-700",
  3: "bg-violet-100 text-violet-700",
  4: "bg-pink-100 text-pink-700",
  5: "bg-amber-100 text-amber-800",
  6: "bg-slate-100 text-slate-700",
  7: "bg-zinc-200 text-zinc-700",
  8: "bg-red-100 text-red-700",
  9: "bg-orange-100 text-orange-700",
  10: "bg-indigo-100 text-indigo-700",
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
    <section className="space-y-4">
      <header className="space-y-3">
        <div className="flex items-end justify-between gap-3">
          <div>
            <h1 className="text-2xl font-bold">Your herd</h1>
            <p className="text-xs text-muted-foreground">
              {data ? `${data.length} animals · your sub-herd only` : "Loading…"}
            </p>
          </div>
          <Link
            href="/animals/new"
            className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground"
          >
            Register
          </Link>
        </div>

        <div className="relative">
          <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by name or code (e.g. Mantabole or L-2026-005)"
            className="w-full rounded-md border bg-background py-2 pl-9 pr-3 text-sm"
          />
        </div>

        <div className="-mx-1 flex gap-2 overflow-x-auto px-1 pb-1">
          {FILTERS.map(({ key, label }) => {
            const active = filter === key;
            return (
              <button
                key={key}
                onClick={() => setFilter(key)}
                className={cn(
                  "flex shrink-0 items-center gap-1.5 whitespace-nowrap rounded-full border px-3 py-1 text-xs",
                  active
                    ? "border-primary bg-primary text-primary-foreground"
                    : "border-border bg-background text-muted-foreground hover:bg-accent",
                )}
              >
                {label}
                <span
                  className={cn(
                    "rounded-full px-1.5 text-[10px]",
                    active ? "bg-primary-foreground/20" : "bg-muted",
                  )}
                >
                  {counts[key]}
                </span>
              </button>
            );
          })}
        </div>
      </header>

      {isLoading && <SkeletonGrid />}
      {error && (
        <p className="text-destructive">
          Unable to load animals.{" "}
          {error instanceof Error ? error.message : ""}
        </p>
      )}
      {data && filtered.length === 0 && !isLoading && (
        <div className="rounded-md border bg-card p-8 text-center">
          <p className="font-medium">No animals match.</p>
          <p className="text-sm text-muted-foreground">
            Try a different filter or search term.
          </p>
        </div>
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
  const isFemale = animal.sex === 1;

  return (
    <li>
      <Link
        href={`/animals/${animal.id}`}
        className="group block overflow-hidden rounded-xl border bg-card shadow-sm transition hover:shadow-md"
      >
        <div className="relative h-32 w-full" style={{ background: gradient }}>
          <div
            className={cn(
              "absolute inset-x-0 top-0 h-1.5",
              isFemale ? "bg-pink-400" : "bg-sky-500",
            )}
          />
          <span
            className={cn(
              "absolute right-2 top-3 inline-flex h-7 w-7 items-center justify-center rounded-full text-xs font-bold shadow",
              TIER_CLASS[animal.performanceTier],
            )}
            title={`Performance tier ${TIER_LABEL[animal.performanceTier]}`}
          >
            {TIER_LABEL[animal.performanceTier]}
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
            <span
              className={cn(
                "shrink-0 text-base font-bold",
                isFemale ? "text-pink-500" : "text-sky-500",
              )}
              aria-label={isFemale ? "Female" : "Male"}
            >
              {isFemale ? "♀" : "♂"}
            </span>
          </div>
          <div className="flex items-center justify-between gap-2 text-xs">
            <span
              className={cn(
                "rounded-full px-2 py-0.5 font-medium",
                STATUS_TONE[animal.status] ?? "bg-muted text-muted-foreground",
              )}
            >
              {STATUS_LABEL[animal.status] ?? "Unknown"}
            </span>
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
          <div className="h-32 w-full animate-pulse bg-muted" />
          <div className="space-y-2 p-3">
            <div className="h-4 w-2/3 animate-pulse rounded bg-muted" />
            <div className="h-3 w-1/2 animate-pulse rounded bg-muted" />
          </div>
        </li>
      ))}
    </ul>
  );
}
