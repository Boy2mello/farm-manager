"use client";

import { useEffect, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { X, Save, AlertCircle } from "lucide-react";
import { api } from "@/lib/api/client";
import { Button } from "@/components/ui/button";
import { useToast } from "@/components/ui/toast";

const STATUS_OPTIONS: Array<{ value: number; label: string; description: string }> = [
  { value: 1, label: "Active", description: "Normal default state" },
  { value: 2, label: "Open", description: "Not pregnant, available to be bred" },
  { value: 3, label: "Exposed", description: "Recently mated, status unknown" },
  { value: 4, label: "Pregnant", description: "Confirmed pregnant" },
  { value: 5, label: "Lactating", description: "Nursing a calf" },
  { value: 6, label: "Dry", description: "Weaned off calf, resting" },
  { value: 7, label: "Sold", description: "Disposed by sale" },
  { value: 8, label: "Dead", description: "Mortality" },
  { value: 9, label: "Missing", description: "Lost / stolen / strayed" },
  { value: 10, label: "Transferred", description: "Moved to another farm" },
];

interface AnimalEditDialogProps {
  animal: {
    id: string;
    codeName: string;
    primaryName: string | null;
    aliases: string[];
    status: number;
    sex: 1 | 2;
  };
  open: boolean;
  onClose: () => void;
}

export function AnimalEditDialog({ animal, open, onClose }: AnimalEditDialogProps) {
  const qc = useQueryClient();
  const toast = useToast();
  const [status, setStatus] = useState(animal.status);
  const [primaryName, setPrimaryName] = useState(animal.primaryName ?? "");
  const [aliasesText, setAliasesText] = useState(animal.aliases.join(", "));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const dialogRef = useRef<HTMLDivElement>(null);

  // Reset when the dialog opens against a different animal.
  useEffect(() => {
    if (open) {
      setStatus(animal.status);
      setPrimaryName(animal.primaryName ?? "");
      setAliasesText(animal.aliases.join(", "));
      setError(null);
    }
  }, [open, animal]);

  // Esc-to-close + focus trap (very lightweight).
  useEffect(() => {
    if (!open) return;
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") onClose();
    }
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  if (!open) return null;

  async function save() {
    setSaving(true);
    setError(null);
    try {
      const aliases = aliasesText
        .split(/[,;]/)
        .map((a) => a.trim())
        .filter(Boolean);

      await api(`/api/v1/animals/${animal.id}`, {
        method: "PATCH",
        body: JSON.stringify({
          status,
          primaryName: primaryName.trim() || null,
          aliases,
        }),
      });

      // Refresh caches that depend on this animal.
      await Promise.all([
        qc.invalidateQueries({ queryKey: ["animal", animal.id] }),
        qc.invalidateQueries({ queryKey: ["animals"] }),
        qc.invalidateQueries({ queryKey: ["animals", "picker"] }),
        qc.invalidateQueries({ queryKey: ["analytics"] }),
      ]);

      toast.success("Saved", `${primaryName || animal.codeName} updated.`);
      onClose();
    } catch (e) {
      const msg = e instanceof Error ? e.message : "Save failed";
      setError(msg);
      toast.error("Couldn't save", msg);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="edit-animal-title"
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-0 backdrop-blur-sm sm:items-center sm:p-4"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div
        ref={dialogRef}
        className="w-full max-w-lg overflow-hidden rounded-t-2xl border bg-card shadow-2xl sm:rounded-2xl"
      >
        <header className="flex items-center justify-between border-b px-5 py-4">
          <div className="min-w-0">
            <h2 id="edit-animal-title" className="text-lg font-semibold leading-tight">
              Edit {animal.primaryName ?? animal.codeName}
            </h2>
            <p className="font-mono text-[11px] text-muted-foreground">{animal.codeName}</p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
            aria-label="Close"
          >
            <X className="h-4 w-4" />
          </button>
        </header>

        <div className="space-y-4 px-5 py-4">
          <div className="space-y-1.5">
            <label className="text-sm font-medium" htmlFor="name">
              Primary name
            </label>
            <input
              id="name"
              type="text"
              value={primaryName}
              onChange={(e) => setPrimaryName(e.target.value)}
              placeholder="e.g. Mantabole"
              className="w-full rounded-md border bg-background px-3 py-2 text-sm shadow-xs"
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-sm font-medium" htmlFor="aliases">
              Aliases <span className="text-[10px] uppercase text-muted-foreground">optional</span>
            </label>
            <input
              id="aliases"
              type="text"
              value={aliasesText}
              onChange={(e) => setAliasesText(e.target.value)}
              placeholder="comma-separated, e.g. Tiki, Mmadikrempe"
              className="w-full rounded-md border bg-background px-3 py-2 text-sm shadow-xs"
            />
            <p className="text-[11px] text-muted-foreground">
              Aliases are searchable across the whole app.
            </p>
          </div>

          <div className="space-y-1.5">
            <span className="text-sm font-medium">Status</span>
            <ul className="grid gap-1.5">
              {STATUS_OPTIONS.map((opt) => {
                const active = status === opt.value;
                return (
                  <li key={opt.value}>
                    <button
                      type="button"
                      onClick={() => setStatus(opt.value)}
                      className={`flex w-full items-start justify-between gap-2 rounded-md border px-3 py-2 text-left text-sm transition ${
                        active
                          ? "border-primary bg-primary/10"
                          : "bg-card hover:bg-accent/40"
                      }`}
                    >
                      <span className="min-w-0">
                        <span className="font-medium">{opt.label}</span>
                        <span className="ml-2 text-[11px] text-muted-foreground">
                          {opt.description}
                        </span>
                      </span>
                      <span
                        className={`mt-0.5 inline-flex h-4 w-4 shrink-0 items-center justify-center rounded-full border ${
                          active ? "border-primary bg-primary" : "border-border"
                        }`}
                      >
                        {active && <span className="h-2 w-2 rounded-full bg-primary-foreground" />}
                      </span>
                    </button>
                  </li>
                );
              })}
            </ul>
          </div>

          {error && (
            <p className="flex items-start gap-2 rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
              {error}
            </p>
          )}
        </div>

        <footer className="flex items-center justify-end gap-2 border-t bg-muted/30 px-5 py-3">
          <Button type="button" variant="ghost" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button
            type="button"
            onClick={save}
            disabled={saving}
            leading={<Save className="h-4 w-4" />}
          >
            {saving ? "Saving…" : "Save changes"}
          </Button>
        </footer>
      </div>
    </div>
  );
}
