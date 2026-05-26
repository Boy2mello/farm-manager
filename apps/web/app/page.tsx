import Link from "next/link";
import { Sprout } from "lucide-react";

export default function HomePage() {
  return (
    <main className="container flex min-h-screen flex-col items-center justify-center gap-8 py-12 text-center">
      <div className="space-y-4">
        <div className="flex items-center justify-center gap-2">
          <Sprout className="h-9 w-9 text-primary" />
          <h1 className="text-3xl font-bold tracking-tight sm:text-5xl">Farm Manager</h1>
        </div>
        <p className="text-muted-foreground max-w-prose">
          Mobile-first livestock management. Capture once, compute everything.
          Your herd is private — sign in to view it.
        </p>
      </div>
      <Link
        href="/login"
        className="rounded-md bg-primary px-6 py-3 font-medium text-primary-foreground"
      >
        Sign in
      </Link>
    </main>
  );
}
