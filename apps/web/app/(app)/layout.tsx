import Link from "next/link";
import { Home, Sprout, BarChart3, Bell, User } from "lucide-react";
import { SyncIndicator } from "@/components/sync-indicator";

export default function AppLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-30 border-b bg-background/95 backdrop-blur">
        <div className="container flex h-14 items-center justify-between">
          <Link href="/" className="font-semibold">
            Farm Manager
          </Link>
          <Link href="/profile" className="text-sm text-muted-foreground">
            Profile
          </Link>
        </div>
      </header>

      <main className="container flex-1 py-4 pb-24 sm:pb-6">{children}</main>

      <SyncIndicator />

      {/* Bottom tab bar — mobile-first per spec §7.2 */}
      <nav className="fixed bottom-0 left-0 right-0 z-40 border-t bg-background/95 backdrop-blur sm:hidden">
        <ul className="grid grid-cols-5">
          <TabLink href="/" icon={<Home className="h-5 w-5" />} label="Home" />
          <TabLink href="/animals" icon={<Sprout className="h-5 w-5" />} label="Herd" />
          <TabLink href="/analytics" icon={<BarChart3 className="h-5 w-5" />} label="Analytics" />
          <TabLink href="/alerts" icon={<Bell className="h-5 w-5" />} label="Alerts" />
          <TabLink href="/profile" icon={<User className="h-5 w-5" />} label="Profile" />
        </ul>
      </nav>
    </div>
  );
}

function TabLink({
  href,
  icon,
  label,
}: {
  href: string;
  icon: React.ReactNode;
  label: string;
}) {
  return (
    <li>
      <Link
        href={href}
        className="flex flex-col items-center justify-center gap-1 py-2 text-xs text-muted-foreground hover:text-foreground"
      >
        {icon}
        {label}
      </Link>
    </li>
  );
}
