# Farm Manager — Recommended Tech Stack

| | |
|---|---|
| **Document** | Farm Manager — Recommended Tech Stack |
| **Version** | 1.1 |
| **Status** | Approved |
| **Date** | 2026-05-25 |
| **Companion to** | Livestock_Management_System_Spec.md |

---

## 0. Strong recommendation (rationale)

**Stack**: **Next.js + ASP.NET Core + PostgreSQL + Docker**, deployed on Ubuntu (Hetzner now, any cloud later).

This is the most balanced stack for this project because:
- The owner already works with **C#, ASP.NET, Docker, and Ubuntu** — no new ecosystem to learn, no productivity tax.
- **PostgreSQL** is excellent for structured livestock records and lineage queries (recursive CTEs, JSONB for flexible attributes, full-text search).
- **Next.js** delivers a modern dashboard and a mobile-friendly UI from a single codebase, served as a PWA.
- **Docker** makes deployment clean on the existing Ubuntu server and trivially portable to any cloud later.
- It can later support API integrations, WhatsApp alerts, AI-driven recommendations, and a thin native wrapper if iOS PWA limitations ever bite.

The result is **a modular livestock-management platform**, not a digital spreadsheet:

| Module | What it is |
|---|---|
| 1. **Web dashboard** | The owner & manager's primary surface — analytics, KPIs, lineage, reports |
| 2. **Mobile-friendly field capture** | Same web app, optimised at phone viewports; offline-capable PWA |
| 3. **API backend** | ASP.NET Core Web API — every feature exposed; mobile, web, integrations all consume it |
| 4. **PostgreSQL database** | Source of truth for animals, events, lineage, KPIs |
| 5. **Automation engine** | Hangfire jobs + MediatR pipeline + (later) NRules — drives tier recalc, KPI snapshots, reminders, auto-flagging |
| 6. **Reporting engine** | PDF / Excel / CSV generators; pre-computed KPI snapshots; ad-hoc query builder |
| 7. **Notification engine** | Multi-channel dispatcher: Web Push, WhatsApp, Email, SMS, in-app |

Each module is independently testable, independently scalable, and replaceable without touching the others.

---

## 1. Constraints

These are non-negotiable inputs:

| Constraint | Decision |
|---|---|
| **Backend framework** | ASP.NET Core Web API |
| **Container runtime** | Docker |
| **Host OS** | Ubuntu (24.04 LTS) |
| **Initial host** | Hetzner Cloud |
| **Cloud portability** | Must run unchanged on AWS / Azure / GCP later |
| **App style** | Responsive web app, optimized for mobile use (no separate native app) |

---

## 2. Stack at a glance

