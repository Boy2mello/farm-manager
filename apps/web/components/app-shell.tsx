"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Home,
  Sprout,
  BarChart3,
  Bell,
  FileText,
  User,
  PlusCircle,
} from "lucide-react";
import { SyncIndicator } from "@/components/sync-indicator";
import { cn } from "@/lib/utils";

type NavItem = {
  href: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
};

// Six primary destinations — same list, two surfaces.
const NAV: NavItem[] = [
  { href: "/", label: "Home", icon: Home },
  { href: "/animals", label: "Herd", icon: Sprout },
  { href: "/analytics", label: "Analytics", icon: BarChart3 },
  { href: "/alerts", label: "Alerts", icon: Bell },
  { href: "/reports", label: "Reports", icon: FileText },
  { href: "/profile", label: "Profile", icon: User },
];

function isActive(pathname: string | null, href: string): boolean {
  if (!pathname) return false;
  if (href === "/") return pathname === "/";
  return pathname === href || pathname.startsWith(href + "/");
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const activeLabel = NAV.find((n) => isActive(pathname, n.href))?.label ?? "";

  return (
    <div className="flex min-h-screen flex-col">
      {/* Top bar — always shows the brand + current page. Hides the desktop nav row at <md. */}
      <header className="sticky top-0 z-30 border-b bg-background/95 backdrop-blur">
        <div className="container flex h-14 items-center justify-between gap-3">
          <Link href="/" className="flex items-center gap-2 font-semibold">
            <Sprout className="h-5 w-5 text-primary" />
            Farm Manager
          </Link>

          {/* Desktop nav (md+) */}
          <nav className="hidden items-center gap-1 md:flex">
            {NAV.map((item) => {
              const active = isActive(pathname, item.href);
              const Icon = item.icon;
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={cn(
                    "flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm",
                    active
                      ? "bg-primary/10 font-medium text-primary"
                      : "text-muted-foreground hover:bg-accent hover:text-foreground",
                  )}
                >
                  <Icon className="h-4 w-4" />
                  {item.label}
                </Link>
              );
            })}
          </nav>

          {/* Mobile breadcrumb (<md): current page name + quick-capture FAB */}
          <span className="text-sm font-medium text-muted-foreground md:hidden">
            {activeLabel}
          </span>
        </div>
      </header>

      <main className="container flex-1 py-4 pb-24 md:pb-6">{children}</main>

      <SyncIndicator />

      {/* Floating quick-capture button on mobile — primary action always one tap away. */}
      <Link
        href="/capture/calving"
        className="fixed bottom-20 right-4 z-40 flex h-14 w-14 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-lg md:hidden"
        aria-label="Quick capture"
      >
        <PlusCircle className="h-7 w-7" />
      </Link>

      {/* Bottom tab bar — mobile-first per spec §7.2; hides at md+. */}
      <nav className="fixed bottom-0 left-0 right-0 z-40 border-t bg-background/95 backdrop-blur md:hidden">
        <ul className="grid grid-cols-6">
          {NAV.map((item) => {
            const active = isActive(pathname, item.href);
            const Icon = item.icon;
            return (
              <li key={item.href}>
                <Link
                  href={item.href}
                  className={cn(
                    "flex flex-col items-center justify-center gap-0.5 py-2 text-[10px]",
                    active ? "text-primary" : "text-muted-foreground",
                  )}
                  aria-current={active ? "page" : undefined}
                >
                  <Icon className={cn("h-5 w-5", active && "stroke-[2.4]")} />
                  {item.label}
                </Link>
              </li>
            );
          })}
        </ul>
      </nav>
    </div>
  );
}
