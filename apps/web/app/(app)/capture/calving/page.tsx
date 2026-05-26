"use client";

import Link from "next/link";
import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { useForm, type UseFormRegister, type Path } from "react-hook-form";
import { z } from "zod";
import { Camera, ArrowLeft, Baby, Save, Calendar, Weight, AlertTriangle } from "lucide-react";
import { api } from "@/lib/api/client";
import { enqueueEvent } from "@/lib/sync/background-sync";
import { BarcodeScanner } from "@/components/capture/barcode-scanner";
import { VoiceRecorder } from "@/components/capture/voice-recorder";
import { tryGetCoarseLocation } from "@/lib/hardware/geolocation";
import { PageHeader } from "@/components/ui/page-header";
import { Section } from "@/components/ui/section";
import { Button } from "@/components/ui/button";
import { useToast } from "@/components/ui/toast";

const schema = z.object({
  damId: z.string().uuid("Pick a dam from the herd or scan the ear tag."),
  calvingDate: z.string().min(1, "Date required."),
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
  const toast = useToast();
  const [scanning, setScanning] = useState(false);
  const [_voiceBlob, setVoiceBlob] = useState<Blob | null>(null);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
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

  const stillbirth = watch("stillbirth");
  const difficulty = watch("difficultyScore");
  const calfSex = watch("calfSex");

  async function onSubmit(values: FormValues) {
    const location = await tryGetCoarseLocation();
    const payload = { ...values, sireId: values.sireId || null, capturedLocation: location };

    if (typeof navigator !== "undefined" && !navigator.onLine) {
      await enqueueEvent("/api/v1/animals/calvings", "POST", payload);
      toast.info("Saved locally", "Will sync the next time you're online.");
      setTimeout(() => router.push("/animals"), 1200);
      return;
    }

    try {
      const res = await api<{ calfCodeName: string }>("/api/v1/animals/calvings", {
        method: "POST",
        body: JSON.stringify(payload),
      });
      toast.success(`Calf ${res.calfCodeName} registered`, "The code-name is auto-issued and printable.");
      setTimeout(() => router.push("/animals"), 1200);
    } catch (e) {
      const msg = e instanceof Error ? e.message : "Capture failed";
      if (msg.toLowerCase().includes("fetch")) {
        await enqueueEvent("/api/v1/animals/calvings", "POST", payload);
        toast.info("Saved locally", "Will sync the next time you're online.");
        setTimeout(() => router.push("/animals"), 1200);
      } else {
        toast.error("Capture failed", msg);
      }
    }
  }

  function applyScannedTag(value: string) {
    setValue("damId", value, { shouldValidate: true, shouldDirty: true });
    setScanning(false);
    toast.info("Tag scanned", value);
  }

  return (
    <section className="space-y-5 animate-fade-in">
      {scanning && (
        <BarcodeScanner onDetect={applyScannedTag} onClose={() => setScanning(false)} />
      )}

      <Link
        href="/animals"
        className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="h-3 w-3" /> Back to herd
      </Link>

      <PageHeader
        icon={<Baby className="h-6 w-6" />}
        title="Record calving"
        description="Single-screen capture. Code-name is auto-issued on save."
      />

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <Section
          title="Dam & date"
          description="Who calved, and when"
          icon={<Calendar className="h-4 w-4" />}
        >
          <div className="space-y-3">
            <Field label="Dam" error={errors.damId?.message} required>
              <div className="flex gap-2">
                <input
                  type="text"
                  className="input flex-1"
                  placeholder="UUID or scan ear tag"
                  {...register("damId")}
                />
                <Button
                  type="button"
                  variant="outline"
                  size="md"
                  leading={<Camera className="h-4 w-4" />}
                  onClick={() => setScanning(true)}
                >
                  Scan
                </Button>
              </div>
            </Field>

            <Field label="Calving date" error={errors.calvingDate?.message} required>
              <input type="date" className="input" {...register("calvingDate")} />
            </Field>
          </div>
        </Section>

        <Section title="Calf" icon={<Baby className="h-4 w-4" />}>
          <div className="space-y-3">
            <Field label="Sex" required>
              <div className="grid grid-cols-2 gap-2">
                <SegButton
                  active={Number(calfSex) === 1}
                  onClick={() => setValue("calfSex", 1)}
                  label="Female"
                  emoji="♀"
                />
                <SegButton
                  active={Number(calfSex) === 2}
                  onClick={() => setValue("calfSex", 2)}
                  label="Male"
                  emoji="♂"
                />
              </div>
            </Field>

            <Field label="Calf weight (kg)" optional>
              <div className="relative">
                <Weight className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <input
                  type="number"
                  step={0.1}
                  placeholder="optional"
                  className="input pl-9"
                  {...register("calfWeightKg")}
                />
              </div>
            </Field>

            <Field label="Stillbirth?">
              <Switch
                checked={stillbirth}
                onChange={(v) => setValue("stillbirth", v)}
                onLabel="Yes — stillborn"
                offLabel="No — live birth"
                tone={stillbirth ? "danger" : "success"}
              />
            </Field>
          </div>
        </Section>

        <Section title="Difficulty & care" icon={<AlertTriangle className="h-4 w-4" />}>
          <div className="space-y-3">
            <Field label={`Difficulty: ${["", "Easy", "Mild", "Moderate", "Hard", "Severe"][Number(difficulty)]}`} required>
              <div className="grid grid-cols-5 gap-1">
                {[1, 2, 3, 4, 5].map((n) => (
                  <button
                    key={n}
                    type="button"
                    onClick={() => setValue("difficultyScore", n)}
                    className={`h-10 rounded-md border text-sm font-medium transition ${
                      Number(difficulty) === n
                        ? n >= 4
                          ? "border-rose-300 bg-rose-100 text-rose-800"
                          : "border-primary bg-primary text-primary-foreground"
                        : "bg-card hover:bg-accent"
                    }`}
                  >
                    {n}
                  </button>
                ))}
              </div>
            </Field>

            <div className="grid gap-3 sm:grid-cols-2">
              <Checkbox label="Assistance required" name="assistanceRequired" register={register} />
              <Checkbox label="Placenta delivered" name="placentaDelivered" register={register} />
            </div>
          </div>
        </Section>

        <Section title="Notes (optional)">
          <textarea
            className="input min-h-24"
            placeholder="Anything noteworthy — heat behaviour, calf vigour, vet notes…"
            {...register("notes")}
          />
          <VoiceRecorder onRecorded={setVoiceBlob} />
        </Section>

        <div className="sticky bottom-20 z-30 flex items-center justify-end gap-2 rounded-xl border bg-card p-3 shadow-md sm:bottom-0">
          <Button
            type="submit"
            disabled={isSubmitting}
            size="lg"
            leading={<Save className="h-4 w-4" />}
            fullWidth
          >
            {isSubmitting ? "Saving…" : "Save calving"}
          </Button>
        </div>
      </form>

      <style jsx>{`
        :global(.input) {
          width: 100%;
          border-radius: 8px;
          border: 1px solid hsl(var(--border));
          background-color: hsl(var(--background));
          padding: 0.625rem 0.75rem;
          font-size: 0.875rem;
          box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.04);
        }
        :global(.input:focus-visible) {
          outline: none;
          border-color: hsl(var(--ring));
          box-shadow: 0 0 0 3px hsl(var(--ring) / 0.25);
        }
      `}</style>
    </section>
  );
}

function Field({
  label,
  children,
  error,
  required,
  optional,
}: {
  label: string;
  children: React.ReactNode;
  error?: string;
  required?: boolean;
  optional?: boolean;
}) {
  return (
    <div className="space-y-1.5">
      <label className="flex items-center gap-1 text-sm font-medium">
        {label}
        {required && <span className="text-destructive">*</span>}
        {optional && <span className="text-[10px] uppercase tracking-wide text-muted-foreground">optional</span>}
      </label>
      {children}
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  );
}

function SegButton({
  active,
  onClick,
  label,
  emoji,
}: {
  active: boolean;
  onClick: () => void;
  label: string;
  emoji: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`flex h-12 items-center justify-center gap-2 rounded-md border text-sm font-medium transition ${
        active
          ? "border-primary bg-primary text-primary-foreground"
          : "bg-card hover:bg-accent"
      }`}
    >
      <span className="text-base">{emoji}</span>
      {label}
    </button>
  );
}

