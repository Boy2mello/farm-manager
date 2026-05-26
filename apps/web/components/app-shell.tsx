"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import {
  Home,
  Sprout,
  BarChart3,
  Bell,
  FileText,
  User,
  PlusCircle,
  Lock,
} from "lucide-react";
import { SyncIndicator } from "@/components/sync-indicator";
import { cn } from "@/lib/utils";
import { getAccessToken } from "@/lib/api/client";

type NavItem = {
  href: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
};

const NAV: NavItem[] = [
  { href: "/dashboard", label: "Dashboard", icon: Home },
  { href: "/animals", label: "Herd", icon: Sprout },
  { href: "/analytics", label: "Analytics", icon: BarChart3 },
  { href: "/alerts", label: "Alerts", icon: Bell },
  { href: "/reports", label: "Reports", icon: FileText },
  { href: "/profile", label: "Profile", icon: User },
];

function isActive(pathname: string | null, href: string): boolean {
  if (!pathname) return false;
  // Dashboard is also the root for logged-in users.
  if (href === "/dashboard" && pathname === "/") return true;
  return pathname === href || pathname.startsWith(href + "/");
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [authState, setAuthState] = useState<"checking" | "authed" | "unauthed">("checking");

  // Auth guard: every page under (app)/ requires a token. If absent, send to /login
  // with a `next` query so the user lands back where they were trying to go.
  useEffect(() => {
    const token = getAccessToken();
    if (token) {
      setAuthState("authed");
      return;
    }
    setAuthState("unauthed");
    const next = encodeURIComponent(pathname ?? "/");
    router.replace(`/login?next=${next}`);
  }, [pathname, router]);

  if (authState !== "authed") {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <div className="flex max-w-sm flex-col items-center gap-3 px-6 text-center">
          <Lock className="h-8 w-8 text-muted-foreground" />
          <p className="text-sm text-muted-foreground">
            {authState === "checking"
              ? "Checking your session…"
              : "You need to sign in to view this herd."}
          </p>
          <Link
            href={`/login?next=${encodeURIComponent(pathname ?? "/")}`}
            className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground"
          >
            Sign in
          </Link>
        </div>
      </div>
    );
  }

  const activeLabel = NAV.find((n) => isActive(pathname, n.href))?.label ?? "";

  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-30 border-b bg-background/95 backdrop-blur">
        <div className="container flex h-14 items-center justify-between gap-3">
          <Link href="/dashboard" className="flex items-center gap-2 font-semibold">
            <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground">
              <Sprout className="h-4 w-4" />
            </span>
            <span className="hidden sm:inline">Farm Manager</span>
          </Link>

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

          <span className="text-sm font-medium text-muted-foreground md:hidden">
            {activeLabel}
          </span>
        </div>
      </header>

      <main className="container flex-1 py-4 pb-24 md:pb-6">{children}</main>

      <SyncIndicator />

      <Link
        href="/capture/calving"
        className="fixed bottom-20 right-4 z-40 flex h-14 w-14 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-lg md:hidden"
        aria-label="Quick capture"
      >
        <PlusCircle className="h-7 w-7" />
      </Link>

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
