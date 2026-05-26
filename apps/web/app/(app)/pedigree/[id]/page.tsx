"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import {
  ReactFlow,
  Background,
  Controls,
  MiniMap,
  type Node,
  type Edge,
  type NodeProps,
  Handle,
  Position,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import { ArrowLeft, GitBranch, ShieldAlert } from "lucide-react";
import { api } from "@/lib/api/client";
import { cn } from "@/lib/utils";
import { PageHeader } from "@/components/ui/page-header";
import { Section } from "@/components/ui/section";
import { TierBadge, StatusPill, SexChip } from "@/components/ui/badges";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { Button } from "@/components/ui/button";

type PedigreeNode = {
  animalId: string;
  codeName: string;
  primaryName: string | null;
  sex: 1 | 2;
  status: number;
  performanceTier: 0 | 1 | 2 | 3 | 4 | 5;
  isBSired: boolean;
  dob: string;
  damId: string | null;
  sireId: string | null;
  generation: number;
};

type PedigreeTree = {
  rootId: string;
  rootCodeName: string;
  rootPrimaryName: string | null;
  generations: number;
  nodes: PedigreeNode[];
};

// Custom React Flow node — renders an animal as a rich card.
function AnimalFlowNode({ data }: NodeProps) {
  const a = data as unknown as PedigreeNode & { isRoot?: boolean };
  return (
    <div
      className={cn(
        "relative w-48 overflow-hidden rounded-xl border bg-card shadow-md transition",
        a.isRoot && "ring-2 ring-primary ring-offset-2",
      )}
    >
      <Handle type="target" position={Position.Top} className="!bg-transparent !border-0" />
      <Handle type="source" position={Position.Bottom} className="!bg-transparent !border-0" />

      <div
        className="relative h-14"
        style={{ background: gradientFor(a.codeName) }}
      >
        <div
          className={cn(
            "absolute inset-x-0 top-0 h-1",
            a.sex === 1 ? "bg-pink-400" : "bg-sky-500",
          )}
        />
        <span className="absolute right-1.5 top-1.5 scale-90">
          <TierBadge tier={a.performanceTier} size="sm" />
        </span>
        {a.isBSired && (
          <span className="absolute left-1.5 top-1.5 rounded bg-yellow-300 px-1 text-[9px] font-bold uppercase text-yellow-900">
            B
          </span>
        )}
        <span className="code-name-badge absolute bottom-1 left-1.5 text-[10px]">
          {a.codeName}
        </span>
      </div>

      <Link
        href={`/animals/${a.animalId}`}
        className="block space-y-1 px-2.5 py-2 text-xs hover:bg-accent/40"
      >
        <p className="truncate font-semibold leading-tight">
          {a.primaryName ?? "(unnamed)"}
        </p>
        <div className="flex items-center justify-between gap-1">
          <SexChip sex={a.sex} />
          <StatusPill status={a.status} />
        </div>
      </Link>
    </div>
  );
}

const nodeTypes = { animal: AnimalFlowNode };