function Switch({
  checked,
  onChange,
  onLabel,
  offLabel,
  tone,
}: {
  checked: boolean;
  onChange: (v: boolean) => void;
  onLabel: string;
  offLabel: string;
  tone: "success" | "danger";
}) {
  return (
    <div className="grid grid-cols-2 gap-2">
      <button
        type="button"
        onClick={() => onChange(false)}
        className={`h-11 rounded-md border text-sm font-medium transition ${
          !checked
            ? tone === "success"
              ? "border-emerald-300 bg-emerald-50 text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-200"
              : "bg-card"
            : "bg-card hover:bg-accent"
        }`}
      >
        {offLabel}
      </button>
      <button
        type="button"
        onClick={() => onChange(true)}
        className={`h-11 rounded-md border text-sm font-medium transition ${
          checked
            ? tone === "danger"
              ? "border-rose-300 bg-rose-50 text-rose-800 dark:bg-rose-950/40 dark:text-rose-200"
              : "border-primary bg-primary text-primary-foreground"
            : "bg-card hover:bg-accent"
        }`}
      >
        {onLabel}
      </button>
    </div>
  );
}

function Checkbox({
  label,
  name,
  register,
}: {
  label: string;
  name: Path<FormValues>;
  register: UseFormRegister<FormValues>;
}) {
  return (
    <label className="flex cursor-pointer items-center gap-2 rounded-md border bg-card px-3 py-2 text-sm hover:bg-accent/40">
      <input type="checkbox" {...register(name)} className="h-4 w-4" />
      {label}
    </label>
  );
}
