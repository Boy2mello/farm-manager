"use client";

import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { useParams } from "next/navigation";
import { api } from "@/lib/api/client";

type AnimalDetail = {
  id: string;
  codeName: string;
  primaryName: string | null;
  aliases: string[];
  sex: number;
  dob: string;
  dobPrecision: number;
  source: number;
  status: number;
  performanceTier: number;
  isBSired: boolean;
  damId: string | null;
  sireId: string | null;
  calfCount: number;
  calvesAlive: number;
  avgCalvingIntervalDays: number | null;
  calvesPerYear: number | null;
  lastCalvingDate: string | null;
};

const TABS = ["Overview", "Performance", "Calvings", "Breeding", "Health", "Lineage", "Financial"];

export default function AnimalDetailPage() {
  const params = useParams<{ id: string }>();
  const { data, isLoading, error } = useQuery({
    queryKey: ["animal", params.id],
    queryFn: () => api<AnimalDetail>(`/api/v1/animals/${params.id}`),
    enabled: !!params.id,
  });

  if (isLoading) return <p className="text-muted-foreground">Loading…</p>;
  if (error) return <p className="text-destructive">Unable to load animal.</p>;
  if (!data) return null;

  return (
    <article className="space-y-4">
      <header className="space-y-2">
        <div className="flex items-center gap-3">
          <span className="code-name-badge text-sm">{data.codeName}</span>
          {data.isBSired && (
            <span className="rounded bg-yellow-200 px-1.5 text-xs font-bold uppercase text-yellow-900">
              (B)
            </span>
          )}
        </div>
        <h1 className="text-2xl font-bold">{data.primaryName ?? "(unnamed)"}</h1>
        <p className="text-sm text-muted-foreground">
          {data.sex === 1 ? "Female" : "Male"} · DOB {data.dob}
          {data.aliases.length > 0 && ` · aka ${data.aliases.join(", ")}`}
        </p>
      </header>

      <nav className="flex gap-2 overflow-x-auto border-b text-sm">
        {TABS.map((tab) => (
          <button
            key={tab}
            className="border-b-2 border-transparent px-3 py-2 hover:border-primary"
          >
            {tab}
          </button>
        ))}
      </nav>

      <section className="grid gap-3 sm:grid-cols-2">
        <Stat label="Total calves" value={data.calfCount} />
        <Stat label="Calves alive" value={data.calvesAlive} />
        <Stat label="Calves per year" value={data.calvesPerYear?.toFixed(2) ?? "—"} />
        <Stat
          label="Avg calving interval"
          value={data.avgCalvingIntervalDays != null
            ? `${(Number(data.avgCalvingIntervalDays) / 30.4375).toFixed(1)} mo`
            : "—"}
        />
        <Stat label="Last calving" value={data.lastCalvingDate ?? "—"} />
        <Stat label="Status" value={STATUS_LABELS[data.status] ?? String(data.status)} />
      </section>

      <div className="flex flex-wrap gap-2">
        {data.sex === 1 && (
          <Link
            href={`/capture/calving?damId=${data.id}`}
            className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground"
          >
            Record calving
          </Link>
        )}
        <Link
          href={`/pedigree/${data.id}`}
          className="rounded-md border px-4 py-2 text-sm font-medium"
        >
          View pedigree
        </Link>
      </div>
    </article>
  );
}

const STATUS_LABELS: Record<number, string> = {
  1: "Active",
  2: "Open",
  3: "Exposed",
  4: "Confirmed pregnant",
  5: "Lactating",
  6: "Dry",
  7: "Sold",
  8: "Dead",
  9: "Missing",
  10: "Transferred",
};

function Stat({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="rounded-md border bg-card p-3">
      <p className="text-xs uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-1 text-lg font-semibold">{value}</p>
    </div>
  );
}
