"use client";

import { useQuery } from "@tanstack/react-query";
import { useParams } from "next/navigation";
import { api } from "@/lib/api/client";

type AnimalDetail = {
  id: string;
  codeName: string;
  primaryName: string | null;
  damId: string | null;
  sireId: string | null;
};

// Phase B places the pedigree behind a feature flag — React Flow integration arrives in B.6.
export default function PedigreePage() {
  const params = useParams<{ id: string }>();
  const { data, isLoading } = useQuery({
    queryKey: ["animal", params.id, "pedigree-root"],
    queryFn: () => api<AnimalDetail>(`/api/v1/animals/${params.id}`),
    enabled: !!params.id,
  });

  return (
    <section className="space-y-3">
      <h1 className="text-2xl font-bold">Pedigree</h1>
      {isLoading && <p className="text-muted-foreground">Loading…</p>}
      {data && (
        <div className="space-y-2 rounded-md border bg-card p-4">
          <p>
            <span className="code-name-badge">{data.codeName}</span>{" "}
            <strong>{data.primaryName ?? "(unnamed)"}</strong>
          </p>
          <p className="text-sm text-muted-foreground">
            Dam: {data.damId ?? "—"} · Sire: {data.sireId ?? "—"}
          </p>
          <p className="text-xs text-muted-foreground">
            Full interactive React Flow tree lands in Phase B.6.
          </p>
        </div>
      )}
    </section>
  );
}
