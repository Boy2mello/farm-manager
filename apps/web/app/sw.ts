/// <reference lib="webworker" />
/// <reference types="@serwist/next/typings" />
import { defaultCache } from "@serwist/next/worker";
import { Serwist, NetworkFirst, NetworkOnly, BackgroundSyncPlugin } from "serwist";

declare const self: ServiceWorkerGlobalScope;

const SYNC_TAG = "farm-manager-event-queue";

const bgSyncPlugin = new BackgroundSyncPlugin("farm-manager-bg-sync", {
  maxRetentionTime: 24 * 60, // 24 h in minutes — retain queue across rural-signal outages
});

const serwist = new Serwist({
  precacheEntries: self.__SW_MANIFEST,
  skipWaiting: true,
  clientsClaim: true,
  navigationPreload: true,
  runtimeCaching: [
    // GETs of animal data — stale-while-revalidate via NetworkFirst with cache fallback.
    {
      matcher: ({ url, request }) =>
        request.method === "GET" && url.pathname.startsWith("/api/v1/animals"),
      handler: new NetworkFirst({
        cacheName: "farm-api-animals",
        networkTimeoutSeconds: 3,
      }),
    },
    // Lineage + analytics queries — same pattern.
    {
      matcher: ({ url, request }) =>
        request.method === "GET" &&
        (url.pathname.startsWith("/api/v1/lineage") || url.pathname.startsWith("/api/v1/analytics")),
      handler: new NetworkFirst({
        cacheName: "farm-api-reads",
        networkTimeoutSeconds: 3,
      }),
    },
    // Writes — never cache, but enrol in Background Sync so they retry on reconnect.
    {
      matcher: ({ url, request }) =>
        url.pathname.startsWith("/api/") &&
        (request.method === "POST" || request.method === "PUT" || request.method === "DELETE"),
      handler: new NetworkOnly({ plugins: [bgSyncPlugin] }),
    },
    ...defaultCache,
  ],
});

// Listen for app-triggered sync (Background Sync API) — flushes the Dexie queue inside clients.
self.addEventListener("sync", (event) => {
  const e = event as ExtendableEvent & { tag: string; waitUntil(p: Promise<unknown>): void };
  if (e.tag === SYNC_TAG) {
    e.waitUntil(notifyClientsToFlush());
  }
});

async function notifyClientsToFlush() {
  const clients = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
  for (const client of clients) {
    client.postMessage({ type: "flush-queue" });
  }
}

// Web Push — spec §16.1 — shows a notification even when the PWA is closed.
self.addEventListener("push", (event) => {
  if (!event.data) return;
  let payload: Record<string, unknown> = {};
  try {
    payload = event.data.json();
  } catch {
    payload = { title: "Farm Manager", body: event.data.text() };
  }

  const title = (payload.title as string) ?? "Farm Manager";
  const body = (payload.body as string) ?? "";
  const topic = (payload.topic as string) ?? "alert";
  const data = (payload.data as Record<string, unknown>) ?? {};

  event.waitUntil(
    self.registration.showNotification(title, {
      body,
      tag: topic,
      renotify: true,
      icon: "/icons/icon-192.png",
      badge: "/icons/icon-192.png",
      data,
    }),
  );
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  const data = (event.notification.data ?? {}) as Record<string, unknown>;
  const calfId = data.calfId as string | undefined;
  const target = calfId ? `/animals/${calfId}` : "/alerts";

  event.waitUntil(
    (async () => {
      const clients = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
      const focused = clients.find((c) => c.url.includes(target));
      if (focused) {
        await focused.focus();
        return;
      }
      const open = clients[0];
      if (open) {
        await open.focus();
        await (open as WindowClient).navigate?.(target);
        return;
      }
      await self.clients.openWindow(target);
    })(),
  );
});

serwist.addEventListeners();
