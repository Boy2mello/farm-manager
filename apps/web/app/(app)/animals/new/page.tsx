"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import {
  ArrowLeft,
  Sprout,
  Save,
  CalendarDays,
  AlertCircle,
} from "lucide-react";
import { api } from "@/lib/api/client";
import { PageHeader } from "@/components/ui/page-header";
import { Section } from "@/components/ui/section";
import { Button } from "@/components/ui/button";
import { useToast } from "@/components/ui/toast";
import { AnimalPicker } from "@/components/animal-picker";

const SCHEMA = z.object({
  primaryName: z
    .string()
    .max(120, "Name too long.")
    .optional()
    .or(z.literal("")),
  sex: z.coerce.number().int().min(1).max(2),
  dob: z.string().min(1, "Date of birth is required."),
  dobPrecision: z.coerce.number().int().min(1).max(3), // 1 = Day, 2 = Month, 3 = Year
  source: z.coerce.number().int().min(1).max(5),
  aliasesText: z.string().max(300).optional().or(z.literal("")),
  damId: z.string().uuid().optional().or(z.literal("")),
  sireId: z.string().uuid().optional().or(z.literal("")),
});
type FormValues = z.infer<typeof SCHEMA>;

type RegisterResponse = { animalId: string; codeName: string };

const SOURCE_OPTIONS: Array<{ value: number; label: string; description: string }> = [
  { value: 1, label: "Born on farm", description: "Calf was born here" },
  { value: 2, label: "Purchased", description: "Bought in from another farm" },
  { value: 3, label: "Inherited", description: "Received from family / estate" },
  { value: 4, label: "Transferred in", description: "Moved from a sub-herd" },
  { value: 5, label: "Legacy", description: "Already on the farm at go-live" },
];

const DOB_PRECISION_OPTIONS: Array<{ value: number; label: string; hint: string }> = [
  { value: 1, label: "Exact date", hint: "Day, month, year" },
  { value: 2, label: "Month only", hint: "Approximate — fall back to the 1st" },
  { value: 3, label: "Year only", hint: "Approximate — fall back to Jan 1" },
];

