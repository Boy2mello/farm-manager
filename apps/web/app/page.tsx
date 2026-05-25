import Link from "next/link";

export default function HomePage() {
  return (
    <main className="container flex min-h-screen flex-col items-center justify-center gap-8 py-12 text-center">
      <div className="space-y-3">
        <h1 className="text-3xl font-bold tracking-tight sm:text-5xl">Farm Manager</h1>
        <p className="text-muted-foreground max-w-prose">
          Mobile-first livestock management. Capture once, compute everything.
        </p>
      </div>
      <div className="flex flex-col gap-3 sm:flex-row">
        <Link
          href="/login"
          className="rounded-md bg-primary px-6 py-3 font-medium text-primary-foreground"
        >
          Sign in
        </Link>
        <Link
          href="/animals"
          className="rounded-md border px-6 py-3 font-medium"
        >
          Browse herd
        </Link>
      </div>
    </main>
  );
}
