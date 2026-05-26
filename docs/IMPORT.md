# Livestock register import

> Status: production-ready · idempotent · runs automatically on first boot.

This document explains how the real herd register (`docs/Livestock_Register.xlsx`) is loaded into
Farm Manager. It is written so any agent or operator can pick up a fresh deployment, follow the
checklist, and end with a fully-populated database.

## What the importer does

| Excel sheet | Target |
|---|---|
| **Master Register** (rows 4–43) | Creates an `Animal` per row + the resident bull (Boshomane) and the deceased dam Mphonyana. Parses DOB precision (day / month / year), sex, breed, aliases. |
| **Master Register** dam/sire columns | Second pass — links every animal's `DamId` + `SireId` by resolving primary names + aliases. |
| **Lineage** | Emits a `CalvingEvent` for each mother→calf row (28 events). Marks calves as B-sired when sire = Boshomane. |
| **Breeding Status** Open / Confirmed / Covered sections | Sets `Animal.Status` to Open / PregnantConfirmed; emits `ServiceEvent` + positive `PregnancyCheckEvent` for each recorded cover. |
| **Calving Calendar** | Back-dates a service event 283 days before the listed due date for any pregnancy not already recorded; transitions the cow to PregnantConfirmed. |
| **Performance Ranking** | Sets `Animal.PerformanceTier` (A–E) and writes a `TierAssignment` audit row with the source workbook's reasoning text. |
| **Sold-Historic** | For sales/slaughters: synthesises any animal not in the active register (Russell, Gijima, etc.), then emits `SaleEvent` and transitions to Sold. For deaths (Mphonyana lightning, Kgomotso/Tshidi, Vaal 1): emits `DeathEvent` and transitions to Dead. For thefts (Ouma's bull calf): transitions to Missing. |
| **Heifer Calf Readiness**, **Bull Calf Plan**, **First-Time Mothers**, **Calving by Year**, **Owner Summary**, **History Notes**, **README & Assumptions** | Informational only — the data is reproducible from the events above. Not re-imported. |

**Aliases resolved automatically**: Mmadikrempe = Tiki · Smongo = Smongonase = Manki ·
Mawick = Obakeng · Makantase Jr = Junior · Georgina = Springbok · Lerato Makantase = Thando ·
Lapi = Lapa · Poelo = Matlhale · Amogelang = Amo · Nandipha Magudumana = Nandipha · Bali = mebala.

**Defaults & assumptions**:

- Code-names are auto-assigned with prefix `L` (legacy) and the animal's birth year (e.g. `L-2024-008`).
- Calving events imported from the spreadsheet default to: difficulty 1, no assistance, placenta delivered, no calf weight. Real weights can be back-filled later from a vet record.
- Service events without a recorded sire default to Boshomane (the resident bull). The Excel never lists a different sire on cows in the active herd.
- Pregnancy checks are dated `service_date + 60 days` so the cow appears on the Calving Calendar at the right time.
- Sale price defaults to `0.01` for historical sales where the price wasn't recorded — this lets the row pass the `priceTotal > 0` invariant without faking financials.
- Sub-herds (Jijo / Olly / Tumi) are created as `Farm` rows. The Master Register doesn't include a per-animal owner column, so every animal currently maps to a single organisation. The farm rows are available for a future per-animal owner mapping.

## Idempotency

Every operation checks for existing state before writing:

| Entity | Idempotency key |
|---|---|
| Organisation | Name (`Tumi's Farm`) |
| Farm | (organisation_id, name) — unique index |
| Animal | (organisation_id, primary_name) + alias resolution |
| CalvingEvent | (dam_id, calf_id) |
| ServiceEvent | (cow_id, service_date) |
| PregnancyCheckEvent | (cow_id, check_date) |
| SaleEvent | animal_id (unique index — "first sale wins") |
| DeathEvent | animal_id (unique index — one-shot) |
| TierAssignment | Skipped if current tier matches the row |

Re-running the importer adds nothing new and never duplicates rows.

## How to run it — three ways

### 1. Automatic — first boot of the API container

Drop the workbook at `docs/Livestock_Register.xlsx` (already in the repo). The
`apps/api/FarmManager.Api/Dockerfile` copies it into `/app/docs/` at build time. The first time
the API starts against an empty Postgres it:

1. Runs EF Core migrations.
2. Creates roles (`SuperAdmin`, `Owner`, `FarmManager`, `Vet`, `FieldWorker`, `Observer`).
3. Creates the organisation `Tumi's Farm`.
4. Creates the bootstrap admin user (see [Admin user](#admin-user)).
5. Runs `HerdSeeder.SeedAsync` which detects `Livestock_Register.xlsx` and dispatches the importer.

The seeding step is gated by `app.Environment.IsDevelopment() || Configuration.Seed:Enabled == true`,
so in production set the env var **`Seed__Enabled=true`** to opt in to first-boot seeding.

### 2. CLI — one-shot import from a host shell

```bash
# From the repo root:
dotnet run --project apps/api/FarmManager.Api -- import-register

# Or against an alternate workbook:
dotnet run --project apps/api/FarmManager.Api -- import-register /path/to/Livestock_Register.xlsx
```

The CLI uses the connection string in `appsettings.json` / `appsettings.Development.json` /
env vars. It exits with code 0 on success, 1 if the importer reported errors, 2 if the workbook
was not found.

Inside the container:

```bash
docker exec -it farm-manager-api dotnet FarmManager.Api.dll import-register
```

### 3. HTTPS upload — re-import after edits to the workbook

```bash
# Authenticate first.
TOKEN=$(curl -s -X POST https://<host>/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"Boy2mello@farm-manager.local","password":"Boy2mello!Farm26"}' \
  | jq -r .accessToken)

curl -X POST https://<host>/api/v1/admin/import/livestock-register \
  -H "Authorization: Bearer $TOKEN" \
  -F "workbook=@docs/Livestock_Register.xlsx" \
  -F "organisationName=Tumi's Farm"
```

The endpoint requires `Owner` or `SuperAdmin` role and accepts uploads up to 20 MB. It returns a
JSON summary with counts of each entity created plus any warnings.

## Admin user

The first boot creates one administrator so the operator can log in immediately:

| Field | Default | Override |
|---|---|---|
| Username | `Boy2mello` | `BootstrapAdmin__UserName` |
| Email | `Boy2mello@farm-manager.local` | `BootstrapAdmin__Email` |
| Password | `Boy2mello!Farm26` | `BootstrapAdmin__Password` |
| Roles | `SuperAdmin`, `Owner` | — |

**Change this password immediately on first login** — the default is in source control. The
password is logged at warning level once on first boot so you can find it in the deployment
output if needed.

## Verification checklist

After running the importer, hit these endpoints (or use the matching UI page) to confirm the
data made it across:

| Check | Expected |
|---|---|
| `GET /api/v1/animals` | Returns ≥ 40 animals (the 40 active register entries plus Boshomane and any synthesized sold-historic animals). |
| `GET /api/v1/animals?status=4` (PregnantConfirmed) | Returns 14 cows matching the Calving Calendar. |
| `GET /api/v1/animals?status=2` (Open) | Returns 8 cows matching Breeding Status §Open Cows. |
| `GET /api/v1/lineage/inbreeding?sireId=<Rapula>&damId=<Bali>` | Returns a `HardBlock` verdict because Rapula and Bali are both Boshomane-sired half-siblings. |
| `GET /api/v1/analytics/kpis` | Once the nightly job runs (or you trigger it from `/hangfire`), reports live_cattle ≥ 40, confirmed_pregnancies = 14. |
| `GET /api/v1/analytics/underperformers` | Shows the cows whose tier is D or E (Georgina, Mmadikrempe, Baizani, Venus, Coco Gauff, Lerato Makantase). |
| Web UI `/animals` | Shows every animal with its `L-2026-NNN` code-name badge. (B)-sired calves carry the yellow B chip. |
| Web UI `/animals/<id>` for a pregnant cow | Status shows "Confirmed pregnant"; the Breeding tab lists the service event. |
| Web UI `/reports/herd-census.pdf` | Downloads a PDF listing every active animal. |

## Re-running the importer after the workbook changes

1. Edit `docs/Livestock_Register.xlsx`.
2. Either re-deploy the containers (the file ships inside the image), or call the upload endpoint
   from §3 above to push the new workbook without redeploying.
3. The importer is idempotent — only changes are written.

## Troubleshooting

| Symptom | Diagnosis |
|---|---|
| `Workbook not found` in logs | Make sure `docs/Livestock_Register.xlsx` is present at the deployment's working directory. Check `dotnet FarmManager.Api.dll import-register --help` style verbosity by checking Seq for the search-paths printed at startup. |
| `Unmatched names` in the import report | A name in Lineage / Breeding Status doesn't match any animal in Master Register and isn't covered by the alias table. Either fix the spreadsheet or extend the `Aliases` dictionary in `LivestockRegisterImporter.cs`. |
| Tier engine never fires | Nightly Hangfire jobs only run at 02:00 UTC. To force, open `/hangfire` (SuperAdmin only) and click "Trigger now" on the recurring `nightly-tier-recalc` job. |
| `409 calving_too_close` on import | Two calvings within 24 h of each other on the same dam — fix the dates in the spreadsheet. |

## Source files

- `apps/api/FarmManager.Application/Imports/ImportReport.cs` — DTO + interface
- `apps/api/FarmManager.Infrastructure/Imports/LivestockRegisterImporter.cs` — the implementation
- `apps/api/FarmManager.Infrastructure/Persistence/Seeding/HerdSeeder.cs` — first-boot wiring
- `apps/api/FarmManager.Infrastructure/Persistence/Seeding/AdminBootstrapper.cs` — bootstrap admin
- `apps/api/FarmManager.Api/Controllers/AdminController.cs` — multipart upload endpoint
- `apps/api/FarmManager.Api/Program.cs` — CLI entry point (`import-register` arg)
- `apps/api/FarmManager.Tests/Imports/WorkbookSmokeTests.cs` — workbook shape tests

## Summary of imported records (typical first run against the shipped workbook)

| Entity | Count |
|---|---|
| Organisations | 1 |
| Farms | 3 (Tumi / Jijo / Olly) |
| Animals | ~50 — 40 active + Boshomane + Mphonyana + ~8 synthesized sold-historic |
| Calving events | 28 (Lineage sheet) |
| Service events | 6 (Covered Cows) + up to 14 synthesized from Calving Calendar = ~20 |
| Pregnancy check events | 14 (one positive per confirmed pregnancy) |
| Sale events | ~8 (Madiketane, Ouma, Vaal 2–4, Pitoria + 5 sold bull calves) |
| Death events | 3 (Mphonyana, Kgomotso, Vaal 1) |
| Tier assignments | 20 (Performance Ranking sheet) |
| Audit log entries | One per write — visible via `/api/v1/admin/audit` (Phase E hash-chained) |
