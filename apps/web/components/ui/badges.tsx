import { cn } from "@/lib/utils";

// --- Tier badge -------------------------------------------------

const TIER_LABEL = ["—", "A", "B", "C", "D", "E"] as const;
const TIER_CLASS = [
  "bg-muted text-muted-foreground",
  "bg-emerald-500 text-white",
  "bg-lime-500 text-white",
  "bg-amber-400 text-amber-950",
  "bg-orange-500 text-white",
  "bg-rose-500 text-white",
];

export function TierBadge({ tier, size = "md" }: { tier: 0 | 1 | 2 | 3 | 4 | 5; size?: "sm" | "md" | "lg" }) {
  const dim = size === "sm" ? "h-5 w-5 text-[10px]" : size === "lg" ? "h-8 w-8 text-sm" : "h-6 w-6 text-xs";
  return (
    <span
      className={cn(
        "inline-flex items-center justify-center rounded-full font-mono font-bold leading-none",
        dim,
        TIER_CLASS[tier],
      )}
      title={tier === 0 ? "Unranked" : `Tier ${TIER_LABEL[tier]}`}
    >
      {TIER_LABEL[tier]}
    </span>
  );
}

// --- Status pill ------------------------------------------------

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
  1: "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-300",
  2: "bg-sky-100 text-sky-700 dark:bg-sky-950/50 dark:text-sky-300",
  3: "bg-violet-100 text-violet-700 dark:bg-violet-950/50 dark:text-violet-300",
  4: "bg-pink-100 text-pink-700 dark:bg-pink-950/50 dark:text-pink-300",
  5: "bg-amber-100 text-amber-800 dark:bg-amber-950/50 dark:text-amber-300",
  6: "bg-slate-100 text-slate-700 dark:bg-slate-900/50 dark:text-slate-300",
  7: "bg-zinc-200 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300",
  8: "bg-red-100 text-red-700 dark:bg-red-950/50 dark:text-red-300",
  9: "bg-orange-100 text-orange-700 dark:bg-orange-950/50 dark:text-orange-300",
  10: "bg-indigo-100 text-indigo-700 dark:bg-indigo-950/50 dark:text-indigo-300",
};

export function StatusPill({ status, className }: { status: number; className?: string }) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium",
        STATUS_TONE[status] ?? "bg-muted text-muted-foreground",
        className,
      )}
    >
      {STATUS_LABEL[status] ?? "Unknown"}
    </span>
  );
}

export const STATUS_LABELS = STATUS_LABEL;

// --- Sex chip ---------------------------------------------------

export function SexChip({ sex }: { sex: 1 | 2 }) {
  const isFemale = sex === 1;
  return (
    <span
      className={cn(
        "inline-flex h-5 items-center gap-0.5 rounded-full px-1.5 text-[11px] font-semibold",
        isFemale
          ? "bg-pink-100 text-pink-700 dark:bg-pink-950/40 dark:text-pink-300"
          : "bg-sky-100 text-sky-700 dark:bg-sky-950/40 dark:text-sky-300",
      )}
      aria-label={isFemale ? "Female" : "Male"}
    >
      {isFemale ? "♀" : "♂"} {isFemale ? "F" : "M"}
    </span>
  );
}

// --- Generic Badge ----------------------------------------------

interface BadgeProps {
  tone?: "default" | "success" | "warning" | "danger" | "info" | "neutral";
  size?: "sm" | "md";
  children: React.ReactNode;
  className?: string;
}

export function Badge({ tone = "default", size = "sm", children, className }: BadgeProps) {
  const tones: Record<NonNullable<BadgeProps["tone"]>, string> = {
    default: "bg-primary/10 text-primary",
    success: "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-300",
    warning: "bg-amber-100 text-amber-800 dark:bg-amber-950/50 dark:text-amber-300",
    danger: "bg-rose-100 text-rose-700 dark:bg-rose-950/50 dark:text-rose-300",
    info: "bg-sky-100 text-sky-700 dark:bg-sky-950/50 dark:text-sky-300",
    neutral: "bg-muted text-muted-foreground",
  };
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full font-medium leading-none",
        size === "sm" ? "px-2 py-0.5 text-[11px]" : "px-2.5 py-1 text-xs",
        tones[tone],
        className,
      )}
    >
      {children}
    </span>
  );
}
