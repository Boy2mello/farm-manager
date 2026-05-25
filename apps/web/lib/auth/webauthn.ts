"use client";

/**
 * Thin WebAuthn helper (spec §7.5). Calls /api/v1/auth/webauthn/* endpoints that arrive in
 * Phase E security hardening; the client surface is here today so the login UI can offer
 * "Continue with Passkey" the moment the server flow is enabled.
 */
import { api } from "@/lib/api/client";

export async function isPasskeyAvailable(): Promise<boolean> {
  if (typeof window === "undefined" || !window.PublicKeyCredential) return false;
  try {
    return await window.PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
  } catch {
    return false;
  }
}

export async function loginWithPasskey(email: string): Promise<{ accessToken: string } | null> {
  if (!(await isPasskeyAvailable())) return null;

  const challenge = await api<{ challenge: string; allowCredentialIds: string[] }>(
    `/api/v1/auth/webauthn/challenge?email=${encodeURIComponent(email)}`,
    { method: "POST" },
  );

  const assertion = await navigator.credentials.get({
    publicKey: {
      challenge: base64urlToBuffer(challenge.challenge),
      timeout: 60_000,
      userVerification: "preferred",
      allowCredentials: challenge.allowCredentialIds.map((id) => ({
        type: "public-key",
        id: base64urlToBuffer(id),
      })),
    },
  });

  if (!assertion || assertion.type !== "public-key") return null;

  const cred = assertion as PublicKeyCredential;
  const response = cred.response as AuthenticatorAssertionResponse;

  return api<{ accessToken: string }>("/api/v1/auth/webauthn/verify", {
    method: "POST",
    body: JSON.stringify({
      id: cred.id,
      rawId: bufferToBase64url(cred.rawId),
      type: cred.type,
      response: {
        clientDataJSON: bufferToBase64url(response.clientDataJSON),
        authenticatorData: bufferToBase64url(response.authenticatorData),
        signature: bufferToBase64url(response.signature),
        userHandle: response.userHandle ? bufferToBase64url(response.userHandle) : null,
      },
    }),
  });
}

function base64urlToBuffer(s: string): ArrayBuffer {
  const pad = "=".repeat((4 - (s.length % 4)) % 4);
  const b64 = (s + pad).replace(/-/g, "+").replace(/_/g, "/");
  const bin = atob(b64);
  const buf = new Uint8Array(bin.length);
  for (let i = 0; i < bin.length; i++) buf[i] = bin.charCodeAt(i);
  return buf.buffer;
}

function bufferToBase64url(buf: ArrayBuffer): string {
  const bytes = new Uint8Array(buf);
  let bin = "";
  for (let i = 0; i < bytes.length; i++) bin += String.fromCharCode(bytes[i]);
  return btoa(bin).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}
