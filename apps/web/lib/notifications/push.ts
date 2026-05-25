"use client";

import { api } from "@/lib/api/client";

/**
 * Web Push subscription flow per spec §7.6 / Phase C.4.
 * Pull the VAPID public key from the API, ask the browser for a subscription, and POST it back.
 */
export async function ensurePushSubscription(): Promise<PushSubscription | null> {
  if (typeof window === "undefined") return null;
  if (!("serviceWorker" in navigator) || !("PushManager" in window)) return null;

  const permission = await Notification.requestPermission();
  if (permission !== "granted") return null;

  const reg = await navigator.serviceWorker.ready;
  const existing = await reg.pushManager.getSubscription();
  if (existing) {
    await uploadSubscription(existing);
    return existing;
  }

  const { publicKey } = await api<{ publicKey: string }>("/api/v1/push/public-key");
  if (!publicKey) return null;

  const sub = await reg.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: urlBase64ToUint8Array(publicKey),
  });

  await uploadSubscription(sub);
  return sub;
}

async function uploadSubscription(sub: PushSubscription) {
  const json = sub.toJSON();
  if (!json.endpoint || !json.keys) return;
  await api("/api/v1/push/subscribe", {
    method: "POST",
    body: JSON.stringify({
      endpoint: json.endpoint,
      p256dh: json.keys.p256dh,
      auth: json.keys.auth,
      userAgent: typeof navigator !== "undefined" ? navigator.userAgent : null,
    }),
  });
}

function urlBase64ToUint8Array(b64: string): Uint8Array {
  const padding = "=".repeat((4 - (b64.length % 4)) % 4);
  const base64 = (b64 + padding).replace(/-/g, "+").replace(/_/g, "/");
  const raw = atob(base64);
  const output = new Uint8Array(raw.length);
  for (let i = 0; i < raw.length; ++i) output[i] = raw.charCodeAt(i);
  return output;
}
