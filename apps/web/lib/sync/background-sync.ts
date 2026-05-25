"use client";

import { apiBaseUrl } from "@/lib/utils";
import { getAccessToken } from "@/lib/api/client";
import { getDb, type QueuedEvent } from "@/lib/db/dexie";

const SYNC_TAG = "farm-manager-event-queue";

/**
 * Adds an event to the local write-ahead queue and asks the service worker to flush it
 * on reconnect (spec §7.3 — Background Sync API). Returns the assigned local id immediately
 * so the UI can show optimistic confirmation toasts.
 */
export async function enqueueEvent(
  endpoint: string,
  method: QueuedEvent["method"],
  payload: unknown,
): Promise<number> {
  const db = getDb();
  const id = await db.queue.add({
    endpoint,
    method,
    payload,
    idempotencyKey: crypto.randomUUID(),
    createdAt: Date.now(),
    attempts: 0,
    status: "pending",
  });

  if ("serviceWorker" in navigator) {
    try {
      const reg = await navigator.serviceWorker.ready;
      if ("sync" in reg) {
        // @ts-expect-error — BackgroundSyncManager isn't in lib.dom yet on every browser.
        await reg.sync.register(SYNC_TAG);
      } else {
        // Fall back to a one-shot flush when the SW lacks Background Sync (iOS today).
        void flushQueue();
      }
    } catch {
      void flushQueue();
    }
  } else {
    void flushQueue();
  }

  return id as number;
}

/**
 * Flushes every pending event, one at a time so conflicts are surfaced in order.
 * Idempotent — safe for repeated calls from `online` listeners and the SW.
 */
export async function flushQueue(): Promise<{ sent: number; failed: number; conflicts: number }> {
  const db = getDb();
  const pending = await db.queue
    .where("status")
    .anyOf("pending", "failed")
    .sortBy("createdAt");

  let sent = 0;
  let failed = 0;
  let conflicts = 0;

  for (const item of pending) {
    await db.queue.update(item.id!, { status: "syncing", attempts: item.attempts + 1 });
    try {
      const res = await fetch(`${apiBaseUrl()}${item.endpoint}`, {
        method: item.method,
        headers: buildHeaders(item.idempotencyKey),
        body: item.method === "DELETE" ? undefined : JSON.stringify(item.payload),
      });

      if (res.ok) {
        await db.queue.delete(item.id!);
        sent++;
      } else if (res.status === 409) {
        const body = await res.text().catch(() => "Conflict");
        await db.conflicts.add({
          endpoint: item.endpoint,
          payload: item.payload,
          serverMessage: body,
          detectedAt: Date.now(),
        });
        await db.queue.update(item.id!, { status: "conflict", lastError: body });
        conflicts++;
      } else {
        const body = await res.text().catch(() => res.statusText);
        await db.queue.update(item.id!, { status: "failed", lastError: body });
        failed++;
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : "network error";
      await db.queue.update(item.id!, { status: "failed", lastError: msg });
      failed++;
    }
  }

  return { sent, failed, conflicts };
}

function buildHeaders(idempotencyKey: string): HeadersInit {
  const headers = new Headers({
    "Content-Type": "application/json",
    Accept: "application/json",
    "Idempotency-Key": idempotencyKey,
  });
  const token = getAccessToken();
  if (token) headers.set("Authorization", `Bearer ${token}`);
  return headers;
}

/**
 * Best-effort hook to attach reconnect listeners. Call once from a top-level client component.
 */
export function attachReconnectFlush() {
  if (typeof window === "undefined") return;
  window.addEventListener("online", () => void flushQueue());
}
