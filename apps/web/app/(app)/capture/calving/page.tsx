"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Camera } from "lucide-react";
import { api } from "@/lib/api/client";
import { enqueueEvent } from "@/lib/sync/background-sync";
import { BarcodeScanner } from "@/components/capture/barcode-scanner";
import { VoiceRecorder } from "@/components/capture/voice-recorder";
import { tryGetCoarseLocation } from "@/lib/hardware/geolocation";

const schema = z.object({
  damId: z.string().uuid(),
  calvingDate: z.string(),
  calfSex: z.coerce.number().int().min(1).max(2),
  sireId: z.string().uuid().optional().or(z.literal("")),
  sireExternalNote: z.string().max(200).optional(),
  difficultyScore: z.coerce.number().int().min(1).max(5),
  assistanceRequired: z.coerce.boolean(),
  placentaDelivered: z.coerce.boolean(),
  motheringAbility: z.coerce.number().int().min(1).max(5).optional(),
  stillbirth: z.coerce.boolean(),
  calfWeightKg: z.coerce.number().positive().optional(),
  calfVigour: z.coerce.number().int().min(1).max(5).optional(),
  notes: z.string().max(1000).optional(),
});

type FormValues = z.infer<typeof schema>;

export default function CalvingCapturePage() {
  const router = useRouter();
  const search = useSearchParams();
  const [toast, setToast] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [scanning, setScanning] = useState(false);
  const [voiceBlob, setVoiceBlob] = useState<Blob | null>(null);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      damId: search.get("damId") ?? "",
      calvingDate: new Date().toISOString().slice(0, 10),
      calfSex: 1,
      difficultyScore: 1,
      assistanceRequired: false,
      placentaDelivered: true,
      stillbirth: false,
    },
  });

  async function onSubmit(values: FormValues) {
    setError(null);
    setToast(null);

    // Tag the event with the current GPS fix (best-effort).
    const location = await tryGetCoarseLocation();

    const payload = {
      ...values,
      sireId: values.sireId || null,
      capturedLocation: location,
      hasVoiceNote: voiceBlob != null,
    };

    // Offline / unreliable network: queue and let Background Sync flush it.
    if (typeof navigator !== "undefined" && !navigator.onLine) {
      await enqueueEvent("/api/v1/animals/calvings", "POST", payload);
      setToast("Saved locally. Will sync on reconnect.");
      setTimeout(() => router.push("/animals"), 1500);
      return;
    }

    try {
      const res = await api<{ calfCodeName: string }>("/api/v1/animals/calvings", {
        method: "POST",
        body: JSON.stringify(payload),
      });
      setToast(`Calf ${res.calfCodeName} registered.`);
      setTimeout(() => router.push("/animals"), 1500);
    } catch (e) {
      // Network errors fall through to the queue so the user is never blocked in the kraal.
      const msg = e instanceof Error ? e.message : "Capture failed";
      if (msg.toLowerCase().includes("fetch") || msg === "Failed to fetch") {
        await enqueueEvent("/api/v1/animals/calvings", "POST", payload);
        setToast("Saved locally. Will sync on reconnect.");
        setTimeout(() => router.push("/animals"), 1500);
      } else {
        setError(msg);
      }
    }
  }

  function applyScannedTag(value: string) {
    setValue("damId", value, { shouldValidate: true, shouldDirty: true });
    setScanning(false);
  }

  return (
    <section className="space-y-4">
      {scanning && (
        <BarcodeScanner onDetect={applyScannedTag} onClose={() => setScanning(false)} />
      )}

      <header>
        <h1 className="text-2xl font-bold">Record calving</h1>
        <p className="text-sm text-muted-foreground">
          Single-screen capture per spec §13.1. Code-name is auto-issued on save.
        </p>
      </header>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 rounded-lg border bg-card p-4 shadow">
        <Field label="Dam (animal id)" error={errors.damId?.message}>
          <div className="flex gap-2">
            <input type="text" className="input flex-1" {...register("damId")} />
            <button
              type="button"
              onClick={() => setScanning(true)}
              className="flex items-center gap-1 rounded-md border px-3 py-2 text-sm"
              aria-label="Scan ear tag"
            >
              <Camera className="h-4 w-4" /> Scan
            </button>
          </div>
        </Field>

        <Field label="Calving date" error={errors.calvingDate?.message}>
          <input type="date" className="input" {...register("calvingDate")} />
        </Field>

        <Field label="Calf sex" error={errors.calfSex?.message}>
          <select className="input" {...register("calfSex")}>
            <option value={1}>Female</option>
            <option value={2}>Male</option>
          </select>
        </Field>

        <Field label="Sire (animal id, optional)" error={errors.sireId?.message}>
          <input type="text" className="input" placeholder="leave blank if external" {...register("sireId")} />
        </Field>

        <Field label="Difficulty (1–5)" error={errors.difficultyScore?.message}>
          <input type="number" min={1} max={5} className="input" {...register("difficultyScore")} />
        </Field>

        <div className="grid gap-3 sm:grid-cols-2">
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" {...register("assistanceRequired")} /> Assistance required
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" {...register("placentaDelivered")} /> Placenta delivered
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" {...register("stillbirth")} /> Stillbirth
          </label>
        </div>

        <Field label="Calf weight (kg, optional)">
          <input type="number" step={0.1} className="input" {...register("calfWeightKg")} />
        </Field>

        <Field label="Notes (optional)">
          <textarea className="input min-h-24" {...register("notes")} />
        </Field>

        <VoiceRecorder onRecorded={setVoiceBlob} />

        {error && (
          <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error}
          </p>
        )}
        {toast && (
          <p className="rounded-md bg-primary/10 px-3 py-2 text-sm font-medium text-primary">
            {toast}
          </p>
        )}

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-md bg-primary py-3 font-medium text-primary-foreground disabled:opacity-50"
        >
          {isSubmitting ? "Saving…" : "Save calving"}
        </button>
      </form>

      <style jsx>{`
        :global(.input) {
          width: 100%;
          border-radius: 6px;
          border: 1px solid hsl(var(--border));
          background-color: hsl(var(--background));
          padding: 0.5rem 0.75rem;
          font-size: 0.875rem;
        }
      `}</style>
    </section>
  );
}

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1">
      <label className="text-sm font-medium">{label}</label>
      {children}
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  );
}
