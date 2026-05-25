import { apiBaseUrl } from "@/lib/utils";

export type ApiError = { status: number; message: string; details?: unknown };

const ACCESS_TOKEN_KEY = "fm.accessToken";

export function getAccessToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function setAccessToken(token: string | null) {
  if (typeof window === "undefined") return;
  if (token) window.localStorage.setItem(ACCESS_TOKEN_KEY, token);
  else window.localStorage.removeItem(ACCESS_TOKEN_KEY);
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

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    const err: ApiError = { status: res.status, message: text || res.statusText };
    throw err;
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}
