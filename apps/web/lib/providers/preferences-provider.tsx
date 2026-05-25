"use client";

import { createContext, useContext, useEffect, useMemo, useState } from "react";

type Preferences = {
  gloveMode: boolean;
  quietRuralMode: boolean;
  autoQuietRural: boolean;
  language: "en" | "af" | "tn" | "zu" | "st";
};

type PreferencesContextValue = Preferences & {
  setGloveMode: (v: boolean) => void;
  setQuietRuralMode: (v: boolean) => void;
  setAutoQuietRural: (v: boolean) => void;
  setLanguage: (lang: Preferences["language"]) => void;
};

const DEFAULTS: Preferences = {
  gloveMode: false,
  quietRuralMode: false,
  autoQuietRural: true,
  language: "en",
};

const STORAGE_KEY = "fm.preferences";

const Ctx = createContext<PreferencesContextValue | null>(null);

export function UserPreferencesProvider({ children }: { children: React.ReactNode }) {
  const [prefs, setPrefs] = useState<Preferences>(DEFAULTS);

  // Hydrate from localStorage.
  useEffect(() => {
    if (typeof window === "undefined") return;
    try {
      const raw = window.localStorage.getItem(STORAGE_KEY);
      if (raw) setPrefs({ ...DEFAULTS, ...JSON.parse(raw) });
    } catch {
      /* ignore */
    }
  }, []);

  // Persist.
  useEffect(() => {
    if (typeof window === "undefined") return;
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(prefs));
    document.documentElement.dataset.gloveMode = prefs.gloveMode ? "on" : "off";
    document.documentElement.dataset.quietRural =
      prefs.quietRuralMode ? "on" : "off";
  }, [prefs]);

  // Auto quiet-rural: detect link speed via the Network Information API (spec §7.2 < 100 kbps).
  useEffect(() => {
    if (typeof navigator === "undefined" || !prefs.autoQuietRural) return;
    const conn = (navigator as unknown as { connection?: NetworkInfo }).connection;
    if (!conn) return;

    function evaluate() {
      const slow = conn!.downlink !== undefined && conn!.downlink < 0.1;
      const cellular = conn!.effectiveType === "slow-2g" || conn!.effectiveType === "2g";
      setPrefs((p) => ({ ...p, quietRuralMode: slow || cellular }));
    }

    evaluate();
    conn.addEventListener?.("change", evaluate);
    return () => conn.removeEventListener?.("change", evaluate);
  }, [prefs.autoQuietRural]);

  const value = useMemo<PreferencesContextValue>(
    () => ({
      ...prefs,
      setGloveMode: (v) => setPrefs((p) => ({ ...p, gloveMode: v })),
      setQuietRuralMode: (v) => setPrefs((p) => ({ ...p, quietRuralMode: v })),
      setAutoQuietRural: (v) => setPrefs((p) => ({ ...p, autoQuietRural: v })),
      setLanguage: (lang) => setPrefs((p) => ({ ...p, language: lang })),
    }),
    [prefs],
  );

  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function usePreferences(): PreferencesContextValue {
  const ctx = useContext(Ctx);
  if (!ctx) {
    throw new Error("usePreferences must be used inside <UserPreferencesProvider>.");
  }
  return ctx;
}

interface NetworkInfo extends EventTarget {
  downlink?: number;
  effectiveType?: "slow-2g" | "2g" | "3g" | "4g";
  saveData?: boolean;
  addEventListener?: (type: "change", listener: () => void) => void;
  removeEventListener?: (type: "change", listener: () => void) => void;
}
