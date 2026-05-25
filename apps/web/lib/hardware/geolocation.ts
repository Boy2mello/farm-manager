"use client";

export type CoarseLocation = { latitude: number; longitude: number; accuracy: number };

/**
 * Lazily reads the current GPS fix and tags an event with the camp/paddock GPS coordinates
 * (spec §7.5). Returns `null` if permission is denied or unavailable — the capture flow never
 * blocks waiting for GPS in the kraal.
 */
export async function tryGetCoarseLocation(timeoutMs = 4000): Promise<CoarseLocation | null> {
  if (typeof navigator === "undefined" || !navigator.geolocation) return null;
  return new Promise((resolve) => {
    const timer = window.setTimeout(() => resolve(null), timeoutMs);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        window.clearTimeout(timer);
        resolve({
          latitude: pos.coords.latitude,
          longitude: pos.coords.longitude,
          accuracy: pos.coords.accuracy,
        });
      },
      () => {
        window.clearTimeout(timer);
        resolve(null);
      },
      { enableHighAccuracy: false, maximumAge: 60_000, timeout: timeoutMs },
    );
  });
}
