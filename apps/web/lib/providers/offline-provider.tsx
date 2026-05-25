"use client";

import { useEffect } from "react";
import { attachReconnectFlush, flushQueue } from "@/lib/sync/background-sync";

/**
 * Phase C.1 bootstrap: registers the service-worker message handler that flushes the
 * Dexie queue when Background Sync fires, attaches an `online` listener as a fallback,
 * and runs one flush at mount so any queued events from the previous session sync.
 */
export function OfflineProvider({ children }: { children: React.ReactNode }) {
  useEffect(() => {
    attachReconnectFlush();

    function onMessage(event: MessageEvent) {
      if (event.data?.type === "flush-queue") {
        void flushQueue();
      }
    }

    if ("serviceWorker" in navigator) {
      navigator.serviceWorker.addEventListener("message", onMessage);
    }

    // Initial drain — covers the "app reopened with leftover queue" case.
    if (navigator.onLine) {
      void flushQueue();
    }

    return () => {
      if ("serviceWorker" in navigator) {
        navigator.serviceWorker.removeEventListener("message", onMessage);
      }
    };
  }, []);

  return children;
}
