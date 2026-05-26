import { ArrowDownRight, ArrowUpRight, Minus } from "lucide-react";
import { cn } from "@/lib/utils";

interface KpiCardProps {
  label: string;
  value: React.ReactNode;
  hint?: string;
  delta?: number | null;
  deltaLabel?: string;
  deltaPositiveIsBad?: boolean;
  icon?: React.ReactNode;
  tone?: "default" | "success" | "warning" | "danger" | "info";
  className?: string;
}

const TONE_CLASS: Record<NonNullable<KpiCardProps["tone"]>, string> = {
  default: "bg-card",
  success: "bg-emerald-50/60 dark:bg-emerald-950/30",
  warning: "bg-amber-50/60 dark:bg-amber-950/30",
  danger: "bg-rose-50/60 dark:bg-rose-950/30",
  info: "bg-sky-50/60 dark:bg-sky-950/30",
};

const TONE_ICON: Record<NonNullable<KpiCardProps["tone"]>, string> = {
  default: "text-muted-foreground",
  success: "text-emerald-600",
  warning: "text-amber-600",
  danger: "text-rose-600",
  info: "text-sky-600",
};

export function KpiCard({
  label, value, hint, delta, deltaLabel, deltaPositiveIsBad, icon, tone = "default", className,
}: KpiCardProps) {
  const isPositive = delta != null && delta > 0;
  const isNegative = delta != null && delta < 0;
  const goodColour = "text-emerald-600 dark:text-emerald-400";
  const badColour = "text-rose-600 dark:text-rose-400";

  let deltaColour = "text-muted-foreground";
  if (isPositive) deltaColour = deltaPositiveIsBad ? badColour : goodColour;
  if (isNegative) deltaColour = deltaPositiveIsBad ? goodColour : badColour;

  return (
    <article
      className={cn(
        "relative rounded-xl border bg-card p-4 shadow-xs transition-colors",
        TONE_CLASS[tone],
        className,
      )}
    >
      <div className="flex items-start justify-between gap-3">
        <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
          {label}
        </p>
        {icon && (
          <span className={cn("flex h-9 w-9 items-center justify-center rounded-lg bg-background/60", TONE_ICON[tone])}>
            {icon}
          </span>
        )}
      </div>
      <p className="mt-2 text-3xl font-bold tracking-tight tabular-nums leading-none">{value}</p>
      <div className="mt-2 flex items-center justify-between text-xs">
        {hint ? <span className="text-muted-foreground">{hint}</span> : <span />}
        {delta != null && (
          <span className={cn("inline-flex items-center gap-0.5 font-medium tabular-nums", deltaColour)}>
            {isPositive ? <ArrowUpRight className="h-3 w-3" /> : isNegative ? <ArrowDownRight className="h-3 w-3" /> : <Minus className="h-3 w-3" />}
            {delta > 0 && "+"}
            {delta}
            {deltaLabel && <span className="ml-1 text-muted-foreground">{deltaLabel}</span>}
          </span>
        )}
      </div>
    </article>
  );
}
