"use client";

import { useEffect, useState } from "react";
import { CloudOff, Cloud, RefreshCw } from "lucide-react";
import { getDb } from "@/lib/db/dexie";
import { flushQueue } from "@/lib/sync/background-sync";

export function SyncIndicator() {
  const [pending, setPending] = useState(0);
  const [conflicts, setConflicts] = useState(0);
  const [online, setOnline] = useState(true);
  const [syncing, setSyncing] = useState(false);

  useEffect(() => {
    setOnline(typeof navigator !== "undefined" ? navigator.onLine : true);
    const on = () => setOnline(true);
    const off = () => setOnline(false);
    window.addEventListener("online", on);
    window.addEventListener("offline", off);

    let cancelled = false;
    async function tick() {
      try {
        const db = getDb();
        const p = await db.queue.where("status").anyOf("pending", "failed", "syncing").count();
        const c = await db.conflicts.count();
        if (!cancelled) {
          setPending(p);
          setConflicts(c);
        }
      } catch {
        /* ignore — IndexedDB may not be ready yet */
      }
    }
    void tick();
    const interval = window.setInterval(tick, 4000);

    return () => {
      cancelled = true;
      window.removeEventListener("online", on);
      window.removeEventListener("offline", off);
      window.clearInterval(interval);
    };
  }, []);

  async function manualFlush() {
    setSyncing(true);
    try {
      await flushQueue();
    } finally {
      setSyncing(false);
    }
  }

  if (pending === 0 && conflicts === 0 && online) return null;

  return (
    <div className="fixed bottom-16 left-1/2 z-50 -translate-x-1/2 sm:bottom-3">
      <button
        onClick={manualFlush}
        disabled={syncing}
        className="flex items-center gap-2 rounded-full border bg-background/95 px-3 py-1.5 text-xs shadow backdrop-blur"
      >
        {!online ? (
          <>
            <CloudOff className="h-3.5 w-3.5 text-destructive" />
            Offline
          </>
        ) : syncing ? (
          <>
            <RefreshCw className="h-3.5 w-3.5 animate-spin" />
            Syncing…
          </>
        ) : (
          <>
            <Cloud className="h-3.5 w-3.5 text-muted-foreground" />
            {pending > 0 && <span>{pending} pending</span>}
            {conflicts > 0 && (
              <span className="text-destructive">{conflicts} conflicts</span>
            )}
          </>
        )}
      </button>
    </div>
  );
}
