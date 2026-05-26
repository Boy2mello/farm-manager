import { apiBaseUrl } from "@/lib/utils";

export type ApiError = { status: number; message: string; details?: unknown };

const ACCESS_TOKEN_KEY = "fm.accessToken";
const ME_CACHE_KEY = "fm.me";

export function getAccessToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function setAccessToken(token: string | null) {
  if (typeof window === "undefined") return;
  if (token) {
    window.localStorage.setItem(ACCESS_TOKEN_KEY, token);
  } else {
    window.localStorage.removeItem(ACCESS_TOKEN_KEY);
    window.localStorage.removeItem(ME_CACHE_KEY);
  }
}

/**
 * Best-effort sign-out: clear the token + cached profile and bounce to /login.
 * Used both by the Profile page's button and by the `api` helper when the
 * server returns 401 (expired/invalid token).
 */
export function clearSessionAndRedirect(reason: "expired" | "manual" = "manual") {
  setAccessToken(null);
  if (typeof window === "undefined") return;
  const next = encodeURIComponent(window.location.pathname + window.location.search);
  const qs = reason === "expired" ? `?reason=expired&next=${next}` : `?next=${next}`;
  window.location.href = `/login${qs}`;
}

export async function api<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const token = getAccessToken();
  const headers = new Headers(init.headers);
  headers.set("Accept", "application/json");
  if (!headers.has("Content-Type") && init.body) {
    headers.set("Content-Type", "application/json");
  }
  if (token) headers.set("Authorization", `Bearer ${token}`);

  const res = await fetch(`${apiBaseUrl()}${path}`, { ...init, headers });

  if (res.status === 401) {
    // Token expired or missing — kick the user to login with a return path.
    clearSessionAndRedirect("expired");
    const err: ApiError = { status: 401, message: "Session expired" };
    throw err;
  }

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    const err: ApiError = { status: res.status, message: text || res.statusText };
    throw err;
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}