| Layer | Choice | Why |
|---|---|---|
| **Web app** | Next.js 15 (React 19, TypeScript) | SSR for fast first paint on mobile; built-in image optimisation; PWA-ready; large hiring pool |
| **Styling / UI** | Tailwind CSS 4 + ShadCN UI | Utility-first, accessible, mobile-friendly components; trivial to theme |
| **Server-state** | TanStack Query | Caching, optimistic updates, stale-while-revalidate |
| **Forms** | React Hook Form + Zod | Type-safe forms; Zod schemas shared with API contracts |
| **Charts** | Recharts (or Apache ECharts) | Mobile-friendly responsive charts |
| **Lineage diagrams** | **React Flow** | Interactive pedigree trees (zoom/pan/click on phone & desktop); custom node rendering for animal cards; PDF export |
| **PWA layer** | Serwist (next-pwa successor) | Service worker, installable to home screen, offline shell, Web Push |
| **Backend** | ASP.NET Core 9 Web API (C# 13) | Constraint; rock-solid, productive, strongly typed |
| **ORM** | Entity Framework Core 9 + Npgsql | Standard for .NET + Postgres |
| **Hot-path queries** | Dapper | For analytics / lineage queries where EF is too heavy |
| **CQRS / mediator** | MediatR | Clean command/query separation; in-process handlers |
| **Validation** | FluentValidation | Composable; integrates with MediatR |
| **Object mapping** | Mapster | Faster than AutoMapper; source-generated |
| **API docs** | Swashbuckle (OpenAPI 3) | Auto-generated from controllers; client codegen |
| **Database** | PostgreSQL 16 | Free, mature, EF Core support; same DB in dev/prod/cloud |
| **Search** | Postgres FTS (`tsvector`) → Meilisearch later | Built-in; no extra infra at MVP |
| **Cache & sessions** | Redis 7 + StackExchange.Redis | Idempotency keys, pedigree cache, KPI cache |
| **Messaging** | RabbitMQ 3.13 + MassTransit | Outbox pattern; sagas; retries; swap to SNS/SQS later |
| **Background jobs** | **Hangfire** (primary) or **Quartz.NET** (alternative) | Vaccination reminders, nightly KPI snapshots, tier recalc, code-name sequence consolidation. Hangfire has the better dashboard UI; Quartz.NET is leaner if a UI isn't needed. Either works on Postgres. |
| **Rules engine** | **Custom rules in C# (Phase A)** → **NRules** (Phase B+) | Performance tiering, flag catalogue, inbreeding blocks start as plain C# in `Application/Flagging`. When rules become numerous or non-developers should edit them, migrate to **NRules** (forward-chaining production rules in .NET) without changing the API surface. |
| **Auth** | ASP.NET Core Identity + JWT (access + refresh) | No external IDP needed at MVP; MFA via TOTP; WebAuthn for biometric on PWA |
| **Object storage** | MinIO (Docker) → AWS S3 / Cloudflare R2 / Backblaze B2 in cloud | S3-compatible API; swap by changing connection string |
| **Reverse proxy / TLS** | Caddy 2 | Auto-HTTPS via Let's Encrypt; ~10 lines of config |
| **Logging** | Serilog → Seq (or Grafana Loki) | Structured JSON logs; queryable |
| **Metrics** | Prometheus + Grafana | Standard combo; Hetzner-friendly |
| **Tracing** | OpenTelemetry → Tempo (or Jaeger) | Distributed traces across API + jobs |
| **WhatsApp** | Meta WhatsApp Cloud API (direct) or Twilio / 360dialog | Free for first 1,000 conversations/month with Meta |
| **SMS** | Clickatell / SMSPortal | SA-friendly |
| **Email** | Mailgun / Brevo / SendGrid (SMTP) | Transactional + daily digests |
| **Web Push** | Web Push API + VAPID keys | Free, browser-native, works on installed PWA |
| **CI/CD** | GitHub Actions | Build, test, push image to GHCR, deploy |
| **Image registry** | GitHub Container Registry (GHCR) | Free for public; private allowed |
| **Local dev** | Docker Compose | Mirrors prod 1:1 |
| **Prod runtime** | Docker Compose on Hetzner Cloud VM | Or Coolify / Dokploy for managed UI |
| **Testing — unit** | xUnit | Standard .NET |
| **Testing — integration** | Testcontainers .NET | Real Postgres / Redis / RabbitMQ in tests |
| **Testing — E2E** | Playwright (mobile viewports) | Cross-browser; emulates Pixel / iPhone |
| **Load testing** | k6 or NBomber | Either is fine |

---

## 3. Architecture diagram

```
                            ┌──────────────────────────────────────┐
                            │   Browser on phone (primary)         │
                            │   - Next.js 15 PWA (installable)     │
                            │   - Service worker (offline shell)   │
                            │   - Web Push (notifications)         │
                            │   - TanStack Query (cache)           │
                            └──────────────┬───────────────────────┘
                                           │ HTTPS / WSS
┌──────────────────────────────────────────▼─────────────────────────────────────┐
│                              Hetzner Cloud VM (Ubuntu 24.04)                    │
│                                                                                 │
│  ┌──────────────┐    ┌────────────────────┐    ┌──────────────────────────┐   │
│  │  Caddy 2     │◄───┤  Next.js (Node)    │    │  ASP.NET Core 9 API      │   │
│  │  auto-HTTPS  │    │  - SSR/RSC         │    │  - Controllers + MediatR │   │
│  │  reverse pxy │───►│  - PWA assets      │    │  - EF Core + Dapper      │   │
│  └──────┬───────┘    └────────────────────┘    │  - Hangfire jobs         │   │
│         │                                       │  - MassTransit consumers │   │
│         │                                       └──────┬───────────────────┘   │
│         │                                              │                        │
│         │   ┌────────────────────────┬─────────────────┴────────┐              │
│         │   │                        │                          │              │
│         ▼   ▼                        ▼                          ▼              │
│  ┌──────────────┐         ┌──────────────────┐         ┌─────────────────┐    │
│  │ PostgreSQL16 │         │   Redis 7        │         │  RabbitMQ 3.13  │    │
│  │   (volume)   │         │   (volume)       │         │   (volume)      │    │
│  └──────────────┘         └──────────────────┘         └─────────────────┘    │
│                                                                                 │
│  ┌──────────────┐         ┌──────────────────┐         ┌─────────────────┐    │
│  │   MinIO      │         │  Grafana + Loki  │         │   Seq           │    │
│  │   (volume)   │         │  + Prometheus    │         │   (logs UI)     │    │
│  └──────────────┘         └──────────────────┘         └─────────────────┘    │
│                                                                                 │
└────────────────────────────────────────────────────────────────────────────────┘
       │                            │                              │
       ▼                            ▼                              ▼
   Hetzner DNS              Meta WhatsApp                    Mailgun / SES
   (domain)                 Cloud API                        (email)
```

Everything is a Docker container. Caddy terminates TLS and routes:
- `/` → Next.js (port 3000)
- `/api/*` → ASP.NET Core (port 5000)
- `/storage/*` → MinIO (port 9000)
- `/grafana/*` → Grafana (port 3001, basic-auth, restricted IPs)

---

## 4. Repository structure

Single git repository:

```
farm-manager/
├── apps/
│   ├── web/                         # Next.js 15 + Tailwind + ShadCN
│   │   ├── app/                     # App Router
│   │   ├── components/
│   │   ├── lib/api/                 # Generated OpenAPI client
│   │   ├── public/
│   │   └── package.json
│   │
│   └── api/                         # ASP.NET Core 9 Web API
│       ├── FarmManager.Api/         # ASP.NET host
│       ├── FarmManager.Application/ # MediatR handlers, validation
│       ├── FarmManager.Domain/      # Entities, value objects, domain events
│       ├── FarmManager.Infrastructure/ # EF Core, Hangfire, MassTransit
│       ├── FarmManager.Workers/     # Background workers (optional separate process)
│       ├── FarmManager.Tests/
│       └── FarmManager.sln
│
├── infra/
│   ├── docker-compose.yml           # Local dev
│   ├── docker-compose.prod.yml      # Hetzner prod
│   ├── Caddyfile
│   ├── grafana/
│   ├── prometheus/
│   └── loki/
│
├── scripts/
│   ├── deploy.sh                    # SSH deploy to Hetzner
│   ├── backup.sh                    # pg_dump + MinIO sync
│   └── gen-client.sh                # OpenAPI → TS client
│
├── docs/
│   ├── Livestock_Management_System_Spec.md
│   ├── Tech_Stack.md                # this file
│   └── Livestock Register.md        # source data
│
├── .github/workflows/
│   ├── ci.yml                       # build, test, lint
│   └── deploy.yml                   # build images, push to GHCR, deploy
│
└── README.md
```

The **api** folder is a standard .NET solution with Clean Architecture layering. The **web** folder is a standard Next.js project.

---

## 5. ASP.NET Core layout (Clean Architecture)

```
FarmManager.Domain/                  # Pure domain — no dependencies
  Entities/
    Animal.cs
    CalvingEvent.cs
    ServiceEvent.cs
    Flag.cs
    ...
  ValueObjects/
    CodeName.cs                      # Encapsulates "C-2026-003" parsing/formatting
    InbreedingCoefficient.cs
  Events/
    CalvingRecordedEvent.cs
    TierChangedEvent.cs

FarmManager.Application/             # Use cases — depends on Domain only
  Common/
    Behaviours/                      # MediatR pipeline (logging, validation, txn)
    Interfaces/
  Animals/
    Commands/RegisterAnimal/
    Queries/GetAnimalById/
  Calvings/
    Commands/RecordCalving/          # Executes RULE-001 + RULE-019
  Lineage/
    Queries/GetPedigree/
    Queries/InbreedingCoefficient/
  Analytics/
    Queries/HerdKpiSnapshot/
  Flagging/
    Services/TierEvaluator.cs        # Tier matrix, flag catalogue
    Jobs/NightlyTierRecalcJob.cs

FarmManager.Infrastructure/          # Adapters
  Persistence/
    FarmManagerDbContext.cs
    Configurations/                  # EF Core fluent config
    Migrations/
    Repositories/
  Identity/
  Messaging/
    MassTransitConfig.cs
    Consumers/
  Storage/
    MinioFileStore.cs
  Notifications/
    WhatsAppSender.cs
    WebPushSender.cs

FarmManager.Api/                     # Composition root + HTTP
  Controllers/
  Endpoints/                         # Or minimal APIs
  Program.cs
  appsettings.json
```

This is the standard Clean Architecture template ([github.com/jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture)) — well-known, well-documented, easy to onboard new developers.

---

## 5b. Modular engines (the 7 pillars)

These are **logical modules** inside the same Next.js + ASP.NET Core codebase, not separate microservices. Each pillar has a clearly-defined API surface and can be evolved or replaced independently.

### Pillar 1 — Web Dashboard
- Owner & Manager primary surface (also Vet, Bookkeeper)
- Built on Next.js 15 (App Router + React Server Components)
- Tailwind + ShadCN UI components
- TanStack Query for server state
- Recharts for KPI charts; React Flow for lineage
- Same codebase serves mobile and desktop viewports

### Pillar 2 — Mobile-Friendly Field Capture
- Same Next.js app, installed as PWA on the phone home screen
- Serwist service worker (offline shell)
- IndexedDB (via Dexie) for local herd cache
- Background Sync API to flush queued events
- WebAuthn for biometric login
- BarcodeDetector API for ear-tag scanning
- Web Bluetooth for RFID readers (Android)
- Lighthouse mobile score ≥ 90 enforced in CI

### Pillar 3 — API Backend
- ASP.NET Core 9 Web API
- Clean Architecture (Domain / Application / Infrastructure / Api)
- MediatR for CQRS commands & queries
- FluentValidation for input
- Mapster for DTO mapping
- Swashbuckle for OpenAPI 3 spec
- Versioned API (`/api/v1/...`)
- Idempotency keys for non-GET endpoints
- Rate limiting via ASP.NET Core RateLimiter
- Same API consumed by the web app, future native wrappers, and external integrations

### Pillar 4 — PostgreSQL Database
- PostgreSQL 16 (single DB initially; per-context schema separation)
- EF Core 9 + Npgsql for write models
- Dapper for hot-path read queries
- JSONB columns for flexible attributes (e.g. breed composition, flag metrics)
- Recursive CTEs for lineage / ancestor queries
- Postgres FTS for animal name search
- pgcrypto for sensitive-column encryption
- Materialised views for tier ranking and KPI snapshots

### Pillar 5 — Automation Engine
The brain. Implemented as three composable layers:

**Layer 1 — MediatR pipeline (in-process)**
Every command runs through a MediatR pipeline that auto-fires domain events. When a `CalvingEvent` is recorded, downstream handlers (tier recalc, flag re-evaluation, code-name assignment, notification dispatch, reminder scheduling) all execute in a single transactional boundary.

**Layer 2 — Hangfire background jobs**
For work that should not block the user's request:
- Nightly tier recalculation (RULE-007)
- Nightly herd KPI snapshot (RULE-018)
- Reminder dispatch (vaccination due, calving due, preg-check due)
- WhatsApp message delivery
- Photo / voice-note processing
- Daily backup verification

Hangfire stores job state in Postgres; the dashboard at `/hangfire` (admin-only) shows queues, retries, and failures.

**Alternative**: Quartz.NET if a dashboard UI is undesirable.

**Layer 3 — Rules engine**
- **Phase A (MVP)**: rules are plain C# classes in `FarmManager.Application/Flagging/` — easy to test, easy to debug, easy to evolve. The tier matrix, flag catalogue, and inbreeding thresholds live here.
- **Phase B+ (when rules count > ~30 OR non-developers want to edit them)**: migrate to **NRules** ([github.com/NRules/NRules](https://github.com/NRules/NRules)). NRules is a .NET forward-chaining rules engine with a fluent DSL — well-suited to "if cow X meets conditions A, B, C then assign flag F" patterns. The migration is a refactor inside the same module; the external contract doesn't change.

This staged approach avoids paying the upfront cost of a rules engine when the rules are still small and well-understood.

### Pillar 6 — Reporting Engine
- Pre-computed daily reports (Hangfire job): Herd Census, Performance Ranking, Calving Calendar, Cull Candidates
- On-demand reports via API
- PDF generation via **QuestPDF** (.NET-native, fluent API, free for community use) — better than wkhtmltopdf or Puppeteer for .NET shops
- Excel export via **ClosedXML** or **EPPlus**
- CSV via built-in System.IO
- Charts in PDFs rendered via ScottPlot or via headless Chromium → PNG
- All reports localised (English now; Setswana / Afrikaans later)

### Pillar 7 — Notification Engine
Multi-channel dispatcher in a single bounded context:

```
NotificationService
  ├── Web Push (VAPID, browser, free)
  ├── WhatsApp (Meta Cloud API — free up to 1000 conversations/month)
  ├── Email   (Mailgun / Brevo / SendGrid SMTP)
  ├── SMS     (Clickatell / Twilio)
  └── In-App  (websocket fan-out to connected browsers)
```

- One `INotificationChannel` interface; implementations per channel
- User preferences stored per-user (channels enabled, quiet hours, language)
- Template engine (Razor) for message bodies in multiple languages
- Delivery receipts logged for compliance
- Retry with exponential backoff for transient failures
- Critical alerts (inbreeding block, mortality) bypass quiet hours

---

## 6. Mobile optimisation strategy (web-first)

The web app is **the** product. Mobile excellence is achieved through:

| Technique | What it does |
|---|---|
| **Responsive design** (Tailwind breakpoints) | One layout that adapts portrait phone → desktop |
| **Server-side rendering** (Next.js SSR/RSC) | First paint in < 1.5 s even on 3G |
| **PWA install** (Serwist) | Add to home screen; runs in full-screen, looks like a native app |
| **Service worker offline shell** | Last-viewed pages and animal list work without network |
| **Web Push** | Real notifications even when browser closed (Android excellent; iOS 16.4+ supports) |
| **Image optimisation** (next/image) | Auto-resized, WebP/AVIF, lazy-loaded |
| **Touch targets ≥ 44 px** (iOS HIG / Material) | Enforced via Tailwind utility classes |
| **Bottom-tab navigation on mobile breakpoints** | Thumb-reachable primary navigation |
| **`viewport-fit=cover`** | Edge-to-edge layout, respects iPhone notch |
| **Web App Manifest** | App icon, theme colour, standalone display mode |
| **WebAuthn / Passkeys** | Biometric login (Face ID / fingerprint) without passwords |
| **Camera & barcode** (BarcodeDetector API + getUserMedia) | Scan ear tags from a browser |
| **IndexedDB** (via Dexie) | Local cache of herd for offline reads |
| **Background Sync API** | Queue events offline, sync on reconnect |
| **`prefers-reduced-motion`** + accessibility | Inclusive defaults |
| **Wake Lock API** | Keep screen on during long capture sessions |

### What we deliberately do NOT do
- No React Native, no Expo, no native app stores
- No Capacitor wrapper unless iOS push notifications become a blocker (they're improving)
- No separate mobile codebase — one web app, optimised end-to-end

If iOS PWA push ever proves insufficient, the fallback is a thin Capacitor wrapper around the same web app (one shared codebase) — not a full native rewrite.

---

## 7. Hetzner Cloud setup

### 7.1 Initial resources

| Resource | Spec | Monthly cost (approx.) |
|---|---|---|
| Cloud VM | CCX13 (2 dedicated vCPU, 8 GB RAM, 80 GB SSD) | €14.27 |
| Volume | 40 GB SSD (Postgres + MinIO data) | €1.60 |
| Backups | 20% surcharge on VM (daily snapshot) | €2.85 |
| Floating IP | 1 IPv4 | €1.19 |
| Domain (annual amortised) | .com or .co.za | €1 |
| WhatsApp Business (Meta Cloud) | Free tier 1,000 service convos/month | €0 |
| SMS (occasional) | Pay-per-use | €0–5 |
| **Total monthly** | | **~€20–25** |

Hetzner is in Falkenstein (Germany) or Helsinki (Finland) — Helsinki has slightly better latency to Southern Africa.

### 7.2 OS prep (one-time)
1. Provision Ubuntu 24.04 LTS via Hetzner Cloud Console
2. SSH-key auth only; disable password login
3. UFW firewall: allow 22 (SSH from your IP only), 80, 443
4. Install Docker Engine + Compose plugin (`apt install docker.io docker-compose-plugin`)
5. Install Fail2ban
6. Create a non-root deploy user
7. Mount the Hetzner volume at `/data`
8. Point DNS A record to the VM's IPv4

### 7.3 Deployment loop
- Push to `main` → GitHub Actions builds Docker images for `web` and `api`
- Images tagged `ghcr.io/<you>/farm-manager-web:<sha>` and `:<sha>`
- Deploy workflow SSHes to Hetzner, runs `docker compose pull && docker compose up -d`
- Caddy auto-renews TLS
- Optional: install **Coolify** or **Dokploy** for a self-hosted PaaS UI on the same VM — gives you a Vercel-like deploy experience over Docker Compose.

### 7.4 Backups
- Hetzner daily VM snapshots (covered by Backups subscription)
- Cron job: `pg_dump` to MinIO bucket nightly, retain 30 days
- Cron job: MinIO `mc mirror` to off-site (Backblaze B2 free 10 GB, or another Hetzner bucket)

### 7.5 Cloud-ready by design
The same `docker-compose.prod.yml` runs on:
- **K3s** on Hetzner Cloud (managed yourself, ~€20 extra/month for HA)
- **AWS ECS Fargate** with RDS PostgreSQL + ElastiCache + SQS
- **Azure Container Apps** + Azure Database for PostgreSQL + Azure Cache for Redis + Service Bus
- **Google Cloud Run** + Cloud SQL + Memorystore + Pub/Sub

All it takes:
1. Move data services (Postgres, Redis, RabbitMQ, MinIO) to managed equivalents
2. Update connection strings via env vars
3. Point DNS to the cloud LB

No code changes. **12-factor from day one.**

---

## 8. Security baseline

| Concern | Mitigation |
|---|---|
| TLS | Caddy auto-issues from Let's Encrypt |
| Auth | ASP.NET Identity + JWT access (15 min) + refresh (30 days) |
| MFA | TOTP for Owner + Admin (mandatory) |
| Biometric | WebAuthn / Passkeys on PWA |
| API rate limiting | ASP.NET Core RateLimiter middleware |
| Input validation | FluentValidation; Zod on frontend |
| SQL injection | EF Core parameterized; Dapper too |
| XSS | React auto-escapes; CSP headers via Caddy |
| CSRF | SameSite cookies + CSRF tokens for state-changing endpoints |
| Secrets | `appsettings.Production.json` excluded from repo; env vars + `.env.local` |
| Secrets manager (Phase 2) | HashiCorp Vault container, or move to AWS Secrets Manager on cloud migration |
| Audit log | All mutations recorded; hash-chained (Phase 2) |
| POPIA | Personal data minimised + encrypted at rest (Postgres pgcrypto for sensitive columns) |
| Penetration test | Annual once monetised |
| Backups encrypted | `pg_dump | age -e -r <pubkey>` before upload |

---

## 9. Developer experience

### 9.1 Local dev (one command)
```bash
git clone <repo>
cd farm-manager
cp .env.example .env
docker compose -f infra/docker-compose.yml up -d
# - Postgres on :5432
# - Redis on :6379
# - RabbitMQ on :5672 (management :15672)
# - MinIO on :9000 (console :9001)
# - Mailhog on :8025

cd apps/api && dotnet run                # API on :5000
cd apps/web && pnpm dev                  # Web on :3000
```

### 9.2 Useful local tools
- **pgAdmin** or **DBeaver** for DB inspection
- **RabbitMQ Management UI** (built-in)
- **MinIO Console** (built-in)
- **Mailhog** (catches all outgoing email)
- **Seq** locally (optional) for structured-log inspection

### 9.3 Code quality
- **Backend**: dotnet format, EditorConfig, Roslyn analyzers
- **Frontend**: ESLint, Prettier, TypeScript strict mode
- **Pre-commit**: Husky + lint-staged
- **Commit lint**: Conventional Commits

### 9.4 OpenAPI → TypeScript client
- ASP.NET Core publishes OpenAPI 3 spec at `/swagger/v1/swagger.json`
- Frontend generates a typed client via `openapi-typescript` or `orval` on `pnpm gen:api`
- The Zod schemas in `packages/contracts` are derived from the same OpenAPI spec
- Result: changes to a controller endpoint immediately break the frontend build if incompatible — no runtime drift

---

## 10. Cost projection by scale

| Scale | Animals | Users | Stack | Monthly cost |
|---|---|---|---|---|
| **MVP** | 100 | 5 | Single Hetzner CCX13 + Docker Compose | €20–25 |
| **Family** | 500 | 20 | Hetzner CCX23 + managed Postgres backup | €40–60 |
| **Cooperative** | 5,000 | 200 | Hetzner CCX33 + read replica + S3-compatible CDN | €100–150 |
| **Multi-tenant SaaS** | 50,000+ | 5,000+ | K3s on Hetzner OR AWS Fargate + RDS + ElastiCache | €500–2,000 |

The same codebase serves all four tiers.

---

## 11. Phased build plan (revised)

### Phase A — Foundation (week 1–2)
- Repo scaffolded
- API: Clean Architecture solution, EF Core + Postgres migration, Identity + JWT, Swagger
- Web: Next.js + Tailwind + ShadCN, layout shell, login screen
- Docker Compose: API + Web + Postgres + Caddy
- GitHub Actions CI building both apps
- Hetzner VM provisioned; deploy via GitHub Actions
- HTTPS via Caddy
- Owner can log in to a deployed instance

### Phase B — Core domain (week 3–6)
- Animal entity + CRUD endpoints
- Calving / Mating / Pregnancy Check endpoints
- **Code-name auto-generation (RULE-019)**
- Lineage queries (ancestors, descendants, siblings)
- Inbreeding coefficient (Wright's)
- Performance tier engine + flag catalogue
- Web UI for all of the above
- Seed data from the existing herd register

### Phase C — Mobile-optimised & PWA (week 7–10)
- Serwist service worker (offline shell)
- Web App Manifest (installable to home screen)
- VAPID + Web Push for notifications
- IndexedDB cache for animal list (Dexie)
- Background Sync for event queue
- Barcode scanner via BarcodeDetector API
- Photo upload to MinIO
- Mobile dashboard layouts (KPI strip, quick-capture row)

### Phase D — Analytics & flagging (week 11–14)
- Hangfire nightly job: KPI snapshot
- Hangfire nightly job: Tier recalc + flag re-evaluation
- Cow profile (7 tabs)
- Bull profile (6 tabs)
- Herd Analytics dashboard
- Underperformer carousel
- WhatsApp + Web Push notifications

### Phase E — Operations & health (week 15–18)
- Vaccination / treatment / deworming / dipping
- Inventory module
- Sales / purchases / deaths / transfers
- Audit log with hash chain
- E2E tests (Playwright)
- Load test + tune
- Backup verification
- Beta with real herd (Tumi's 40 head)

---

## 12. Risks and trade-offs

| Risk | Impact | Mitigation |
|---|---|---|
| iOS PWA push notifications still maturing | Owner on iPhone might not get critical alerts reliably | Use SMS / WhatsApp as fallback; Capacitor wrapper as last resort |
| MassTransit + RabbitMQ adds infra for low MVP volume | Operational complexity | Start with in-process MediatR notifications; introduce MassTransit only when first cross-process event handler arrives (Phase B) |
| Self-hosting Postgres means owning backups & DR | If we lose the VM, we lose data | Hetzner snapshots + pg_dump to S3-compatible off-site nightly + tested restore quarterly |
| Single VM = single point of failure | Outage takes app offline | Acceptable at MVP; introduce K3s + 2-node setup at €40/month when revenue justifies |
| EF Core slow on complex pedigree queries | Slow lineage pages at scale | Drop to Dapper for those specific queries; cache in Redis |
| ASP.NET Core hosting on Linux still less common than Windows | Smaller community for specific Linux-deployment issues | Stick to mainstream Docker patterns; .NET on Linux is fully production-supported since .NET Core 1.0 |

---

## 13. What to start with this week

If you give a developer this stack on Monday:

| Day | Outcome |
|---|---|
| Mon | Hetzner VM provisioned, Docker installed, repo scaffolded |
| Tue | API skeleton (login + first endpoint) + EF Core migration deployed |
| Wed | Next.js shell + Tailwind/ShadCN + login screen deployed |
| Thu | Animal entity end-to-end (register an animal from the phone) |
| Fri | Caddy auto-HTTPS, GitHub Actions deploying on push to main |

End of week 1: there is a real URL, on HTTPS, that you can log into from your phone and add a cow.

---

**End of tech stack recommendation.**
