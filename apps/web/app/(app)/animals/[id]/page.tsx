"use client";

import Link from "next/link";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useParams } from "next/navigation";
import {
  ArrowLeft,
  Cake,
  Heart,
  Baby,
  Activity,
  GitBranch,
  Stethoscope,
  Sprout,
  PlusCircle,
  Network,
  Pencil,
} from "lucide-react";
import { api } from "@/lib/api/client";
import { cn } from "@/lib/utils";
import { Section } from "@/components/ui/section";
import { EmptyState } from "@/components/ui/empty-state";
import { TierBadge, StatusPill, SexChip } from "@/components/ui/badges";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { AnimalEditDialog } from "@/components/animal-edit-dialog";

type AnimalDetail = {
  id: string;
  codeName: string;
  primaryName: string | null;
  aliases: string[];
  sex: 1 | 2;
  dob: string;
  dobPrecision: number;
  source: number;
  status: number;
  performanceTier: 0 | 1 | 2 | 3 | 4 | 5;
  isBSired: boolean;
  damId: string | null;
  sireId: string | null;
  calfCount: number;
  calvesAlive: number;
  avgCalvingIntervalDays: number | null;
  calvesPerYear: number | null;
  lastCalvingDate: string | null;
};

const TABS = [
  { key: "overview", label: "Overview", icon: Activity },
  { key: "performance", label: "Performance", icon: Sprout },
  { key: "calvings", label: "Calvings", icon: Baby },
  { key: "breeding", label: "Breeding", icon: Heart },
  { key: "health", label: "Health", icon: Stethoscope },
  { key: "lineage", label: "Lineage", icon: GitBranch },
] as const;

type TabKey = (typeof TABS)[number]["key"];

const SOURCE_LABEL: Record<number, string> = {
  1: "Born on farm",
  2: "Purchased",
  3: "Inherited",
  4: "Transferred in",
  5: "Legacy",
};

export default function AnimalDetailPage() {
  const params = useParams<{ id: string }>();
  const id = params?.id;
  const [tab, setTab] = useState<TabKey>("overview");
  const [editing, setEditing] = useState(false);

  const { data, isLoading, error } = useQuery({
    queryKey: ["animal", id],
    queryFn: () => api<AnimalDetail>(`/api/v1/animals/${id}`),
    enabled: !!id,
  });

  if (isLoading) return <AnimalSkeleton />;
  if (error) return <p className="p-4 text-destructive">Unable to load animal.</p>;
  if (!data) return null;

  const gradient = gradientFor(data.codeName);

  return (
    <article className="space-y-4 animate-fade-in">
      <Link
        href="/animals"
        className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="h-3 w-3" /> Back to herd
      </Link>

      <header className="overflow-hidden rounded-2xl border bg-card shadow-xs">
        <div className="relative h-32 sm:h-40" style={{ background: gradient }}>
          <div
            className={cn(
              "absolute inset-x-0 top-0 h-1.5",
              data.sex === 1 ? "bg-pink-400" : "bg-sky-500",
            )}
          />
          <span className="absolute right-4 top-4 shadow">
            <TierBadge tier={data.performanceTier} size="lg" />
          </span>
          {data.isBSired && (
            <span
              className="absolute left-4 top-4 rounded bg-yellow-300/95 px-2 py-0.5 text-[11px] font-bold uppercase text-yellow-900 shadow"
              title="(B)-sired — Boshomane bloodline"
            >
              (B)-sired
            </span>
          )}
          <span className="absolute inset-0 flex items-center justify-center text-white/40">
            <Sprout className="h-20 w-20 sm:h-28 sm:w-28" />
          </span>
        </div>

        <div className="flex flex-wrap items-end justify-between gap-3 p-4 sm:p-5">
          <div className="space-y-1">
            <div className="flex flex-wrap items-center gap-2">
              <span className="code-name-badge text-xs">{data.codeName}</span>
              <SexChip sex={data.sex} />
              <StatusPill status={data.status} />
            </div>
            <h1 className="text-2xl font-bold tracking-tight sm:text-3xl">
              {data.primaryName ?? "(unnamed)"}
            </h1>
            <p className="text-xs text-muted-foreground">
              DOB <time dateTime={data.dob}>{data.dob}</time>
              {data.aliases.length > 0 && <> · aka {data.aliases.join(", ")}</>}
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            <Button
              variant="outline"
              leading={<Pencil className="h-4 w-4" />}
              onClick={() => setEditing(true)}
            >
              Edit
            </Button>
            {data.sex === 1 && (
              <Link href={`/capture/calving?damId=${data.id}`}>
                <Button leading={<PlusCircle className="h-4 w-4" />}>Record calving</Button>
              </Link>
            )}
            <Link href={`/pedigree/${data.id}`}>
              <Button variant="outline" leading={<Network className="h-4 w-4" />}>
                Pedigree
              </Button>
            </Link>
          </div>
        </div>
      </header>

      <nav className="flex gap-1 overflow-x-auto rounded-lg border bg-card p-1">
        {TABS.map(({ key, label, icon: Icon }) => {
          const active = tab === key;
          return (
            <button
              key={key}
              onClick={() => setTab(key)}
              className={cn(
                "flex items-center gap-1.5 whitespace-nowrap rounded-md px-3 py-2 text-sm font-medium transition",
                active
                  ? "bg-primary text-primary-foreground shadow-xs"
                  : "text-muted-foreground hover:bg-accent",
              )}
            >
              <Icon className="h-4 w-4" />
              {label}
            </button>
          );
        })}
      </nav>

      {tab === "overview" && <OverviewTab animal={data} />}
      {tab === "performance" && <PerformanceTab animal={data} />}
      {tab === "calvings" && <CalvingsTab animal={data} />}
      {tab === "breeding" && (
        <PlaceholderTab
          title="Breeding history"
          desc="Service events and pregnancy checks will appear here once breeding write flows ship."
        />
      )}
      {tab === "health" && (
        <PlaceholderTab
          title="Health timeline"
          desc="Vaccinations, treatments, dewormings, dippings — once recorded against this animal."
        />
      )}
      {tab === "lineage" && <LineageTab animal={data} />}

      <AnimalEditDialog
        animal={{
          id: data.id,
          codeName: data.codeName,
          primaryName: data.primaryName,
          aliases: data.aliases,
          status: data.status,
          sex: data.sex,
        }}
        open={editing}
        onClose={() => setEditing(false)}
      />
    </article>
  );
}

