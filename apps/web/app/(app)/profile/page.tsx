"use client";

import { useState } from "react";
import { User, Bell, Shield, LogOut, Wifi, Hand } from "lucide-react";
import { usePreferences } from "@/lib/providers/preferences-provider";
import { ensurePushSubscription } from "@/lib/notifications/push";
import { clearSessionAndRedirect } from "@/lib/api/client";
import { useToast } from "@/components/ui/toast";
import { PageHeader } from "@/components/ui/page-header";
import { Section } from "@/components/ui/section";
import { Button } from "@/components/ui/button";

export default function ProfilePage() {
  const prefs = usePreferences();
  const toast = useToast();
  const [pushBusy, setPushBusy] = useState(false);

  async function enablePush() {
    setPushBusy(true);
    try {
      const sub = await ensurePushSubscription();
      if (sub) toast.success("Push notifications enabled", "This device will now receive alerts.");
      else toast.warning("Push not enabled", "Permission was denied or the browser doesn't support it.");
    } catch (e) {
      toast.error("Couldn't enable push", e instanceof Error ? e.message : "Try again.");
    } finally {
      setPushBusy(false);
    }
  }

  function signOut() {
    clearSessionAndRedirect("manual");
  }

  return (
    <section className="space-y-5 animate-fade-in">
      <PageHeader
        icon={<User className="h-6 w-6" />}
        title="Profile & preferences"
        description="Per-device field-tuned settings. Saved locally."
      />

      <Section
        title="Field mode"
        description="Optimise the interface for working in the kraal"
        icon={<Hand className="h-4 w-4" />}
      >
        <div className="space-y-2">
          <Toggle
            label="Glove mode"
            description="Larger buttons; long-press to confirm destructive actions."
            checked={prefs.gloveMode}
            onChange={prefs.setGloveMode}
          />
          <Toggle
            label="Quiet-rural mode"
            description="Hide images and video to save bandwidth on weak signal."
            checked={prefs.quietRuralMode}
            onChange={prefs.setQuietRuralMode}
          />
          <Toggle
            label="Auto-switch on slow networks"
            description="Detect below ~100 kbps and enable quiet-rural automatically."
            checked={prefs.autoQuietRural}
            onChange={prefs.setAutoQuietRural}
          />
        </div>
      </Section>

      <Section
        title="Notifications"
        description="WhatsApp + Web Push fire in parallel. Tier downgrades to D/E and inbreeding blocks always override quiet hours."
        icon={<Bell className="h-4 w-4" />}
      >
        <Button onClick={enablePush} disabled={pushBusy} leading={<Wifi className="h-4 w-4" />}>
          {pushBusy ? "Working…" : "Enable Web Push on this device"}
        </Button>
      </Section>

      <Section title="Session" icon={<Shield className="h-4 w-4" />}>
        <Button variant="outline" onClick={signOut} leading={<LogOut className="h-4 w-4" />}>
          Sign out
        </Button>
      </Section>
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
    <label className="flex cursor-pointer items-center justify-between gap-4 rounded-lg border bg-background p-3 transition hover:bg-accent/30">
      <span className="space-y-0.5">
        <span className="block text-sm font-medium">{label}</span>
        <span className="block text-xs text-muted-foreground">{description}</span>
      </span>
      <span className="relative inline-flex h-6 w-11 shrink-0 items-center">
        <input
          type="checkbox"
          checked={checked}
          onChange={(e) => onChange(e.target.checked)}
          className="peer sr-only"
        />
        <span className="absolute inset-0 cursor-pointer rounded-full bg-muted transition peer-checked:bg-primary" />
        <span className="absolute left-0.5 h-5 w-5 cursor-pointer rounded-full bg-card shadow-xs transition peer-checked:translate-x-5" />
      </span>
    </label>
  );
}
