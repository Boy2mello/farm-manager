# Farm Manager

A web application — deeply optimised for mobile use — that replaces spreadsheet-based herd tracking with an automated decision-support system for cattle farming operations.

> **Status:** Phase A (Foundation) — scaffolding the seven pillars per [docs/Tech_Stack.md](docs/Tech_Stack.md).

## What it does

- Captures herd events once (calving, mating, weighing, treatment) from a phone in the kraal.
- Computes everything derivable (calving intervals, performance tiers A–E, inbreeding F, due dates).
- Auto-flags under-performing cows with human-readable reasoning.
- Blocks inbreeding at high-risk matings (the resident bull Boshomane's lineage is seeded).
- Sends WhatsApp + Web Push alerts when action is needed.
- Works offline; syncs when the network returns.

See [docs/Livestock_Management_System_Spec.md](docs/Livestock_Management_System_Spec.md) for the full functional spec (v1.4).

## Tech stack

| Layer | Choice |
|---|---|
| Web | Next.js 15 + React 19 + Tailwind + ShadCN UI (PWA via Serwist) |
| API | ASP.NET Core 9 (Clean Architecture: Domain / Application / Infrastructure / Api) |
| Database | PostgreSQL 16 (EF Core + Dapper) |
| Cache | Redis 7 |
| Messaging | RabbitMQ + MassTransit (introduced when first cross-process consumer arrives) |
| Background jobs | Hangfire |
| Object storage | MinIO (S3-compatible) |
| Reverse proxy / TLS | Caddy 2 (auto-HTTPS via Let's Encrypt) |
| Notifications | Meta WhatsApp Cloud API (direct), Web Push (VAPID), Mailgun/SES |
| Container | Docker + Docker Compose |
| Host | Hetzner Cloud VM (Ubuntu 24.04) |

See [docs/Tech_Stack.md](docs/Tech_Stack.md) for the full stack rationale and per-pillar layout.

## Repository layout

```
Farm-Manager/
├── apps/
│   ├── web/                           # Next.js 15 + Tailwind + ShadCN + Serwist
│   └── api/                           # ASP.NET Core 9 (Clean Architecture)
│       ├── FarmManager.Domain/
│       ├── FarmManager.Application/
│       ├── FarmManager.Infrastructure/
│       ├── FarmManager.Api/
│       ├── FarmManager.Workers/
│       └── FarmManager.Tests/
├── infra/
│   ├── docker-compose.yml             # local dev
│   ├── docker-compose.prod.yml        # Hetzner prod
│   ├── Caddyfile
│   └── grafana/  prometheus/  loki/
├── scripts/
│   ├── deploy.sh                      # SSH deploy to Hetzner
│   ├── backup.sh                      # pg_dump + MinIO sync
│   └── gen-client.sh                  # OpenAPI → TS client
├── docs/
├── .github/workflows/
└── README.md
```

## Local development

Prerequisites: Docker Desktop, .NET 9 SDK, Node.js 20+ with pnpm.

```bash
git clone https://github.com/Boy2mello/farm-manager.git
cd farm-manager
cp .env.example .env

# Bring up the data services (Postgres, Redis, RabbitMQ, MinIO, Mailhog, Seq)
docker compose -f infra/docker-compose.yml up -d

# API
cd apps/api/FarmManager.Api
dotnet run                              # → http://localhost:5000

# Web (in another terminal)
cd apps/web
pnpm install
pnpm dev                                # → http://localhost:3000
```

### Bootstrap admin user

The first time the API connects to an empty database, it auto-creates the bootstrap administrator:

| Field | Default | Override env var |
|---|---|---|
| Username | `Boy2mello` | `BootstrapAdmin__UserName` |
| Password | `Boy2mello!Farm26` | `BootstrapAdmin__Password` |
| Email | `Boy2mello@farm-manager.local` | `BootstrapAdmin__Email` |
| Roles | SuperAdmin + Owner | — |

**Change this password on first login.** It is committed to source control so deployments are reproducible — rotate it before exposing the host to the internet. The password is logged once at startup so you can grab it from the boot logs if needed.

### Importing the real livestock register

`docs/Livestock_Register.xlsx` is the canonical source of herd data. On first boot the API auto-imports it via the `LivestockRegisterImporter` — see [docs/IMPORT.md](docs/IMPORT.md) for the full mapping, idempotency rules, CLI / upload paths, and verification checklist.

Once both are running:
- Web app: <http://localhost:3000>
- API + Swagger: <http://localhost:5000/swagger>
- Postgres: `localhost:5432` (`farmmanager` / `farmmanager`)
- Redis: `localhost:6379`
- RabbitMQ management UI: <http://localhost:15672> (`guest` / `guest`)
- MinIO console: <http://localhost:9001> (`minioadmin` / `minioadmin`)
- Mailhog: <http://localhost:8025>
- Seq (logs): <http://localhost:5341>

## Deployment

Production runs the same Docker Compose stack on a Hetzner Cloud Ubuntu 24.04 VM behind Caddy 2 (auto-HTTPS via Let's Encrypt). See [scripts/deploy.sh](scripts/deploy.sh) and [.github/workflows/deploy.yml](.github/workflows/deploy.yml). The compose file is portable to AWS ECS Fargate, Azure Container Apps, or GCP Cloud Run — see [docs/Tech_Stack.md §7.5](docs/Tech_Stack.md).

## Phased build

Tracked in detail at [docs/Tech_Stack.md §11](docs/Tech_Stack.md).

| Phase | Weeks | Deliverable |
|---|---|---|
| A — Foundation | 1–2 | Deployable login over HTTPS |
| B — Core domain | 3–6 | Animal + lineage + calving/mating + tier engine + inbreeding blocks |
| C — Mobile PWA | 7–10 | Installable, offline-capable, WhatsApp + Web Push |
| D — Analytics | 11–14 | Tier recalc, KPI snapshots, underperformer carousel, morning brief |
| E — Operations | 15–18 | Health, inventory, commerce, audit log, reporting, load test |

## License

Private — all rights reserved.