export default function PedigreePage() {
  const params = useParams<{ id: string }>();
  const id = params?.id;
  const [generations, setGenerations] = useState(3);

  const { data, isLoading, error } = useQuery({
    queryKey: ["pedigree", id, generations],
    queryFn: () =>
      api<PedigreeTree>(`/api/v1/lineage/${id}/pedigree?generations=${generations}`),
    enabled: !!id,
  });

  // Build React Flow nodes/edges from the flat tree.
  const { nodes, edges, hasInbreeding } = useMemo(() => {
    if (!data) return { nodes: [] as Node[], edges: [] as Edge[], hasInbreeding: false };

    // Compute per-generation layout — generation 0 at top, generations grow downward.
    const byGen = new Map<number, PedigreeNode[]>();
    for (const n of data.nodes) {
      const list = byGen.get(n.generation) ?? [];
      list.push(n);
      byGen.set(n.generation, list);
    }

    const NODE_W = 200;
    const NODE_H = 130;
    const COL_GAP = 24;
    const ROW_GAP = 60;

    const nodes: Node[] = [];
    for (const [gen, list] of byGen) {
      const totalW = list.length * NODE_W + (list.length - 1) * COL_GAP;
      const startX = -totalW / 2;
      list
        .slice()
        .sort((a, b) => a.codeName.localeCompare(b.codeName))
        .forEach((n, i) => {
          nodes.push({
            id: n.animalId,
            type: "animal",
            position: {
              x: startX + i * (NODE_W + COL_GAP),
              y: gen * (NODE_H + ROW_GAP),
            },
            data: { ...n, isRoot: n.generation === 0 },
            draggable: false,
          });
        });
    }

    // Edges — child → parent. Dam in pink, sire in sky.
    const ids = new Set(data.nodes.map((n) => n.animalId));
    const edges: Edge[] = [];
    for (const n of data.nodes) {
      if (n.damId && ids.has(n.damId)) {
        edges.push({
          id: `${n.animalId}-dam`,
          source: n.animalId,
          target: n.damId,
          type: "smoothstep",
          style: { stroke: "#ec4899", strokeWidth: 2 },
          animated: false,
          label: "dam",
          labelStyle: { fontSize: 9, fill: "#ec4899", fontWeight: 600 },
          labelBgPadding: [4, 2],
          labelBgBorderRadius: 4,
          labelBgStyle: { fill: "hsl(var(--card))" },
        });
      }
      if (n.sireId && ids.has(n.sireId)) {
        edges.push({
          id: `${n.animalId}-sire`,
          source: n.animalId,
          target: n.sireId,
          type: "smoothstep",
          style: { stroke: "#0ea5e9", strokeWidth: 2 },
          animated: false,
          label: "sire",
          labelStyle: { fontSize: 9, fill: "#0ea5e9", fontWeight: 600 },
          labelBgPadding: [4, 2],
          labelBgBorderRadius: 4,
          labelBgStyle: { fill: "hsl(var(--card))" },
        });
      }
    }

    // Detect inbreeding by checking whether any ancestor appears more than once
    // (a name that shows up on both the dam-side and sire-side path).
    const appearances = new Map<string, number>();
    for (const n of data.nodes) {
      appearances.set(n.animalId, (appearances.get(n.animalId) ?? 0) + 1);
    }
    const hasInbreeding = Array.from(appearances.values()).some((c) => c > 1);

    return { nodes, edges, hasInbreeding };
  }, [data]);

  return (
    <section className="space-y-4 animate-fade-in">
      <Link
        href={id ? `/animals/${id}` : "/animals"}
        className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="h-3 w-3" /> Back to animal
      </Link>

      <PageHeader
        icon={<GitBranch className="h-6 w-6" />}
        title="Pedigree"
        description={
          data
            ? `${data.rootPrimaryName ?? data.rootCodeName} · ${data.nodes.length} ancestors across ${data.generations + 1} generations`
            : "Loading lineage…"
        }
        actions={
          <div className="flex items-center gap-1.5 rounded-md border bg-card p-1 text-sm">
            {[2, 3, 4, 5].map((g) => (
              <button
                key={g}
                type="button"
                onClick={() => setGenerations(g)}
                className={cn(
                  "rounded px-2.5 py-1 text-xs font-medium",
                  generations === g
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:bg-accent",
                )}
              >
                {g} gen
              </button>
            ))}
          </div>
        }
      />

      {isLoading && <Skeleton className="h-[60vh] w-full rounded-xl" />}
      {error && (
        <p className="text-sm text-destructive">
          Couldn't load pedigree. {error instanceof Error ? error.message : ""}
        </p>
      )}

      {data && data.nodes.length <= 1 && !isLoading && (
        <Section title="No lineage recorded yet">
          <EmptyState
            icon={<GitBranch className="h-5 w-5" />}
            title="No dam or sire on file"
            description="Set the dam and sire on this animal — or record a calving — and the tree fills in automatically."
            action={
              <Link href={`/animals/${id}`}>
                <Button variant="outline" size="sm">
                  Open animal
                </Button>
              </Link>
            }
          />
        </Section>
      )}

      {data && data.nodes.length > 1 && (
        <>
          {hasInbreeding && (
            <div className="flex items-start gap-2 rounded-md border border-amber-300 bg-amber-50 px-3 py-2 text-sm text-amber-900 dark:bg-amber-950/40 dark:text-amber-100">
              <ShieldAlert className="mt-0.5 h-4 w-4 shrink-0" />
              <span>
                <strong className="font-semibold">Shared ancestor detected.</strong> An ancestor
                appears on both the dam and sire sides of this tree. The system will warn or
                block matings involving these animals per spec §12.3.
              </span>
            </div>
          )}

          <div className="overflow-hidden rounded-xl border bg-card" style={{ height: "65vh" }}>
            <ReactFlow
              nodes={nodes}
              edges={edges}
              nodeTypes={nodeTypes}
              fitView
              fitViewOptions={{ padding: 0.25 }}
              minZoom={0.3}
              maxZoom={1.5}
              proOptions={{ hideAttribution: true }}
              nodesConnectable={false}
              elementsSelectable
            >
              <Background gap={20} />
              <Controls showInteractive={false} />
              <MiniMap nodeColor={() => "hsl(var(--primary) / 0.4)"} maskColor="hsl(var(--background) / 0.6)" />
            </ReactFlow>
          </div>

          <Section title="Legend">
            <ul className="grid gap-2 text-xs sm:grid-cols-3">
              <li className="flex items-center gap-2">
                <span className="h-1 w-6 rounded-full bg-pink-500" />
                Dam (mother) line
              </li>
              <li className="flex items-center gap-2">
                <span className="h-1 w-6 rounded-full bg-sky-500" />
                Sire (father) line
              </li>
              <li className="flex items-center gap-2">
                <span className="rounded bg-yellow-300 px-1 text-[10px] font-bold text-yellow-900">B</span>
                (B)-sired — Boshomane bloodline
              </li>
            </ul>
          </Section>
        </>
      )}
    </section>
  );
}

function gradientFor(seed: string): string {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0;
  const hue1 = h % 360;
  const hue2 = (hue1 + 35) % 360;
  return `linear-gradient(135deg, hsl(${hue1} 70% 55%), hsl(${hue2} 65% 38%))`;
}

