"use client";

import { useState } from "react";
import { apiBaseUrl } from "@/lib/utils";
import { getAccessToken } from "@/lib/api/client";

const REPORTS: Array<{ path: string; label: string; description: string }> = [
  { path: "herd-census.pdf", label: "Herd Census (PDF)", description: "Active animals with code-name, sex, DOB, tier." },
  { path: "herd-census.xlsx", label: "Herd Census (Excel)", description: "Full register with status and (B)-sired flag." },
  { path: "performance-ranking.pdf", label: "Performance Ranking (PDF)", description: "Breeding cows sorted by tier." },
  { path: "cull-candidates.pdf", label: "Cull Candidates (PDF)", description: "Tier E cows with CPY." },
];

export default function ReportsPage() {
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function download(path: string) {
    setBusy(path);
    setError(null);
    try {
      const token = getAccessToken();
      const res = await fetch(`${apiBaseUrl()}/api/v1/reports/${path}`, {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (!res.ok) throw new Error(`Report failed: ${res.status}`);
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = path;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Download failed.");
    } finally {
      setBusy(null);
    }
  }

  return (
    <section className="space-y-3">
      <header>
        <h1 className="text-2xl font-bold">Reports</h1>
        <p className="text-xs text-muted-foreground">
          PDF + Excel exports per spec §15.1. Financial reports arrive when the Bookkeeper role is added (Phase E+).
        </p>
      </header>

      {error && (
        <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">{error}</p>
      )}

      <ul className="space-y-2">
        {REPORTS.map((r) => (
          <li key={r.path} className="flex items-center justify-between rounded-md border bg-card p-3">
            <div>
              <p className="font-semibold">{r.label}</p>
              <p className="text-xs text-muted-foreground">{r.description}</p>
            </div>
            <button
              type="button"
              onClick={() => download(r.path)}
              disabled={busy === r.path}
              className="rounded-md bg-primary px-3 py-1.5 text-sm text-primary-foreground disabled:opacity-50"
            >
              {busy === r.path ? "Generating…" : "Download"}
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}
