"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search, X, ChevronDown, Check } from "lucide-react";
import { api } from "@/lib/api/client";
import { cn } from "@/lib/utils";
import { TierBadge, StatusPill, SexChip } from "@/components/ui/badges";

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

interface AnimalPickerProps {
  value: string | null;
  onChange: (id: string | null) => void;
  /** Restrict to a sex (1=F, 2=M). For dam-pickers this should be 1. */
  sex?: 1 | 2;
  /** Hide animals already disposed of */
  excludeStatuses?: number[];
  placeholder?: string;
  disabled?: boolean;
}

/**
 * Searchable, keyboard-friendly animal picker. Hits /api/v1/animals once and filters
 * client-side, which is fine for herds up to a few thousand head. Displays each candidate
 * with the same identity affordances used everywhere else (code-name + tier + status + sex).
 */
export function AnimalPicker({
  value,
  onChange,
  sex,
  excludeStatuses = [7, 8, 10], // Sold, Dead, Transferred
  placeholder = "Search by name or code…",
  disabled,
}: AnimalPickerProps) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const containerRef = useRef<HTMLDivElement>(null);

  const { data } = useQuery({
    queryKey: ["animals", "picker"],
    queryFn: () => api<AnimalSummary[]>("/api/v1/animals"),
    staleTime: 60_000,
  });

  // Lookup the selected animal so we can show its name/code in the trigger.
  const selected = useMemo(
    () => (value ? (data ?? []).find((a) => a.id === value) ?? null : null),
    [value, data],
  );

  // Filter pool: sex constraint + status filter + query match.
  const matches = useMemo(() => {
    if (!data) return [];
    const q = query.trim().toLowerCase();
    return data
      .filter((a) => (sex == null || a.sex === sex))
      .filter((a) => !excludeStatuses.includes(a.status))
      .filter((a) => {
        if (!q) return true;
        return (
          a.codeName.toLowerCase().includes(q) ||
          (a.primaryName ?? "").toLowerCase().includes(q)
        );
      })
      .slice(0, 80);
  }, [data, query, sex, excludeStatuses]);

  // Close on outside click.
  useEffect(() => {
    if (!open) return;
    function onDocClick(e: MouseEvent) {
      if (!containerRef.current?.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener("mousedown", onDocClick);
    return () => document.removeEventListener("mousedown", onDocClick);
  }, [open]);

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        disabled={disabled}
        onClick={() => setOpen((v) => !v)}
        className={cn(
          "flex w-full items-center justify-between gap-2 rounded-md border bg-background px-3 py-2.5 text-left text-sm shadow-xs transition",
          open ? "border-primary ring-2 ring-primary/30" : "hover:bg-accent/40",
          disabled && "opacity-50",
        )}
      >
        {selected ? (
          <span className="flex min-w-0 flex-1 items-center gap-2">
            <span className="truncate font-medium">{selected.primaryName ?? "(unnamed)"}</span>
            <span className="hidden font-mono text-[11px] text-muted-foreground sm:inline">
              {selected.codeName}
            </span>
            <SexChip sex={selected.sex} />
            <StatusPill status={selected.status} />
          </span>
        ) : (
          <span className="flex items-center gap-2 text-muted-foreground">
            <Search className="h-4 w-4" /> {placeholder}
          </span>
        )}
        <span className="flex shrink-0 items-center gap-1">
          {selected && (
            <span
              role="button"
              tabIndex={0}
              onClick={(e) => {
                e.stopPropagation();
                onChange(null);
              }}
              onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                  e.stopPropagation();
                  onChange(null);
                }
              }}
              aria-label="Clear selection"
              className="rounded p-1 hover:bg-muted"
            >
              <X className="h-3.5 w-3.5" />
            </span>
          )}
          <ChevronDown className={cn("h-4 w-4 text-muted-foreground transition", open && "rotate-180")} />
        </span>
      </button>

      {open && (
        <div className="absolute z-30 mt-1 w-full overflow-hidden rounded-md border bg-popover shadow-lg">
          <div className="border-b p-2">
            <div className="relative">
              <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <input
                autoFocus
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                placeholder="Type a name or code"
                className="w-full rounded-md border bg-background py-2 pl-8 pr-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              />
            </div>
          </div>

          <ul className="max-h-72 overflow-y-auto scrollbar-thin">
            {matches.length === 0 && (
              <li className="px-3 py-6 text-center text-sm text-muted-foreground">
                No animals match.
              </li>
            )}
            {matches.map((a) => {
              const isSelected = value === a.id;
              return (
                <li key={a.id}>
                  <button
                    type="button"
                    onClick={() => {
                      onChange(a.id);
                      setOpen(false);
                      setQuery("");
                    }}
                    className={cn(
                      "flex w-full items-center justify-between gap-3 px-3 py-2 text-left text-sm transition",
                      isSelected ? "bg-accent" : "hover:bg-accent/40",
                    )}
                  >
                    <span className="flex min-w-0 items-center gap-2">
                      <TierBadge tier={a.performanceTier} size="sm" />
                      <span className="min-w-0 truncate">
                        <span className="font-medium">{a.primaryName ?? "(unnamed)"}</span>
                        <span className="ml-1.5 font-mono text-[10px] text-muted-foreground">
                          {a.codeName}
                        </span>
                      </span>
                    </span>
                    <span className="flex shrink-0 items-center gap-2">
                      <SexChip sex={a.sex} />
                      <StatusPill status={a.status} />
                      {isSelected && <Check className="h-4 w-4 text-primary" />}
                    </span>
                  </button>
                </li>
              );
            })}
          </ul>
        </div>
      )}
    </div>
  );
}