function OverviewTab({ animal }: { animal: AnimalDetail }) {
  return (
    <div className="grid gap-4 lg:grid-cols-2">
      <Section title="Lifecycle">
        <dl className="grid grid-cols-2 gap-3">
          <Field label="Status">
            <StatusPill status={animal.status} />
          </Field>
          <Field label="Tier">
            <TierBadge tier={animal.performanceTier} />
          </Field>
          <Field label="Sex">
            <SexChip sex={animal.sex} />
          </Field>
          <Field label="Source">{SOURCE_LABEL[animal.source] ?? "—"}</Field>
          <Field label="DOB">
            <span className="inline-flex items-center gap-1">
              <Cake className="h-3 w-3 text-muted-foreground" />
              {animal.dob}
            </span>
          </Field>
          <Field label="Last calving">{animal.lastCalvingDate ?? "—"}</Field>
        </dl>
      </Section>

      <Section title="Performance">
        <dl className="grid grid-cols-3 gap-3">
          <NumberTile label="Total calves" value={animal.calfCount} />
          <NumberTile label="Calves alive" value={animal.calvesAlive} />
          <NumberTile
            label="CPY"
            value={animal.calvesPerYear?.toFixed(2) ?? "—"}
            hint="Calves per productive year"
          />
          <NumberTile
            label="Avg interval"
            value={
              animal.avgCalvingIntervalDays != null
                ? `${(Number(animal.avgCalvingIntervalDays) / 30.4375).toFixed(1)} mo`
                : "—"
            }
            hint="Target: 12–14 months"
          />
          <NumberTile label="(B)-sired" value={animal.isBSired ? "Yes" : "No"} />
          <NumberTile
            label="Tier"
            value={["—", "A", "B", "C", "D", "E"][animal.performanceTier]}
          />
        </dl>
      </Section>
    </div>
  );
}

function PerformanceTab({ animal }: { animal: AnimalDetail }) {
  const interval = animal.avgCalvingIntervalDays != null
    ? Number(animal.avgCalvingIntervalDays) / 30.4375
    : null;
  const target = 14;
  const intervalPct = interval == null ? 0 : Math.min(100, (target / interval) * 100);

  return (
    <div className="space-y-4">
      <Section title="At a glance" icon={<Sprout className="h-4 w-4" />}>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <NumberTile label="Total calves" value={animal.calfCount} />
          <NumberTile label="Alive" value={animal.calvesAlive} />
          <NumberTile label="CPY" value={animal.calvesPerYear?.toFixed(2) ?? "—"} />
          <NumberTile
            label="Avg interval"
            value={interval != null ? `${interval.toFixed(1)} mo` : "—"}
          />
        </div>
      </Section>

      <Section title="Calving interval vs target" description="Target = 14 months (best practice)">
        {interval == null ? (
          <EmptyState
            icon={<Activity className="h-5 w-5" />}
            title="Not enough calvings yet"
            description="A calving interval needs at least two calving events."
          />
        ) : (
          <div className="space-y-2">
            <div className="h-2 overflow-hidden rounded-full bg-muted">
              <div
                className={cn(
                  "h-full rounded-full transition-all",
                  interval <= 14
                    ? "bg-emerald-500"
                    : interval <= 18
                      ? "bg-amber-500"
                      : "bg-rose-500",
                )}
                style={{ width: `${intervalPct}%` }}
              />
            </div>
            <p className="text-xs text-muted-foreground">
              Current <strong className="text-foreground">{interval.toFixed(1)} months</strong>
              {interval <= 14
                ? " — comfortably within target."
                : interval <= 18
                  ? " — within tolerance but watch the next cycle."
                  : " — above 18 mo: cull threshold per the spec tier engine."}
            </p>
          </div>
        )}
      </Section>
    </div>
  );
}

