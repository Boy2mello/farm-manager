"use client";

import Dexie, { type EntityTable } from "dexie";

/**
 * Local herd snapshot — written when on Wi-Fi, served when offline (spec §7.3).
 */
export interface CachedAnimal {
  id: string;
  codeName: string;
  primaryName: string | null;
  sex: number;
  dob: string;
  status: number;
  performanceTier: number;
  isBSired: boolean;
  cachedAt: number; // epoch ms
}

/**
 * Write-ahead queue for events created offline (spec §7.3). Flushed by Background Sync
 * on reconnect via `lib/sync/background-sync.ts`.
 */
export interface QueuedEvent {
  id?: number;
  endpoint: string;      // e.g. "/api/v1/animals/calvings"
  method: "POST" | "PUT" | "DELETE";
  payload: unknown;
  idempotencyKey: string;
  createdAt: number;
  attempts: number;
  lastError?: string;
  status: "pending" | "syncing" | "failed" | "conflict";
}

/**
 * Conflicts surfaced by the server. The owner inspects these in the "needs attention" queue.
 */
export interface ConflictRecord {
  id?: number;
  endpoint: string;
  payload: unknown;
  serverMessage: string;
  detectedAt: number;
}

export type FarmDexie = Dexie & {
  animals: EntityTable<CachedAnimal, "id">;
  queue: EntityTable<QueuedEvent, "id">;
  conflicts: EntityTable<ConflictRecord, "id">;
};

let _db: FarmDexie | null = null;

export function getDb(): FarmDexie {
  if (typeof window === "undefined") {
    throw new Error("Dexie is only available in the browser.");
  }

  if (_db) return _db;

  const db = new Dexie("farm-manager") as FarmDexie;
  db.version(1).stores({
    animals: "id, codeName, primaryName, performanceTier, status",
    queue: "++id, status, createdAt, idempotencyKey",
    conflicts: "++id, detectedAt, endpoint",
  });

  _db = db;
  return db;
}

export async function snapshotHerd(animals: CachedAnimal[]) {
  const db = getDb();
  const now = Date.now();
  await db.transaction("rw", db.animals, async () => {
    await db.animals.clear();
    await db.animals.bulkAdd(animals.map((a) => ({ ...a, cachedAt: now })));
  });
}
