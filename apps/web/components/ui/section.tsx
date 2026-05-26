import { cn } from "@/lib/utils";

interface SectionProps {
  title?: React.ReactNode;
  description?: string;
  actions?: React.ReactNode;
  icon?: React.ReactNode;
  children: React.ReactNode;
  className?: string;
  bodyClassName?: string;
  padded?: boolean;
}

export function Section({
  title, description, actions, icon, children, className, bodyClassName, padded = true,
}: SectionProps) {
  return (
    <section className={cn("overflow-hidden rounded-xl border bg-card shadow-xs", className)}>
      {(title || actions) && (
        <header className="flex flex-wrap items-center justify-between gap-2 border-b px-4 py-3">
          <div className="flex items-center gap-2 min-w-0">
            {icon && <span className="text-primary">{icon}</span>}
            <div className="min-w-0">
              {title && <h2 className="text-sm font-semibold leading-tight">{title}</h2>}
              {description && (
                <p className="truncate text-xs text-muted-foreground">{description}</p>
              )}
            </div>
          </div>
          {actions && <div className="flex items-center gap-2 text-sm">{actions}</div>}
        </header>
      )}
      <div className={cn(padded ? "p-4" : "", bodyClassName)}>{children}</div>
    </section>
  );
}