function CalvingsTab({ animal }: { animal: AnimalDetail }) {
  if (animal.calfCount === 0) {
    return (
      <Section>
        <EmptyState
          icon={<Baby className="h-5 w-5" />}
          title="No calvings yet"
          description="Once you record a calving against this cow it appears here in time order."
          action={
            animal.sex === 1 ? (
              <Link href={`/capture/calving?damId=${animal.id}`}>
                <Button leading={<PlusCircle className="h-4 w-4" />} size="sm">
                  Record calving
                </Button>
              </Link>
            ) : null
          }
        />
      </Section>
    );
  }

  return (
    <Section
      title="Calving history"
      description="Detailed per-event list arrives in the next sprint."
    >
      <p className="text-sm text-muted-foreground">
        {animal.calfCount} calving{animal.calfCount === 1 ? "" : "s"} recorded ·{" "}
        {animal.calvesAlive} alive · last on{" "}
        <strong className="text-foreground">{animal.lastCalvingDate ?? "—"}</strong>.
      </p>
    </Section>
  );
}

function LineageTab({ animal }: { animal: AnimalDetail }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <Section title="Dam" icon={<GitBranch className="h-4 w-4" />}>
        {animal.damId ? (
          <Link
            href={`/animals/${animal.damId}`}
            className="inline-flex items-center gap-1 text-sm font-medium text-primary hover:underline"
          >
            Open dam profile →
          </Link>
        ) : (
          <p className="text-sm text-muted-foreground">Not recorded.</p>
        )}
      </Section>
      <Section title="Sire" icon={<GitBranch className="h-4 w-4" />}>
        {animal.sireId ? (
          <Link
            href={`/animals/${animal.sireId}`}
            className="inline-flex items-center gap-1 text-sm font-medium text-primary hover:underline"
          >
            Open sire profile →
          </Link>
        ) : (
          <p className="text-sm text-muted-foreground">Not recorded.</p>
        )}
      </Section>
      <Section title="Pedigree tree" icon={<Network className="h-4 w-4" />} className="sm:col-span-2">
        <p className="mb-3 text-sm text-muted-foreground">
          See the full tree with interactive zoom + pan.
        </p>
        <Link href={`/pedigree/${animal.id}`}>
          <Button variant="outline" leading={<Network className="h-4 w-4" />}>
            Open pedigree
          </Button>
        </Link>
      </Section>
    </div>
  );
}

function PlaceholderTab({ title, desc }: { title: string; desc: string }) {
  return (
    <Section>
      <EmptyState icon={<Activity className="h-5 w-5" />} title={title} description={desc} />
    </Section>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <dt className="text-[10px] font-medium uppercase tracking-wide text-muted-foreground">
        {label}
      </dt>
      <dd className="mt-1 text-sm">{children}</dd>
    </div>
  );
}

function NumberTile({
  label,
  value,
  hint,
}: {
  label: string;
  value: React.ReactNode;
  hint?: string;
}) {
  return (
    <article className="rounded-lg border bg-muted/30 p-3">
      <p className="text-[10px] font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-1 text-xl font-bold tabular-nums">{value}</p>
      {hint && <p className="mt-1 text-[10px] text-muted-foreground">{hint}</p>}
    </article>
  );
}

function gradientFor(seed: string): string {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0;
  const hue1 = h % 360;
  const hue2 = (hue1 + 35) % 360;
  return `linear-gradient(135deg, hsl(${hue1} 70% 55%), hsl(${hue2} 65% 38%))`;
}

function AnimalSkeleton() {
  return (
    <div className="space-y-4 animate-fade-in">
      <Skeleton className="h-4 w-24" />
      <Skeleton className="h-40 w-full rounded-2xl" />
      <Skeleton className="h-10 w-full rounded-lg" />
      <div className="grid gap-3 sm:grid-cols-2">
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} className="h-32 rounded-xl" />
        ))}
      </div>
    </div>
  );
}
