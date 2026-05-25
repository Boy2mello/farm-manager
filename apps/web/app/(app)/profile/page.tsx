"use client";

import { useState } from "react";
import { usePreferences } from "@/lib/providers/preferences-provider";
import { ensurePushSubscription } from "@/lib/notifications/push";
import { setAccessToken } from "@/lib/api/client";

export default function ProfilePage() {
  const prefs = usePreferences();
  const [pushStatus, setPushStatus] = useState<string | null>(null);

  async function enablePush() {
    setPushStatus(null);
    const sub = await ensurePushSubscription();
    setPushStatus(sub ? "Web Push enabled." : "Couldn't enable push. Check permission settings.");
  }

  function signOut() {
    setAccessToken(null);
    window.location.href = "/login";
  }

  return (
    <section className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold">Profile</h1>
        <p className="text-sm text-muted-foreground">
          Field-tuned preferences are saved on this device.
        </p>
      </header>

      <Card title="Field mode" description="Glove-friendly buttons and a text-only fallback when the signal is weak.">
        <Toggle
          label="Glove mode"
          description="Larger buttons; long-press to confirm destructive actions."
          checked={prefs.gloveMode}
          onChange={prefs.setGloveMode}
        />
        <Toggle
          label="Quiet-rural mode"
          description="Hide images and video to save bandwidth."
          checked={prefs.quietRuralMode}
          onChange={prefs.setQuietRuralMode}
        />
        <Toggle
          label="Auto-switch on slow networks"
          description="Detect < 100 kbps and enable quiet-rural mode automatically."
          checked={prefs.autoQuietRural}
          onChange={prefs.setAutoQuietRural}
        />
      </Card>

      <Card title="Notifications" description="WhatsApp + Web Push are wired; tier-D/E and inbreeding blocks always override quiet hours.">
        <button
          onClick={enablePush}
          className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground"
        >
          Enable Web Push on this device
        </button>
        {pushStatus && <p className="mt-2 text-sm text-muted-foreground">{pushStatus}</p>}
      </Card>

      <Card title="Session">
        <button
          onClick={signOut}
          className="rounded-md border px-4 py-2 text-sm font-medium text-destructive"
        >
          Sign out
        </button>
      </Card>
    </section>
  );
}

function Card({
  title,
  description,
  children,
}: {
  title: string;
  description?: string;
  children: React.ReactNode;
}) {
  return (
    <section className="space-y-3 rounded-lg border bg-card p-4">
      <header>
        <h2 className="font-semibold">{title}</h2>
        {description && (
          <p className="text-xs text-muted-foreground">{description}</p>
        )}
      </header>
      {children}
    </section>
  );
}

function Toggle({
  label,
  description,
  checked,
  onChange,
}: {
  label: string;
  description: string;
  checked: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <label className="flex cursor-pointer items-center justify-between gap-4 rounded-md border bg-background p-3">
      <span className="space-y-0.5">
        <span className="block text-sm font-medium">{label}</span>
        <span className="block text-xs text-muted-foreground">{description}</span>
      </span>
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        className="h-5 w-5"
      />
    </label>
  );
}