export default function NewAnimalPage() {
  const router = useRouter();
  const search = useSearchParams();
  const toast = useToast();
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(SCHEMA),
    defaultValues: {
      primaryName: "",
      sex: 1,
      dob: new Date().toISOString().slice(0, 10),
      dobPrecision: 1,
      source: 1,
      aliasesText: "",
      damId: search.get("damId") ?? "",
      sireId: search.get("sireId") ?? "",
    },
  });

  const sex = Number(watch("sex"));
  const precision = Number(watch("dobPrecision"));
  const source = Number(watch("source"));

  async function onSubmit(values: FormValues) {
    setSubmitting(true);
    setError(null);
    try {
      const aliases = (values.aliasesText ?? "")
        .split(/[,;]/)
        .map((a) => a.trim())
        .filter(Boolean);

      const res = await api<RegisterResponse>("/api/v1/animals", {
        method: "POST",
        body: JSON.stringify({
          primaryName: values.primaryName?.trim() || null,
          sex: values.sex,
          dob: values.dob,
          dobPrecision: values.dobPrecision,
          source: values.source,
          damId: values.damId || null,
          sireId: values.sireId || null,
          aliases: aliases.length > 0 ? aliases : null,
        }),
      });

      toast.success(`Registered as ${res.codeName}`, "Code-name is auto-issued and immutable.");
      router.push(`/animals/${res.animalId}`);
    } catch (e) {
      const msg = e instanceof Error ? e.message : "Couldn't register animal.";
      setError(msg);
      toast.error("Registration failed", msg);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <section className="space-y-5 animate-fade-in">
      <Link
        href="/animals"
        className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="h-3 w-3" /> Back to herd
      </Link>

      <PageHeader
        icon={<Sprout className="h-6 w-6" />}
        title="Register animal"
        description="Code-name (e.g. L-2026-005) is auto-issued the moment you save."
      />

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        {/* Identity */}
        <Section title="Identity" description="Name and aliases — leave name blank if you're not sure yet." icon={<Sprout className="h-4 w-4" />}>
          <div className="space-y-3">
            <Field label="Primary name" optional error={errors.primaryName?.message}>
              <input
                type="text"
                placeholder="e.g. Mantabole"
                className="input"
                {...register("primaryName")}
              />
            </Field>

            <Field label="Aliases" optional>
              <input
                type="text"
                placeholder="comma-separated, e.g. Tiki, Mmadikrempe"
                className="input"
                {...register("aliasesText")}
              />
              <p className="text-[11px] text-muted-foreground">
                Aliases are searchable across the whole app.
              </p>
            </Field>

            <Field label="Sex" required>
              <div className="grid grid-cols-2 gap-2">
                <SegButton
                  active={sex === 1}
                  onClick={() => setValue("sex", 1, { shouldValidate: true })}
                  label="Female"
                  emoji="♀"
                />
                <SegButton
                  active={sex === 2}
                  onClick={() => setValue("sex", 2, { shouldValidate: true })}
                  label="Male"
                  emoji="♂"
                />
              </div>
            </Field>
          </div>
        </Section>

        {/* DOB */}
        <Section title="Date of birth" description="Pick the best precision you have." icon={<CalendarDays className="h-4 w-4" />}>
          <div className="space-y-3">
            <Field label="DOB" required error={errors.dob?.message}>
              <input type="date" className="input" {...register("dob")} />
            </Field>
            <Field label="Precision">
              <div className="grid gap-1.5">
                {DOB_PRECISION_OPTIONS.map((opt) => {
                  const active = precision === opt.value;
                  return (
                    <button
                      key={opt.value}
                      type="button"
                      onClick={() => setValue("dobPrecision", opt.value, { shouldValidate: true })}
                      className={`flex w-full items-start justify-between gap-2 rounded-md border px-3 py-2 text-left text-sm transition ${
                        active ? "border-primary bg-primary/10" : "bg-card hover:bg-accent/40"
                      }`}
                    >
                      <span className="min-w-0">
                        <span className="font-medium">{opt.label}</span>
                        <span className="ml-2 text-[11px] text-muted-foreground">{opt.hint}</span>
                      </span>
                      <span
                        className={`mt-0.5 inline-flex h-4 w-4 shrink-0 items-center justify-center rounded-full border ${
                          active ? "border-primary bg-primary" : "border-border"
                        }`}
                      >
                        {active && <span className="h-2 w-2 rounded-full bg-primary-foreground" />}
                      </span>
                    </button>
                  );
                })}
              </div>
            </Field>
          </div>
        </Section>

        {/* Source */}
        <Section title="How does this animal join the herd?" icon={<Sprout className="h-4 w-4" />}>
          <ul className="grid gap-1.5">
            {SOURCE_OPTIONS.map((opt) => {
              const active = source === opt.value;
              return (
                <li key={opt.value}>
                  <button
                    type="button"
                    onClick={() => setValue("source", opt.value, { shouldValidate: true })}
                    className={`flex w-full items-start justify-between gap-2 rounded-md border px-3 py-2 text-left text-sm transition ${
                      active ? "border-primary bg-primary/10" : "bg-card hover:bg-accent/40"
                    }`}
                  >
                    <span className="min-w-0">
                      <span className="font-medium">{opt.label}</span>
                      <span className="ml-2 text-[11px] text-muted-foreground">{opt.description}</span>
                    </span>
                    <span
                      className={`mt-0.5 inline-flex h-4 w-4 shrink-0 items-center justify-center rounded-full border ${
                        active ? "border-primary bg-primary" : "border-border"
                      }`}
                    >
                      {active && <span className="h-2 w-2 rounded-full bg-primary-foreground" />}
                    </span>
                  </button>
                </li>
              );
            })}
          </ul>
        </Section>

        {/* Lineage */}
        <Section title="Lineage" description="Pick the dam and/or sire if known — both optional.">
          <div className="space-y-3">
            <Field label="Dam (mother)" optional>
              <AnimalPicker
                value={watch("damId") || null}
                onChange={(id) =>
                  setValue("damId", id ?? "", { shouldValidate: true, shouldDirty: true })
                }
                sex={1}
                placeholder="Pick the cow…"
              />
            </Field>
            <Field label="Sire (father)" optional>
              <AnimalPicker
                value={watch("sireId") || null}
                onChange={(id) =>
                  setValue("sireId", id ?? "", { shouldValidate: true, shouldDirty: true })
                }
                sex={2}
                placeholder="Pick the bull…"
              />
            </Field>
          </div>
        </Section>

        {error && (
          <p className="flex items-start gap-2 rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
            {error}
          </p>
        )}

        <div className="sticky bottom-20 z-30 flex items-center justify-end gap-2 rounded-xl border bg-card p-3 shadow-md md:bottom-0">
          <Button
            type="submit"
            disabled={submitting}
            size="lg"
            leading={<Save className="h-4 w-4" />}
            fullWidth
          >
            {submitting ? "Registering…" : "Register animal"}
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
        {optional && (
          <span className="text-[10px] uppercase tracking-wide text-muted-foreground">
            optional
          </span>
        )}
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
        active ? "border-primary bg-primary text-primary-foreground" : "bg-card hover:bg-accent"
      }`}
    >
      <span className="text-base">{emoji}</span>
      {label}
    </button>
  );
}
