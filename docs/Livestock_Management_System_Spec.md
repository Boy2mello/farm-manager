# Farm Manager — Livestock Management System
## Functional Specification

| | |
|---|---|
| **Document** | Farm Manager / Livestock Management System — Functional Specification |
| **Version** | 1.4 |
| **Status** | Draft for Review |
| **Author** | Tumi Tsaagane |
| **Date** | 2026-05-25 |
| **Audience** | Solution architects, product owners, development team, farm operations |
| **Basis** | Livestock Register Excel workbook (40 head, Boran/Brahman/cross herd) |

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Design Principles](#2-design-principles)
3. [Glossary & Domain Concepts](#3-glossary--domain-concepts)
4. [Business Context & Objectives](#4-business-context--objectives)
5. [Personas & Stakeholders](#5-personas--stakeholders)
6. [Solution Overview](#6-solution-overview)
7. [Mobile-First Experience](#7-mobile-first-experience)
8. [Functional Modules](#8-functional-modules)
9. [Performance Analytics — Herd & Individual](#9-performance-analytics--herd--individual)
10. [Auto-Flagging Engine for Under-Performers](#10-auto-flagging-engine-for-under-performers)
11. [Automation & Business Rules](#11-automation--business-rules)
12. [Lineage & Inbreeding Engine](#12-lineage--inbreeding-engine)
13. [Workflows](#13-workflows)
14. [Dashboards](#14-dashboards)
15. [Reporting](#15-reporting)
16. [Notification & Reminder Engine](#16-notification--reminder-engine)
17. [User Roles & Permissions](#17-user-roles--permissions)
18. [Database Entities](#18-database-entities)
19. [Audit & Historical Tracking](#19-audit--historical-tracking)
20. [Integrations](#20-integrations)
21. [Non-Functional Requirements](#21-non-functional-requirements)
22. [Suggested Architecture](#22-suggested-architecture)
23. [Scalability](#23-scalability)
24. [Implementation Roadmap](#24-implementation-roadmap)
25. [Appendices](#25-appendices)

---

## 1. Executive Summary

### 1.1 Purpose
Farm Manager is a **web application — heavily optimised for mobile use** — that provides intelligent livestock management for cattle farming operations. It replaces spreadsheet-based herd tracking with an automated decision-support system the owner can use just as comfortably from a phone in the kraal as from a desk.

The platform is derived from a real-world herd register (40 head across Boran Cross, Brahman, Brahman Cross, Mix Breed; three managed herds: Jijo, Olly, Tumi) and incorporates lessons learned from manual record-keeping: data inconsistencies, ambiguous pregnancy status, missed lineage links, lack of proactive alerts, and no early warning when a cow's performance is declining.

### 1.2 Vision
Move from a static register to an **active, web-based herd-management system, deeply optimised for mobile use**, that:

1. **Captures once** — every event is entered one time, from whichever device is closest, including a phone in the kraal.
2. **Computes everything** — calving intervals, due dates, performance tiers, vaccine schedules, growth rates, and inbreeding risk are all derived, never typed.
3. **Decides where it can** — automatically flags under-performing cows, recommends matings, schedules treatments, fires alerts.
4. **Works in the field** — installable as a PWA on the phone home screen; offline shell + background-sync of captured events when network returns; everything that can be done at a desktop can be done on a phone.
5. **Scales** — from a single 40-head herd to multi-farm enterprises, with the same codebase.

### 1.3 Core capabilities
- Animal registration & profiling **with auto-generated code-name at birth**
- Lineage tracking with inbreeding prevention
- Breeding & reproduction management with three-state pregnancy
- Calving registration with automatic downstream updates
- Vaccination, deworming, dipping schedules with auto-reminders
- **Per-cow & per-bull performance profiles**
- **Herd-wide performance analytics with trend tracking**
- **Automatic flagging of poor performers** with reasoning
- Health and treatment history
- Weight and growth tracking
- Feeding and supplement management
- Sales, purchases, deaths, transfers
- Financial and operational reporting
- Mobile field capture with offline sync
- Audit-grade historical record

### 1.4 Differentiators
- **One web app, mobile-optimised**: a single codebase served as a Progressive Web App (PWA). Installable on the phone home screen, behaves like a native app, also works fully on a desktop browser. No separate iOS/Android app to maintain.
- **Auto code-name at birth**: every calf is issued a unique, year-stamped code-name (e.g. `C-2026-003`) the moment the calving is recorded — printable for an ear tag before the calf even has a given name.
- **Three-state pregnancy** (Open, Confirmed, Unconfirmed) reflecting how farmers actually work
- **Performance tiers (A → E)** automatically assigned and re-evaluated, with audit trail of every tier change
- **Underperformer auto-flagging** with explanatory reasoning and escalation rules
- **Inbreeding check is blocking** for high-risk matings, with the farm's specific bull lineage seeded into the engine
- **(B)-sired flag** as first-class data so descendants of the resident bull have built-in mating constraints
- **Aliased animals** (Mmadikrempe/Tiki, Springbok/Georgina) resolved transparently across all searches

---

## 2. Design Principles

These are the non-negotiable principles that govern every design decision. When choices arise, they are arbitrated in this order.

### Principle 1 — One web app, deeply optimised for mobile
The product is **a web application**, accessed by everyone through a browser, and installable as a PWA on the phone. Even though it is web-first technically, every design decision is made knowing the owner will most often be holding a phone. Therefore:
- Every screen is **responsive**, designed mobile-first within a desktop-capable layout.
- Touch targets ≥ 44 px; primary actions reachable with one thumb.
- Service-worker offline shell: last-viewed pages and animal list work without a signal; new events queue and sync on reconnect.
- Bandwidth-frugal: text-first; images lazy-loaded and WebP/AVIF-optimised; photos defer to Wi-Fi upload.
- No multi-step wizards on critical-path screens; capture is one screen wherever possible.
- The same URL serves the owner's phone, the manager's tablet, and the bookkeeper's laptop — each gets a layout tuned to its viewport.

### Principle 2 — Automate or compute, never re-enter
If a value can be derived from existing data (age, calving interval, due date, tier, inbreeding coefficient), it is **never** captured manually. Manual entry is reserved for observations.

### Principle 3 — Events, not edits
The source of truth is an append-only stream of events (calving, mating, treatment, weighing, sale, death). All views are projections. Corrections happen via corrective events, not by mutating history.

### Principle 4 — Make the right thing easy
The system should make the **best** action the **default** action.
- The default mating partner is the highest-genetic-distance compatible bull.
- The default vaccination schedule is the breed-standard schedule.
- The default re-breeding window is calving-date + 60 days.

### Principle 5 — Tell the user *why*
Every flag, every recommendation, every block carries a human-readable reason. "Cull candidate" alone is unhelpful; "1 calf in 6 years, last calf at age 4.7 — calving interval > 18 months" is actionable.

### Principle 6 — Multi-tenant safe
Data is partitioned by Organisation from day one, even if the MVP serves only one farm. This avoids costly retrofits.

### Principle 7 — POPIA & POPI-aware
Personal data is minimised, encrypted at rest, retained per legal schedule, and exposes Data-Subject rights (export, delete, correct) through the same UI as everyone else.

---

## 3. Glossary & Domain Concepts

| Term | Definition |
|---|---|
| **Animal** | Any individual head of livestock tracked by the system. |
| **Bull** | Intact (non-castrated) male used or intended for breeding. |
| **Steer** | Castrated male. |
| **Cow** | Adult female that has calved at least once. |
| **Heifer** | Young female that has not yet calved. |
| **Heifer calf** | Female under ~24 months, not yet ready for breeding. |
| **Bull calf** | Young male, typically pre-puberty. |
| **Weaner** | Calf separated from the dam, typically 7–9 months old. |
| **Yearling** | Animal aged 12–24 months. |
| **Dam** | The mother of a specific animal. |
| **Sire** | The father of a specific animal. |
| **Calving** | Event of a cow giving birth. |
| **Calving interval** | Days/months between two consecutive calvings of the same cow. Target: 12–14 months. |
| **Calving rate** | Calves born / breeding cows exposed, per year. |
| **Precocity** | Age at first calving. Target: ≤ 36 months. |
| **Productive years** | Years a cow has been of breeding age (age − 2). |
| **Calves per productive year (CPY)** | Total calves ÷ productive years. Cull threshold: < 0.5. |
| **Open** | Cow that is neither pregnant nor recently calved, available to be bred. |
| **Covered / Serviced** | Cow that has been mated. |
| **Confirmed pregnant** | Pregnancy verified by palpation, ultrasound, or visual sign. |
| **Unconfirmed** | Cow with an expected due date based on observed service, but not yet preg-tested. |
| **Exposed** | Cow that has been in the bull camp but pregnancy status unknown. |
| **Dry** | Lactating cow weaned off her calf; resting before next calving. |
| **Lactating** | Cow currently nursing a calf. |
| **Gestation** | Pregnancy period. Standard cow gestation = **283 days**. |
| **Inbreeding coefficient (F)** | Wright's coefficient — probability that two alleles at a locus are identical by descent. |
| **(B) sired** | Calf sired by the farm's resident bull, Boshomane. |
| **BCS** | Body Condition Score — 1 (emaciated) to 9 (obese); 5–6 is target for breeding cows. |
| **ADG** | Average Daily Gain — weight gained per day. |
| **Performance tier** | A–E grade applied to breeding cows. A = top, E = cull candidate. |
| **Underperformer flag** | A system-assigned, persistent flag indicating a cow has met one or more poor-performance criteria. |
| **Watch list** | A cohort of animals flagged but not yet recommended for culling. |
| **Lot / Group / Camp** | A pasture or paddock grouping. |
| **PoP / FoP** | Probability of Pregnancy / Forecast of Pregnancy (Phase 3 ML feature). |
| **Code-name** | The auto-generated, year-stamped identifier issued to every calf at birth — e.g. `C-2026-003`. Immutable, globally unique within the organisation, separate from (and used alongside) any given name and ear-tag number. |

---

## 4. Business Context & Objectives

### 4.1 Current state
The farm is managed via:
- A Markdown register listing animals by breed group with births, mothers, and sale events
- An Excel workbook with derived sheets (Master Register, Performance Ranking, Heifer Readiness, Bull Calf Plan, Calving Calendar)
- Notebook entries for vaccinations, dipping, weighings

### 4.2 Pain points
1. Pregnancy status is ambiguous (cows on both "Open" and "Expected Delivery" lists)
2. Mother–calf relationships are narrative and contradict in places
3. Performance trends are invisible — no automatic flagging of declining cows
4. No proactive reminders for vaccinations, dipping, pregnancy checks, weaning
5. No inbreeding safeguards
6. Cannot quickly answer "How is each cow performing?" or "How is the herd trending year-over-year?"
7. Loss events buried in history notes
8. No field capture — records wait until the owner is at a computer
9. No audit trail
10. Sharing data with the family is via screenshots

### 4.3 Strategic objectives
| # | Objective | Target metric |
|---|---|---|
| O1 | Eliminate data inconsistencies | 0 unreconciled mother–calf links |
| O2 | Improve calving rate | +10% within 18 months |
| O3 | Reduce calving interval | Average ≤ 14 months |
| O4 | Zero overdue mandatory vaccinations | 100% on schedule within 6 months |
| O5 | Zero inbreeding events | 100% blocked at high risk |
| O6 | Reduce owner data-entry time | 50% reduction vs Excel baseline |
| O7 | Field capture coverage | ≥ 80% of events captured day-of |
| O8 | Decision turnaround | Cull candidates flagged ≤ 30 days from underperformance |
| O9 | Mobile usage | ≥ 90% of events captured on mobile |

---

## 5. Personas & Stakeholders

### 5.1 Farm Owner ("Tumi")
- Owns the operation, sets strategy, makes buy/sell/cull decisions
- Mobile usage: **primary** — reviews dashboards from the phone daily
- Desktop usage: occasional, deeper reports

### 5.2 Farm Manager / Co-owner ("Jijo", "Olly")
- Day-to-day operations of a herd they manage
- Mobile usage: **primary**

### 5.3 Field Worker / Herd Boy
- Records what's observed in the kraal: mountings, calvings, sick animals, weights
- Mobile usage: **exclusive** — never uses desktop
- May have low literacy, intermittent connectivity, work gloves on

### 5.4 Veterinarian
- Performs vaccinations, treatments, pregnancy checks
- Mobile usage: high (visits multiple farms)

### 5.5 Bookkeeper / Accountant
- Sales, purchases, expenses
- Mobile usage: low; mostly desktop

### 5.6 System Administrator
- Manages users, farms, schedules
- Desktop usage: primary

### 5.7 External Observer (read-only)
- Family member, lending bank
- Mobile usage: primary (dashboards only)

---

## 6. Solution Overview

### 6.1 Logical architecture (10,000-ft view)

```
        ┌─────────────────────────────────────────────────────────┐
        │            CLIENT — ONE WEB APP (PWA)                    │
        │                                                          │
        │  Next.js 15 + React 19 + Tailwind + ShadCN               │
        │  - Service worker (offline shell, background sync)       │
        │  - IndexedDB (local herd cache)                          │
        │  - Web Push (notifications even when app closed)         │
        │  - Installable on phone home screen                      │
        │  - Same URL → phone, tablet, desktop                     │
        └─────────────────┬───────────────────────────────────────┘
                          │ HTTPS (REST + WebSocket)
        ┌─────────────────▼───────────────────────────────────────┐
        │           Caddy 2 — auto-HTTPS reverse proxy             │
        └─────────────────┬───────────────────────────────────────┘
                          │
        ┌─────────────────▼───────────────────────────────────────┐
        │             ASP.NET CORE 9 WEB API                       │
        │   Clean Architecture: Domain · Application · Infra · Api │
        │   MediatR · FluentValidation · EF Core · Hangfire        │
        │   Auth: ASP.NET Identity + JWT · MFA · WebAuthn          │
        └─┬─────────────────┬─────────────────┬───────────────────┘
          │                 │                 │
       PostgreSQL 16    RabbitMQ + MassTransit   Redis 7
       (EF Core)        (outbox events)          (cache, idempotency)
                                                       │
                          ┌────────────────────────────┼──────────────┐
                          ▼                            ▼              ▼
                      MinIO                  WhatsApp Cloud API   Mailgun
                  (S3-compatible)            (notifications)      (email)
```

All containers run under Docker Compose on a single Hetzner Cloud Ubuntu 24.04 VM at MVP scale. See `Tech_Stack.md` for the exact container layout and the cloud-migration matrix.

### 6.2 Bounded contexts
At MVP these are **logical modules within a single ASP.NET Core API**, not separate microservices. Splitting into independent services is a Phase 3+ option only if scale demands it.

| Context | Responsibility |
|---|---|
| **Animal** | Identity, profile, lifecycle state, location |
| **Lineage** | Sire/dam relationships, pedigree queries, inbreeding |
| **Breeding** | Heat, service, pregnancy, calving |
| **Health** | Vaccinations, treatments, schedules, parasites |
| **Growth** | Weighings, ADG, body condition |
| **Inventory** | Feed, supplements, medicines, semen straws |
| **Commerce** | Sales, purchases, transfers, deaths |
| **Analytics** | Performance metrics, tiers, KPIs, trends |
| **Reporting** | Aggregated views, PDFs, exports |
| **Notification** | Web Push, WhatsApp, SMS, email |
| **Audit** | Immutable history, "as-of" queries |

---

## 7. Mobile-Optimised Web Experience

This section is normative: the platform is **a single responsive web application**, designed so that every workflow is excellent on a phone. There is no separate native mobile app. The web app is delivered as a **Progressive Web App (PWA)** so it can be installed on the phone's home screen, runs in a full-screen "app" frame, and receives push notifications.

> **Hard commitment.** Every end-user feature must be fully usable on a phone-sized viewport. If a feature cannot be done well on a phone, that is a defect to be fixed, not a reason to require desktop. Owner, Manager, Vet, and Field Worker workflows are validated on a mobile viewport in CI (Playwright with `iPhone 13` and `Pixel 7` device emulation). Bookkeeper and System Administrator are the only roles permitted to have desktop-preferred screens (large pivot tables, bulk admin).

### 7.1 Why mobile-first
- The owner's working environment is the farm, not an office.
- Field workers have no other option — they only have phones.
- A vet on a multi-farm round needs the whole patient history on the device, even when there is no cellular signal.
- South African rural connectivity (3G/4G/LoRa) is intermittent; the app must continue to function offline.

### 7.2 Mobile UX patterns

**Home screen**
- 4 large quick-capture buttons at the top: **Calving · Mating · Treatment · Weighing**
- Below: "Today" card listing tasks due (preg checks, vaccinations, deliveries)
- Bottom tab bar: Home · Herd · Analytics · Alerts · Profile

**Animal lookup**
- Persistent search bar reachable with thumb
- Tag-scan button (camera + Bluetooth RFID)
- Voice-to-text search ("Find Kgetlheng" / "Phatla ya Kgetlheng" / Setswana support)
- Recently-viewed list

**Single-screen capture**
- Each event type has a one-screen form
- Smart defaults pre-fill ≥ 60% of fields (date, location, last bull, breed-standard dosage)
- Inline validation; no popovers
- Photo and voice-note attachable on every event

**Glove mode**
- User setting toggling: huge buttons, no fine gestures, reduced visual noise
- Long-press to confirm (instead of small "tick" buttons)

**Quiet-rural mode**
- Auto-detect low-bandwidth (< 100 kbps)
- Switch to text-only views, defer images
- Background sync queues events without UI feedback latency

**Voice notes everywhere**
- Any event accepts a voice note (auto-transcribed when network returns)
- Useful when typing on a phone in poor light or with gloves

**Code-name on every card**
- Every animal card on mobile shows the code-name (e.g. `T-2026-003`) as a monospaced badge in the corner of the photo
- The given name (Bali, Kgetlheng, etc.) is the primary label; the code-name is the secondary, machine-friendly identifier
- Long-press the code-name to copy; useful for paperwork, brokerage notes, WhatsApp messages

**One-handed operation**
- Every primary action sits in the bottom half of the screen for thumb reach
- Critical destructive actions (delete, void) require long-press + confirm to prevent accidental fat-finger taps
- All forms scroll above the keyboard; nothing is hidden behind it

### 7.3 Offline architecture (PWA)
- **Service worker** (via Serwist) caches the app shell, last-viewed pages, and recent animal records
- **IndexedDB** (via Dexie) holds the user's herd snapshot for fast offline reads — typically up to 5,000 animals manageable in-browser
- **Write-ahead queue** in IndexedDB persists events created offline; flushed via the **Background Sync API** on reconnect
- **Optimistic UI**: events take effect locally immediately; conflicts surface in a "needs attention" queue if the server rejects on sync
- **Conflict resolution policy**:
  - Two calvings logged for the same dam within 24 hours → manager intervention required
  - Same animal weight logged twice the same day → keep latest by timestamp
  - Same animal sold by two users → block; first one wins
- **Smart pre-fetch**: when on Wi-Fi, pre-cache the next 30 days of expected events, scheduled tasks, and key animal photos

### 7.4 Mobile-specific dashboards

**Owner mobile dashboard** (one scroll):
1. Big number: confirmed pregnancies / expected this month
2. Alert chips: red badges for overdue items
3. Tier distribution donut
4. Recent calving feed (last 5)
5. "Tap to capture" CTA

**Field worker mobile dashboard**:
1. Today's task list (vaccination round, weighing batch)
2. "Quick record" buttons (huge)
3. My pending uploads (offline queue)

### 7.5 Hardware integrations on mobile (via browser APIs)
- **Camera** via `getUserMedia()` and **BarcodeDetector API** for ear-tag scanning
- **GPS** via `Geolocation API` for location tagging (which camp / paddock)
- **Biometric auth** via **WebAuthn / Passkeys** — Face ID, fingerprint, Windows Hello — no typed passwords in the field
- **Voice recording** via `MediaRecorder API`
- **Bluetooth RFID readers** (Allflex, Datamars) via **Web Bluetooth API** (Chrome/Edge on Android; limited on iOS — fallback to QR/barcode there)
- **Push notifications** via the **Web Push API** with VAPID keys; works on Android excellently and on iOS 16.4+ for installed PWAs

### 7.6 Push notifications
- Real-time alerts even when app is closed
- Critical alerts (calving started, mortality, inbreeding block) bypass quiet hours
- Notification channels grouped by type (Health, Breeding, Sales, System)

### 7.7 Performance budget on mobile (PWA)
| Metric | Budget |
|---|---|
| First Contentful Paint on 3G | ≤ 1.5 s |
| Largest Contentful Paint on 3G | ≤ 2.5 s |
| Time to Interactive | ≤ 3 s on mid-range Android |
| Screen render (after install) | ≤ 300 ms |
| Event capture round-trip (offline) | ≤ 200 ms (local IndexedDB only) |
| Sync of 100 queued events | ≤ 10 s on 3G |
| Service worker cache footprint | ≤ 50 MB |
| Total JS payload (initial) | ≤ 200 KB gzipped |
| Lighthouse mobile score | ≥ 90 across Performance, Accessibility, Best Practices, PWA |

### 7.8 Desktop layout (same app, wider viewport)
The same Next.js codebase serves desktop browsers. At wider viewports the layout exposes:
- Side-by-side panels (e.g. herd list + animal detail)
- Larger pivot tables and multi-axis charts
- Multi-animal bulk operations
- Admin screens (users, schedules, breed templates) — these are intentionally desktop-preferred
- Long-form reports and PDF generation

There is **no separate desktop app and no separate mobile app**. The same URL serves both, with responsive breakpoints controlling layout. Owners may prefer desktop for month-end review, but no field operation requires it.

---

## 8. Functional Modules

### 8.1 Animal Registration & Profiling

**Capabilities**
- Register manually or via bulk import (CSV/Excel)
- Capture: name, common name, aliases, tag, RFID, photos, DOB (date or year-only), breed, breed composition, sex, source (born / purchased / inherited), purchase price, current location, status, markings, horn status, notes
- Link dam and sire (sire may be External or Unknown)
- Group/lot assignment
- **Auto-generated code-name at birth** — see Section 8.1.1
- Print ear-tag labels (PDF/Zebra) — code-name pre-printed
- View animal timeline

#### 8.1.1 Auto-generated calf code-name

Every calf receives an immutable, system-assigned code-name the instant a calving event is recorded — before any human types a given name.

**Default format**: `<PREFIX>-<YYYY>-<NNN>`

| Token | Meaning | Default |
|---|---|---|
| `PREFIX` | Organisation-configurable letter(s); identifies the operation | `C` (Calf) — configurable to e.g. `T` for Tumi, `J` for Jijo, `O` for Olly |
| `YYYY` | Calf's year of birth (4 digits) | from calving date |
| `NNN` | Sequence within that year, zero-padded | starts at 001, increments per calving in the year |

**Examples**:
- First calf born in 2026 on Tumi's farm: `T-2026-001`
- 23rd calf born in 2026 on the same farm: `T-2026-023`
- 7th calf born in 2027: `T-2027-007`

**Rules**:
- The code-name is **assigned atomically** at calving registration (RULE-019). Race conditions on simultaneous calvings cannot produce duplicates.
- The code-name is **immutable** once assigned. If the calf is later renamed, the given name changes but the code-name does not. This preserves an audit-grade identifier.
- The code-name is **scoped to the organisation**. If Jijo and Tumi run separate organisations on the same platform, each has its own sequence.
- Sequence resets to `001` on 1 January each year (organisation timezone, default SAST).
- The width of `NNN` widens automatically if a year exceeds 999 calves (zero-pad becomes `NNNN`).
- The format is **organisation-configurable**: alternative templates such as `<FARM>-<YY>-<NNN>` (`TUMI-26-001`), `<BREED>-<YYYY>-<NNN>` (`BRX-2026-001`), or even `<YEAR>-<NNN>` are supported via a template string in organisation settings.
- The code-name is the **default display label** in every list, card, search result, and notification — so any animal can be referred to before it has a name, and field workers can record events against a calf the moment its ear tag is fitted.
- The code-name is **searchable** along with given names and aliases (see Section 8.1 — aliased animals resolution).

**On the mobile UI**:
- Code-name appears as a small, monospaced badge on every animal card, top-right corner of the photo
- Tapping the code-name copies it to clipboard (for paste into messages / paperwork)
- The "Quick record → Calving" flow shows the just-generated code-name in a confirmation toast: "Calf `T-2026-024` registered. Print tag?"

**For non-born-on-farm animals**:
Purchased and inherited animals also receive a code-name, with prefix `P` (Purchased) and the year of acquisition: e.g. `P-2026-005`. Adult animals already on the farm at the time the system goes live get a one-off code-name with prefix `L` (Legacy) and the migration year: e.g. `L-2026-001` … `L-2026-040`.



**Edge cases**
- Year-only DOB: store as `2020-01-01` with `dob_precision = year`
- Aliased animals: search resolves across all aliases
- Purchased adults with unknown DOB: estimated age in years; vet confirmation requested
- Source = "exchanged for X": cross-link

### 8.2 Lineage Management
*See Section 12 for the full engine.*

### 8.3 Breeding & Reproduction

**Capabilities**
- Heat detection log
- Mating record (cow, bull or AI straw, date, observer)
- Pregnancy check log (palpation / ultrasound / blood)
- Pregnancy lifecycle: Open → Exposed → Pregnant Confirmed → Late-stage → Calving → Lactating → Dry → Open
- Auto-computed expected due date (service + 283 days)
- Calendar of upcoming calvings and check dates
- Bull workload tracking
- Synchronised breeding protocol templates (CIDR, GnRH, PGF2α)
- Auto-computed re-breeding window (calving + 45 d open; calving + 90 d target)

### 8.4 Calving Management
- Event entry (mobile-friendly, photo-capable, voice-note-capable)
- Captures: dam, date/time, difficulty (1–5), assistance, calf details, placenta delivered, mothering ability
- Auto-creates calf record
- Auto-links dam & sire
- Multiple-birth supported
- Stillbirth handled (calf record with Dead status so stats include all births)

### 8.5 Vaccination & Treatment
- Templates by life stage (Brucella S19, blackleg, anthrax, lumpy skin)
- Treatment records with drug, dose, route, withdrawal period, vet, follow-up
- Deworming schedule with rotation
- **Dipping schedule** seasonal (summer weekly, winter fortnightly) for tick-borne disease management
- Withdrawal period enforcement (blocks sale screen)
- Batch operations
- Inventory linked

### 8.6 Weight & Growth
- Weighing record (date, weight, method, BCS)
- Computed: birth/weaning/yearling/18-month/mature weights
- ADG between weighings and over standard intervals
- Growth curve vs breed standard
- Alerts on under-target / over-target

### 8.7 Feeding & Supplements
- Feed types catalogue (Molatek Production Lick, Voermol MultiMin 16, grazing, silage, concentrate)
- Plans per animal category
- Daily intake or bulk delivery records
- Cost per kg, monthly cost per animal/category
- Stock with reorder points

### 8.8 Sales, Purchases, Deaths, Transfers
- Sale: animal, date, buyer, weight, price (R/kg or lump), commission, transport, paperwork
- Purchase: new animal record, price, seller
- Death/Mortality: cause, suspected disease, PM findings, insurance
- Transfer: inter-farm with confirmation
- Slaughter for own use: tracked as sale to "Self"

### 8.9 Inventory Module (Feed, Medicine, Equipment)
- Items with categories
- Stock movements (purchase in / usage out / transfer / write-off)
- Expiry-date alerts at 30/14/7 days
- Cold-chain notes
- Reorder alerts

### 8.10 Performance Analytics & Auto-Flagging
*See Sections 9 and 10 — given their prominence in this specification, they are dedicated chapters.*

### 8.11 Financial & Operational Reporting
- Income statement (sales − purchases − feed − vet − labour − other)
- Asset register (live weight × R/kg)
- Cost per calf weaned
- Sale price benchmarking
- Capital flow between farms
- Tax-ready summary

---

## 9. Performance Analytics — Herd & Individual

This is one of the platform's two flagship modules (the other being auto-flagging in Section 10). It exists because the user cannot today see, at a glance:
- How any one cow is performing
- How any one bull's offspring are performing
- How the herd is trending year-over-year
- Which animals are dragging the herd average down

### 9.1 Three views of performance

Performance data is exposed in three distinct views, each tailored to its audience.

| View | Audience | Surface | Primary question answered |
|---|---|---|---|
| **Herd Analytics** | Owner | Mobile + Web | "How is my whole operation doing this year vs last?" |
| **Animal Profile (Cow)** | Owner, Manager | Mobile | "How is *this cow* performing?" |
| **Animal Profile (Bull)** | Owner, Manager | Mobile | "How are *this bull's offspring* performing?" |

### 9.2 Herd Analytics dashboard

A single screen accessible from the bottom tab bar "Analytics" — designed to be readable on a phone in landscape **or** portrait.

**Top — KPI strip (always visible)**
- Live cattle (count)
- Confirmed pregnancies (count) with delta vs last year
- Calves YTD (count) with delta vs last year
- Calving rate this year (%)
- Mortality rate YTD (%)
- Average calving interval (months)

Each KPI has a sparkline showing the last 12 months and a colour cue (green = improving, red = declining, grey = stable).

**Middle — Trends**

Charts (swipeable carousel on mobile, side-by-side on web):

1. **Calvings per month** (bar) — 24-month window, F/M split
2. **Calving interval trend** (line) — herd average over the last 3 years
3. **Cow tier distribution** (donut) — A/B/C/D/E counts, with the percentage in tier E or D as a "watch %" headline
4. **Calving rate by year** (bar) — last 5 years
5. **Mortality by year** (bar with cause breakdown)
6. **Weaning weight average** (line) — over time, by breed
7. **Bull progeny comparison** (horizontal bars) — average daughter calving interval per active bull

**Bottom — Underperformer carousel**
- Cards for each animal currently flagged
- One-tap to the animal profile
- Filterable by reason: low CPY, long interval, no calf at age, repeat breeder, calf mortality

**Filters**
- Time window (YTD, last 12 months, last 3 years, custom)
- Farm (Jijo / Olly / Tumi / All)
- Breed
- Camp / Lot

### 9.3 Herd-level metrics (computed nightly + on demand)

| Metric | Formula | Why it matters |
|---|---|---|
| **Calving rate** | calves born in period / breeding cows exposed in period | Headline reproductive efficiency |
| **Calving interval (mean)** | mean of (date_n − date_n−1) per cow across all cows | Lower = faster turnover = more revenue |
| **Calving interval (median)** | median of same | More robust to outliers |
| **Conception rate** | confirmed pregnancies / matings recorded | Bull or AI effectiveness |
| **First-service conception rate** | confirmed at first preg-check / first services | High-precision fertility KPI |
| **Repeat-breeder rate** | cows requiring > 2 services / cows serviced | Watch indicator |
| **Mortality rate (overall)** | deaths / starting headcount | Year-over-year |
| **Calf mortality rate (0–30 d)** | calf deaths in first 30 d / live births | Maternal & environmental health |
| **Weaning rate** | calves weaned / calves born | Survival to weaning |
| **Weaning weight (mean)** | average weight of weaned calves by sex × breed | Growth performance |
| **Average daily gain — birth to weaning** | (weaning_wt − birth_wt) / days | Growth performance |
| **Calves / cow / year** | total calves / cow-years in herd | Overall reproductive output |
| **Cost per kg weaned** | total cost / kg of weaners produced | Economic efficiency |
| **Active cows below tier C** | count | Cull pipeline size |
| **Asset value** | sum of (animal × estimated live weight × R/kg) | Balance sheet view |

All metrics support: comparison to previous period; comparison to a configurable target; export to PDF/Excel.

### 9.4 Cow Profile

A dedicated screen per cow with seven horizontally-scrollable tabs:

**Tab 1 — Overview**
- Photo, names/aliases, tag, breed, DOB, age
- **Current state strip**: status (Open / Exposed / Confirmed / Lactating / Dry), pregnancy due if any, days since last calving, BCS, location
- **Tier badge** (A/B/C/D/E) — large, coloured, with one-line reasoning
- **Flag badges**: e.g. "Repeat breeder", "Lost calf 2025", "Late first calver"
- Buttons: Record Calving, Record Mating, Record Treatment, Record Weighing

**Tab 2 — Performance**
- **Headline metrics**:
  - Total calves
  - Calves alive
  - Calves per productive year (with peer-average comparison)
  - Average calving interval (months, vs peer average)
  - Age at first calving (vs target)
  - Months since last calving
- **Calving timeline**: a horizontal timeline showing every calving as a dot, intervals labelled
- **Tier history**: line chart of her tier over time; tier-change annotations
- **Sparklines**: BCS, weight (where captured)

**Tab 3 — Calvings**
- List of every calving event with calf name, sex, sire, date, weight, status
- Tap any calf to navigate to that calf's profile

**Tab 4 — Breeding**
- All services recorded
- All pregnancy checks recorded
- Current pregnancy timeline if applicable
- Bull(s) used historically with progeny outcome

**Tab 5 — Health**
- Vaccination matrix (which vaccines current/overdue)
- Treatment history
- Deworming and dipping
- Any chronic condition flags

**Tab 6 — Lineage**
- Ancestors (3 generations) — collapsible
- Descendants — collapsible
- Click any node to navigate
- Inbreeding indicator if her own F coefficient is non-zero

**Tab 7 — Financial**
- Acquisition cost (or born-on-farm)
- Lifetime feed cost
- Lifetime vet cost
- Revenue (calf sales, milk if recorded)
- Net contribution
- ROI estimate

**Peer benchmarking**
On the Performance tab, every metric carries a **peer average** drawn from cows in the same:
- Breed cohort
- Age cohort (±2 years)
- Farm (if multi-farm)

This makes "is she good or bad?" answerable without holding the whole herd in your head.

### 9.5 Bull Profile (sire view)

Each bull — whether the farm's own, a hire bull, or an external AI source — has a profile screen.

**Tab 1 — Overview**
- Identity, breed, owner, source, DOB, age
- Active / retired / sold
- Current paddock / cow group exposure
- Workload counter (current cows assigned, lifetime services)

**Tab 2 — Progeny statistics**
- **Total offspring** (alive / dead / sold)
- **Sex split**
- **Daughter performance**:
  - Average age at first calving
  - Average calving interval
  - Tier distribution
- **Son performance**:
  - Average weaning weight
  - ADG
  - Number kept as future sires vs sold

**Tab 3 — Calf list**
- Every calf attributed to this bull
- Sortable by DOB, status, dam

**Tab 4 — Mating history**
- Every service recorded
- Conception rate (positive preg-checks / services)
- Inbreeding F of each calf produced

**Tab 5 — Genetic compatibility**
- For every cow in the herd, the F-coefficient of a proposed mating
- Coloured: green (free) / yellow (warn) / red (block)
- "Approved mate" list (the cows this bull can safely cover)

**Tab 6 — Financial**
- Acquisition cost, lifetime cost, lifetime revenue attributed (from his calves), ROI

### 9.6 Group / Cohort comparisons
- Born-in-year cohort: e.g. "all cows born 2020 — which has the best CPY?"
- Breed cohort: e.g. "Brahman vs Brahman Cross average calving interval"
- Sire cohort: e.g. "Makantase's daughters vs herd average"
- Farm cohort: Jijo vs Olly vs Tumi

### 9.7 Predictive analytics (Phase 3)
- **Calving forecast** for the next 18 months (already-pregnant + likely-to-be-bred)
- **Income forecast** based on weaning weight predictions and current beef prices
- **At-risk cows** model — ML over historical features (interval drift, BCS drops, prior tier changes) to flag cows likely to enter tier D in the next 6 months
- **Inbreeding-pressure index** — herd-level measure of how constrained mating options have become

---

## 10. Auto-Flagging Engine for Under-Performers

A flagship capability: the system **automatically and continuously** identifies cows and bulls that are dragging herd performance and surfaces them for the owner's decision — with reasoning.

### 10.1 Why auto-flagging matters
With even 40 cows it is hard to remember each one's calving history, interval, last weight, and whether she's slipping. With 100+, it is impossible. The system carries that memory and pings the owner when action is needed.

### 10.2 What gets flagged
Three flag categories:

1. **Performance flags** — assigned by the tier engine when a cow's metrics drop below thresholds
2. **Event-based flags** — assigned when specific events occur (calf mortality, repeat-breeding, lost calf, vet attention required)
3. **Inactivity flags** — assigned when an animal has not had an expected event (e.g. cow not seen for 30 days, no weight recorded in 12 months, no service after 6 months open)

### 10.3 Flag catalogue

| Flag | Trigger | Severity | Notification |
|---|---|---|---|
| `low_cpy` | CPY < 0.5 sustained 2 months | High | Owner |
| `long_calving_interval` | Last interval > 18 months | High | Owner |
| `late_first_calver` | First calf at age > 4 years | Medium | Owner |
| `repeat_breeder` | ≥ 3 services without confirmed pregnancy | High | Owner + Vet |
| `never_calved_overdue` | Age ≥ 3.5 yrs, no calf, no current confirmed pregnancy | High | Owner |
| `calf_mortality` | A calf of hers died in first 30 d | Medium | Owner |
| `multiple_calf_losses` | ≥ 2 calf losses lifetime | High | Owner |
| `dystocia_history` | Calving difficulty ≥ 4 on last calving | Medium | Owner + Vet |
| `low_bcs` | BCS < 4 sustained 30 days | High | Owner + Vet |
| `weight_loss` | Weight loss > 10% in 60 days | High | Vet |
| `not_seen` | No event of any kind recorded for 30 days | Low | Manager |
| `overdue_pregnancy_check` | Service > 60 days ago, no preg-check | Medium | Manager |
| `overdue_calving` | Past expected calving date by > 7 days | High | Manager + Vet |
| `withdrawal_active` | Currently within withdrawal period | Info | None (informational) |
| `inbreeding_descendant` | Animal has F > 0.0625 (inbred) | Info | None |

### 10.4 Tier assignment matrix

Every breeding cow is assigned a tier on every nightly run **and** on every event that affects her metrics. The assignment is rule-based and explainable.

| Tier | Criteria (all must be true) | Action |
|---|---|---|
| **A — Top performer** | CPY ≥ 0.75 AND age at first calf ≤ 36 mo AND no recent calf loss AND interval ≤ 14 mo | Keep, prioritise breeding |
| **B — Good** | CPY ≥ 0.5 AND interval ≤ 16 mo | Keep |
| **C — Average** | CPY 0.4–0.5 OR interval 16–18 mo | Keep, monitor |
| **D — Watch** | CPY 0.25–0.4 OR interval 18–24 mo OR age at first calf > 4 | Cull if next interval > 18 mo |
| **E — Cull candidate** | CPY < 0.25 OR never bred at age ≥ 3.5 OR ≥ 2 calf losses | Cull recommended |

First-time mothers (calved within 12 months of first service) are exempt from tier assignment until their second calving and are tracked in a separate "Prove next interval" cohort.

### 10.5 Escalation policy

| State | What happens |
|---|---|
| Tier downgrade (e.g. B → C) | Logged silently; visible on animal profile |
| New flag assigned | In-app notification to manager |
| Tier drops to D | WhatsApp notification to owner + add to Watch list |
| Tier D for 2 consecutive evaluations | WhatsApp + email to owner with reasoning; cow added to **Sale Candidate** queue |
| Tier E | WhatsApp + email immediately with reasoning |
| Tier E for 2 consecutive evaluations | Owner sees a "Decision required" task on home screen; once approved, cow moves into Sale workflow |

The owner can dismiss a recommendation with a reason (e.g. "Keeping for sentimental value", "Hand-raised orphan, no other use"). Dismissed cows continue to be tracked but stop generating notifications.

### 10.6 Bulls also get flagged

| Flag | Trigger |
|---|---|
| `low_conception_rate` | Conception rate < 60% after 10 services |
| `daughters_underperform` | Average daughter tier < B and ≥ 5 daughters of breeding age |
| `inbreeding_constrained` | Can mate < 50% of breeding cows due to lineage |
| `retirement_age` | Age ≥ 7 years |

### 10.7 Reasoning transparency

Every flag and tier carries a `reason` string and a `metrics` object showing the exact values that triggered the assignment. Example:

```json
{
  "flag": "long_calving_interval",
  "severity": "high",
  "assigned_at": "2026-05-25T05:00:00Z",
  "reason": "Last calving interval 19.3 months exceeds threshold of 18 months",
  "metrics": {
    "last_calving_interval_months": 19.3,
    "threshold_months": 18,
    "calving_history": [
      {"date": "2023-12-13", "calf": "Poelo"},
      {"date": "2025-08-12", "calf": "Tlhabi"}
    ]
  }
}
```

This makes every decision contestable and auditable.

### 10.8 Bulk actions from the flag list
From the herd analytics underperformer carousel, owners can:
- Mark multiple cows as "Approved for sale"
- Schedule a batch vet visit for flagged cows
- Export the flagged list to PDF for a co-decision discussion

---

## 11. Automation & Business Rules

This section enumerates the **event-driven automation logic**. Each entry has trigger, preconditions, ordered actions, and failure modes.

> **Implementation strategy.** Rules begin life as plain C# classes inside `FarmManager.Application/Flagging/` (the **Automation Engine** pillar). When the rule count grows large or non-developers need to edit them, the same rules are migrated to **NRules** — a forward-chaining production-rules engine for .NET — without changing the external behaviour. The MVP target is to keep this strategy as a refactor-when-needed, not a Day-One investment. Background work (nightly recalculations, reminder dispatch, WhatsApp delivery) runs on **Hangfire** (with **Quartz.NET** as a UI-less alternative).

### 11.1 RULE-001: Calving Event Recorded
**Trigger**: User creates a `CalvingEvent`.

**Preconditions**:
- Dam exists, female, age ≥ 18 months at calving date
- Dam status ∈ { Pregnant Confirmed, Exposed, Unconfirmed }
- Calving date ≤ today
- Calving date ≥ dam's previous calving date + 250 days

**Actions (atomic transaction)**:
1. Create `Animal` for the calf and **assign code-name** (RULE-019)
2. Set `calf.status = Active` (or Dead if stillbirth)
3. Set `dam.status = Lactating`
4. Set `dam.last_calving_date`
5. Set `dam.next_breeding_window_open = calving_date + 45 days`
6. Set `dam.next_breeding_target = calving_date + 90 days`
7. Recompute `dam.calf_count`, `calves_alive`, `avg_calving_interval`, `cpy`
8. Recompute `dam.performance_tier` and log tier change if any
9. Append to `dam.calving_history`
10. Schedule reminder: heat check at calving + 35 days
11. Schedule reminder: weaning at calving + 7 months
12. Emit `calving.recorded.v1`
13. Increment herd statistics counters
14. If calf is (B)-sired, flag descendants accordingly
15. Notify Farm Manager and Owner

### 11.2 RULE-002: Mating Event Recorded
**Actions**:
1. Append service to history
2. Set `cow.status = Exposed`
3. Set `cow.expected_calving_date = service_date + 283 days`
4. Schedule preg-check at service + 60 days
5. Schedule pre-calving reminders (30 / 14 / 7 days)
6. Increment bull workload
7. Emit `mating.recorded.v1`

### 11.3 RULE-003: Pregnancy Check Recorded
**Positive**: status → Pregnant Confirmed; refine due date; schedule pre-calving reminders; emit `pregnancy.confirmed.v1`.
**Negative**: status → Open; clear expected calving; raise repeat-breeder flag if cumulative; emit `pregnancy.failed.v1`.
**Inconclusive**: status stays; re-check scheduled at +30 d.

### 11.4 RULE-004: Health Event Captured
1. Record event
2. Compute `next_due_date = event_date + interval`
3. Apply `withdrawal_until`; block sale screen
4. Decrement inventory stock
5. Schedule reminder at next due − 7 days
6. Emit `health.captured.v1`

### 11.5 RULE-005: Weighing Captured
1. Record weight
2. Compute ADG
3. Update growth curve
4. Compare against breed standard; flag if outside ±20% band
5. Suggest weaning if calf and weight ≥ target
6. Emit `weighing.recorded.v1`

### 11.6 RULE-006: Sale / Death
**Sale**: enforce withdrawal; set status Sold; recompute counts; generate invoice; emit `sale.completed.v1`.
**Death**: status Dead; record cause; attribute to dam mortality if calf; flag camp if contagious; emit `death.recorded.v1`.

### 11.7 RULE-007: Performance Tier Recalculation
**Trigger**: Nightly + on any of {calving, sale, death, weighing} on a breeding cow.

**Actions per breeding cow**:
1. Pull calving history, last weighing, BCS
2. Compute all metrics in Section 9.3
3. Apply tier matrix
4. If tier changed: log tier change, notify per escalation policy (Section 10.5)
5. Re-evaluate all flags from catalogue (Section 10.3)
6. Update flag set (new flags added, resolved flags removed)
7. Emit `tier.changed.v1` if applicable
8. Emit `flag.assigned.v1` / `flag.resolved.v1` for each delta

### 11.8 RULE-008: Inbreeding Check (BLOCKING)
*See Section 12.3.*

### 11.9 RULE-009: Heat → Suggest Mating
Propose top 3 compatible bulls by genetic distance + workload.

### 11.10 RULE-010: Calving Calendar Auto-Maintenance
Recompute on any breeding event; sort by due date; highlight conflicts.

### 11.11 RULE-011: Boshomane Daughter Marking
On calving with sire = Boshomane, set `calf.is_b_sired = true` and add him to permanent block list for that calf.

### 11.12 RULE-012: Withdrawal Period Enforcement
On sale attempt, check `animal.withdrawal_until`. Block if not elapsed.

### 11.13 RULE-013: Lost / Stolen / Strayed
Mark Missing; re-check at 7/14/30 d; auto-promote to Lost at 60 d if unresolved.

### 11.14 RULE-014: Auto-Weaning Suggestion
At calf 6 months or weight ≥ weaning target, suggest weaning event.

### 11.15 RULE-015: Repeat-Breeder Detection
After 3+ services without confirmed pregnancy, flag and suggest vet investigation.

### 11.16 RULE-016: First-Time Mother Special Handling
On heifer's first calving, mark cohort; intensify 60-day monitoring; collect mothering ability; defer tier assignment for one cycle.

### 11.17 RULE-017: Underperformer Auto-Flag
**Trigger**: Nightly + on every event affecting a breeding cow / active bull.

**Actions**:
1. Re-evaluate every flag in catalogue (Section 10.3 + 10.6)
2. For each newly-assigned flag:
   - Persist with `assigned_at`, `reason`, `metrics`
   - Dispatch notification per Section 10.5
3. For each newly-resolved flag:
   - Set `resolved_at`
   - Notify if it was a high-severity flag (positive news matters)
4. Update animal's `flag_set` in cache for fast queries

### 11.18 RULE-018: Herd KPI Snapshot
**Trigger**: Nightly at 02:00 SAST.

**Actions**:
1. Compute every metric in Section 9.3
2. Persist a snapshot with `as_of_date`
3. Recompute deltas vs same period last year and last month
4. Update dashboard cache
5. Generate "morning brief" notification for owner (07:00) with top 3 deltas

### 11.19 RULE-019: Auto Code-Name Generation
**Trigger**: New animal entering the system from any source — calving (RULE-001), purchase, inheritance, transfer-in, or legacy migration.

**Preconditions**:
- Animal record being created has a known DOB or acquisition date
- Organisation ID is known
- A code-name template is configured (defaults applied if not)

**Actions (atomic, within the parent transaction)**:
1. Resolve the prefix: from organisation template based on source
   - Born on farm: prefix from `org.calf_prefix` (default `C`)
   - Purchased: `P`
   - Legacy migration: `L`
   - Custom override allowed at registration
2. Resolve the year: from DOB year (calf) or acquisition year (purchased/legacy)
3. **Atomically reserve the next sequence number** for this `(organisation_id, prefix, year)` triple
   - Implemented via a per-(org, prefix, year) sequence row in Postgres with `SELECT … FOR UPDATE` or via Postgres `SEQUENCE` objects per triple
   - Guarantees uniqueness even under concurrent calvings
4. Format the code-name from the template: `${prefix}-${year}-${seq.toString().padStart(width, '0')}`
   - `width` defaults to 3; auto-widens if year sequence > 999
5. Persist `animal.code_name`; mark immutable (no further updates allowed via API)
6. Index the code-name for full-text + exact-match search
7. If the animal is registered through the calving flow, return the code-name to the mobile client in the same response so the worker sees the toast immediately
8. Emit `animal.code_name.assigned.v1` (sub-event of `animal.registered.v1`)

**Failure modes**:
- If the sequence reservation fails (extreme contention) → retry up to 3 times with jittered backoff
- If the template references an undefined variable → fall back to default `C-YYYY-NNN` and log a warning
- Code-name is **never** reused if an animal is deleted or its registration is reversed — the sequence number is consumed permanently (auditable)

**Configuration UI (Admin)**:
A single screen lets the org admin choose:
- Calf prefix (e.g. `C`, `T`, `TUMI`)
- Sequence width (3 or 4 digits)
- Template (preview: "Next calf will be `T-2026-024`")
- Separate prefixes for purchased / legacy if desired

---

## 12. Lineage & Inbreeding Engine

### 12.1 Lineage representation
- Each animal has 0–1 dam and 0–1 sire
- Unknown parents modelled explicitly (not null)
- External sires recorded with breed, owner, contact, optional pedigree

### 12.2 Pedigree query API
- `getAncestors(animal, gens=4)`
- `getDescendants(animal, gens=4)`
- `getFullSiblings(animal)`
- `getHalfSiblings(animal)`
- `areRelated(a, b) → relationship | null`
- `commonAncestors(a, b)`
- `inbreedingCoefficient(a, b)`

### 12.3 Wright's coefficient
```
F(offspring) = Σ over common ancestors C of:
  (1/2)^(n_A + n_B + 1) × (1 + F_C)
```

| F | Relationship | Action |
|---|---|---|
| 0.0000 | Unrelated | Allow |
| 0.0156 | 2nd cousins | Allow with note |
| 0.0625 | First cousins | Warn |
| 0.1250 | Half-siblings, grandparent-grandchild | Block with override |
| 0.2500 | Full siblings, parent-offspring | Hard block |

### 12.4 Boshomane-specific seed rules
Rapula and Makaku (Boshomane's sons) cannot mate Boshomane's daughters:
Bali, Madikizela, Smongo, Tlhabi, Lele, Noxy, Poelo, Motlalepula, Mpolokeng, Nomsa, Puleng, Bontle, Lesedi.

Mawick (out of Baizani) and Amerko (out of Nandipha Magudumana) are non-(B) and free to mate Boshomane's daughters.

### 12.5 Visualisation
Interactive pedigree tree built with **React Flow** ([reactflow.dev](https://reactflow.dev)):
- Each animal is a custom node showing photo, code-name, given name, sex, breed, tier badge
- Edges show dam/sire relationships, colour-coded (blue = sire, pink = dam)
- Inbred branches highlighted in red
- Pinch-to-zoom + drag-to-pan on mobile; mouse wheel + drag on desktop
- Tap any node to navigate to its profile
- Collapsible generations (default 3 up, 3 down; expand on demand)
- (B)-sired animals get a small yellow chip on the node
- PDF export for pedigree certificates (rendered server-side via QuestPDF + server-rendered React Flow snapshot)

### 12.6 Offspring statistics maintained per parent
*See Section 9.5.*

---

## 13. Workflows

### 13.1 Field-side calving capture (mobile, offline)
1. Worker taps "Quick record → Calving" on phone home screen
2. Scans dam's ear tag (or picks from recently viewed)
3. One screen: date/time (default now), sex, weight (optional), difficulty slider, photo, voice note
4. Saves locally; **code-name `T-2026-024` issued immediately** (sequence reserved locally with conflict resolution on sync); toast confirms
5. Sync queues
6. When network returns, RULE-001 + RULE-019 actions reconcile on server (the local-temp code-name may be re-mapped if a true conflict occurred — very rare with per-org sequencing)
7. Owner gets WhatsApp: "**T-2026-024** — Mantabole has calved (F, healthy). Photo attached. Tap to view."

### 13.2 Pre-mating decision flow
1. Manager flags cow in heat
2. App shows top 3 compatible bulls with F coefficients & workload
3. Manager picks → mating recorded with timestamp

### 13.3 Vaccination day
1. Vet opens "Health → Schedule today" on phone
2. Sees list of animals due
3. Bulk-selects (or scans tags)
4. Confirms — all events recorded; inventory decremented; next-due scheduled

### 13.4 Monthly herd review (owner)
1. Owner taps "Analytics" on phone
2. Reviews KPI deltas, tier distribution donut
3. Swipes to underperformer carousel
4. Taps a flagged cow → reads reason → approves / dismisses recommendation
5. Approved cows queue for sale workflow

### 13.5 Bull rotation / purchase
1. System suggests bull retirement at age 7+
2. Owner shortlists candidates with pedigrees
3. System computes compatible-cow % for each candidate
4. Owner picks; new bull onboarded

---

## 14. Dashboards

### 14.1 Mobile — Owner home
- KPI strip (live, pregnant, calves YTD, alerts)
- Tier donut
- Today's tasks (3 most urgent)
- Underperformer count (badge with red)
- Quick-capture row

### 14.2 Mobile — Manager home
- Today's tasks (full list)
- 4 quick-capture buttons
- "Animals needing attention" cards
- Offline-sync queue indicator

### 14.3 Mobile — Vet home
- Animals flagged for vet
- Active withdrawal periods
- Preg-check schedule
- Mortality alerts

### 14.4 Mobile — Field worker home
- 4 huge quick-capture buttons
- "My pending uploads"
- Today's task list

### 14.5 Web — Owner dashboard
Same KPIs as mobile, but with larger charts, full-pivot tables, side-by-side trend lines, and a Watch List panel.

### 14.6 Web — Manager operational
Daily/weekly schedule, inventory levels, vet visit planner.

### 14.7 Web — Bookkeeper
Sales register, purchases, outstanding invoices, VAT summary.

---

## 15. Reporting

All reports exportable as PDF, Excel, CSV. Mobile users get a "Share as PDF" action.

### 15.1 Standard reports

| Report | Frequency | Surface |
|---|---|---|
| Herd Census | On-demand | Mobile + Web |
| Calving Calendar | Monthly | Mobile + Web |
| Calving Performance | Annual | Web |
| **Performance Ranking (tier A–E)** | Monthly | Mobile + Web |
| **Underperformer Report** | Monthly | Mobile + Web |
| Cull Candidate List | Monthly | Mobile + Web |
| Heifer Pipeline | Quarterly | Mobile + Web |
| Bull Calf Plan | Quarterly | Mobile + Web |
| **Bull Progeny Statistics** | Per bull | Mobile + Web |
| **Cow Performance Profile** | Per cow | Mobile + Web |
| Vaccination Compliance | Monthly | Mobile + Web |
| Mortality & Loss | Annual | Web |
| Sales Register | Monthly | Web |
| Purchases Register | Monthly | Web |
| Pedigree Certificate | Per animal | Web |
| Lineage Audit | Per animal | Mobile + Web |
| Inbreeding Risk | On mating proposal | Mobile |
| Inventory Stock | On-demand | Mobile + Web |
| Financial P&L | Monthly / Annual | Web |
| Tax Summary | Annual | Web |
| Owner Statement (per herd: Jijo/Olly/Tumi) | Monthly | Mobile + Web |

### 15.2 Ad-hoc query builder (web only)
Filter by breed, sex, age range, tier, location, last event, sire, dam, status. Group, aggregate, save, schedule.

---

## 16. Notification & Reminder Engine

### 16.1 Channels
- Push notifications (mobile app) — **primary**
- WhatsApp Business API — primary for SA market
- In-app notification center
- Email (transactional + daily digest)
- SMS (gateway: Clickatell)
- Voice call (critical, opt-in)

### 16.2 Notification types
| Type | Default channel | Configurable |
|---|---|---|
| Calving due in 7 days | Push + WhatsApp | ✓ |
| Cow calved (auto-recorded) | Push + WhatsApp | ✓ |
| **New underperformer flag** | Push + WhatsApp | ✓ |
| **Tier downgrade to D or E** | Push + WhatsApp + Email | ✓ |
| Vaccination overdue | Push + WhatsApp | ✓ |
| Inbreeding blocked | Push (immediate) | ✗ |
| Animal lost / not seen | Push + WhatsApp | ✓ |
| Low inventory stock | Email digest | ✓ |
| Sale completed | Email | ✓ |
| Mortality | Push + WhatsApp + Email | ✓ |
| Pregnancy check due | Push | ✓ |
| **Morning brief** (KPI deltas) | Push + Email | ✓ |

### 16.3 Reminders
Each reminder persisted with target_user, due_at, channels, template, payload. Snooze/dismiss per recipient. Bulk-batched digests at 06:00 SAST.

### 16.4 Quiet hours
Per-user (default 21:00–06:00 SAST). Only critical alerts override.

### 16.5 Localisation
English, Afrikaans, Setswana, isiZulu, Sesotho.

---

## 17. User Roles & Permissions

### 17.1 Roles
Super Admin · Owner · Farm Manager · Vet · Field Worker · Bookkeeper · Read-only Observer.

### 17.2 Permission matrix

| Capability | Super | Owner | Mgr | Vet | Worker | Book | Obs |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| View herd | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| View analytics | ✓ | ✓ | ✓ | ✓ | – | ✓ | ✓ |
| View animal profile | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Add/edit animal | ✓ | ✓ | ✓ | – | – | – | – |
| Record calving | ✓ | ✓ | ✓ | ✓ | ✓ | – | – |
| Record mating | ✓ | ✓ | ✓ | ✓ | ✓ | – | – |
| Record vaccination | ✓ | ✓ | ✓ | ✓ | ✓ | – | – |
| Approve cull | ✓ | ✓ | – | – | – | – | – |
| Record sale | ✓ | ✓ | ✓ | – | – | ✓ | – |
| Edit financials | ✓ | ✓ | – | – | – | ✓ | – |
| View audit log | ✓ | ✓ | ✓ | – | – | – | – |
| Manage users | ✓ | ✓ (own farms) | – | – | – | – | – |

### 17.3 Multi-tenancy
Data partitioned by Organisation. Users may belong to multiple orgs with different roles per org.

---

## 18. Database Entities

### 18.1 Core schemas (sketch)

**Animal**
```
id, organisation_id, farm_id,
code_name (string, unique within org, immutable, indexed),  -- e.g. "C-2026-003"
code_name_prefix, code_name_year, code_name_sequence,        -- denormalised parts
primary_name, aliases[], external_tag, rfid,
sex, breed_id, breed_composition (jsonb), dob, dob_precision,
sire_id, dam_id, sire_external (jsonb),
status, disposal_date, disposal_reason, location_id, photo_urls[],
is_b_sired (bool), withdrawal_until, performance_tier,
created_at, updated_at, created_by, updated_by
```

**CodeNameSequence** (new)
```
organisation_id, prefix, year, next_sequence
PRIMARY KEY (organisation_id, prefix, year)
```
Atomic increment guarantees uniqueness on concurrent calvings.

**CalvingEvent**
```
id, dam_id, calf_id, calving_date, difficulty_score,
assistance_required, placenta_delivered, mothering_ability,
sire_id, calf_sex, calf_weight_kg, calf_vigour, notes, attachments
```

**ServiceEvent**
```
id, cow_id, bull_id, ai_straw_id, service_date, service_type,
expected_calving_date, inbreeding_coefficient, notes
```

**PregnancyCheckEvent**
```
id, cow_id, check_date, method, result, days_bred, vet_id, notes
```

**HealthEvent**
```
id, animal_id, event_date, event_type, product_id, dose, route, vet_id,
batch_number, expiry, withdrawal_until, next_due_date, notes
```

**WeighingEvent**
```
id, animal_id, weighing_date, weight_kg, method, body_condition_score, notes
```

**Flag**
```
id, animal_id, flag_code, severity, assigned_at, resolved_at,
reason (text), metrics (jsonb), source_event_id
```

**TierAssignment**
```
id, animal_id, tier, assigned_at, assigned_by (system/user), reason,
metrics (jsonb), previous_tier
```

**HerdKpiSnapshot**
```
id, organisation_id, farm_id, as_of_date, metric_name, value,
delta_vs_last_period, delta_vs_last_year
```

**SaleEvent, PurchaseEvent, DeathEvent, TransferEvent, InventoryItem, StockMovement, ScheduleTemplate, Reminder, AuditLog, User, Role, Permission, OrganisationMembership, Farm, Location, LineageRelationship** — as previously defined.

### 18.2 Database choices
- PostgreSQL 16 (one schema per microservice)
- OpenSearch for full-text and faceted animal/event search
- Redis for caches (lineage paths, KPI snapshots, hot dashboards) and idempotency keys
- S3 (af-south-1) for photos, voice notes, PDFs
- Optional graph DB (Neo4j) for very large pedigree queries (Phase 3)

---

## 19. Audit & Historical Tracking

- Append-only event log is source of truth; current state is a projection.
- No physical deletes; soft-delete with tombstones.
- Every mutation → AuditLog with before/after JSON.
- Corrections via corrective events, not history mutation.
- As-of queries supported ("herd as of 2025-12-31").
- Retention: events indefinite; audit log ≥ 7 years (SARS, POPIA).
- Any event can be voided with reason; downstream recomputation cascades.

---

## 20. Integrations

### 20.1 First-party (MVP)
- WhatsApp Business API
- Email/SMTP (Mailgun / AWS SES)
- SMS (Clickatell)
- PDF generation (Puppeteer)
- Payment (Yoco / Peach for SaaS billing later)

### 20.2 Phase 2+
- Weighbridge / scales (Tru-Test, Gallagher)
- Accounting (Xero, Sage One, QuickBooks)
- SAStud, DALRRD, SAMIC (government / breed society)
- Auction platforms (Vleissentraal, BKB)
- Weather API
- Vet practice management systems

### 20.3 IoT (Phase 3)
- Smart collars (SCR Heatime)
- Auto-weighing stations
- Geofencing collars
- Drone surveys

### 20.4 API
Public REST API (versioned, OpenAPI). Webhooks for outbound events. Bulk import/export.

---

## 21. Non-Functional Requirements

### 21.1 Performance
| Metric | Target |
|---|---|
| Mobile cold start | ≤ 2 s on mid-range Android |
| Mobile screen render | ≤ 300 ms |
| Mobile event capture (offline) | ≤ 200 ms |
| Mobile sync (100 events) | ≤ 10 s on 3G |
| Web page load | P95 ≤ 2.0 s on 4G |
| Search | P95 ≤ 300 ms |
| Pedigree 4-gen query | P95 ≤ 500 ms |
| KPI snapshot generation | ≤ 5 s for 10k animals |
| Bulk import 1000 animals | ≤ 30 s |

### 21.2 Availability
- 99.5% uptime SLA
- Multi-AZ deployment
- Mobile app remains functional offline indefinitely

### 21.3 Scalability
| Dimension | MVP | 3-year |
|---|---|---|
| Animals | 100 | 100,000 |
| Organisations | 1 | 1,000 |
| Concurrent users | 10 | 5,000 |
| Events/day | 100 | 1,000,000 |
| Photo storage | 10 GB | 5 TB |

### 21.4 Security
- TLS 1.3 everywhere
- OIDC (Keycloak)
- MFA for Admin and Owner
- Field-level encryption for financial PII
- POPIA compliance
- Annual penetration test
- Secrets in Vault / AWS Secrets Manager
- Audit log with hash-chain

### 21.5 Data protection
- POPIA + GDPR-aligned
- Daily backups, 30-day retention; weekly retain 1 year
- Cross-region DR with RPO ≤ 1 h, RTO ≤ 4 h

### 21.6 i18n / a11y
- English, Afrikaans, Setswana, isiZulu, Sesotho
- WCAG 2.1 AA on web; equivalent on mobile
- Configurable font scaling
- High-contrast mode

### 21.7 Maintainability
- Backend code coverage ≥ 80%
- API contract tests
- Terraform IaC
- Blue-green deployments

---

## 22. Suggested Architecture

The detailed tech stack lives in a companion document: **[Tech_Stack.md](./Tech_Stack.md)**. This section summarises only the architectural shape.

> **Approved stack**: **Next.js + ASP.NET Core + PostgreSQL + Docker**, deployed on Ubuntu (Hetzner today, any cloud later). The platform is built as **seven modular pillars** — web dashboard, mobile-friendly field capture, API backend, PostgreSQL database, automation engine, reporting engine, notification engine — each independently testable and replaceable. See `Tech_Stack.md §5b` for the per-pillar specification.

### 22.1 Stack summary
- **Web app**: Next.js 15 + React 19 + TypeScript + Tailwind CSS + ShadCN UI, served as a Progressive Web App (Serwist service worker, Web App Manifest, Web Push). Installable on the phone home screen.
- **Backend**: **ASP.NET Core 9 Web API** (C# 13), Clean Architecture (Domain / Application / Infrastructure / Api). MediatR for CQRS, FluentValidation, Mapster.
- **ORM**: Entity Framework Core 9 with Npgsql; Dapper for hot-path queries (lineage, analytics).
- **Database**: PostgreSQL 16 (one DB to start; per-context schema separation).
- **Cache**: Redis 7 (sessions, idempotency, pedigree, KPI cache).
- **Messaging / events**: RabbitMQ + MassTransit (outbox pattern). At MVP volume, in-process MediatR notifications may suffice; introduce MassTransit when first cross-process consumer appears.
- **Background jobs**: Hangfire (nightly tier recalc, KPI snapshots, reminder dispatch).
- **Auth**: ASP.NET Core Identity + JWT (access + refresh). MFA via TOTP for Owner/Admin. WebAuthn / Passkeys for biometric login on PWA.
- **Storage**: MinIO (S3-compatible) for photos, voice notes, PDFs.
- **Reverse proxy / TLS**: Caddy 2 (auto-HTTPS via Let's Encrypt).
- **Observability**: Serilog → Seq (or Grafana Loki) for logs; Prometheus + Grafana for metrics; OpenTelemetry → Tempo for traces.
- **Search**: PostgreSQL FTS at MVP; Meilisearch later.
- **Notifications**: WhatsApp Cloud API (Meta) for messaging; Web Push API for browser/PWA push; Clickatell for SMS; Mailgun/Brevo for email.

### 22.2 Container & host topology
All services run as Docker containers on a single **Hetzner Cloud Ubuntu 24.04 LTS** VM at MVP scale. Docker Compose orchestrates locally and in production. Same `docker-compose.prod.yml` lifts and shifts unchanged to AWS / Azure / GCP managed-container services later.

```
Caddy (host:80,443) ─┬─► Next.js  (web container, :3000)
                     ├─► ASP.NET API (api container, :5000)
                     ├─► MinIO     (storage container)
                     └─► Grafana   (admin only)

ASP.NET API ──► Postgres (db container)
            ──► Redis    (cache container)
            ──► RabbitMQ (mq container)
            ──► MinIO    (object storage)
```

### 22.3 Patterns
- Clean Architecture layering inside the .NET solution (Domain, Application, Infrastructure, Api)
- CQRS via MediatR (commands change state; queries read projections)
- Outbox pattern for reliable event publication (Postgres outbox table → MassTransit relay)
- Versioned domain events (`<aggregate>.<verb>.v<n>`)
- Idempotency keys on every non-GET endpoint
- Background workers process slow tasks (Hangfire)
- 12-factor configuration via env vars — same image runs on Hetzner, AWS, Azure, GCP

### 22.4 Mobile (PWA) sync architecture
- The browser caches the app shell via service worker
- IndexedDB (via Dexie) holds the user's herd snapshot for offline reads
- Events created offline queue in IndexedDB; the **Background Sync API** flushes them to the API on reconnect
- Server is authoritative for conflict resolution
- Web Push API delivers notifications when the PWA is closed

### 22.5 Hosting & cost (MVP)

| Item | Monthly cost |
|---|---|
| Hetzner CCX13 VM (2 vCPU dedicated, 8 GB RAM) | €14 |
| Hetzner Volume 40 GB SSD | €2 |
| Hetzner Backups (daily snapshot) | €3 |
| Floating IPv4 | €1 |
| Domain (amortised) | €1 |
| WhatsApp / SMS / email | €0–10 |
| **Total** | **~€20–30 / month** |

### 22.6 Cloud-ready by design
The same Docker images and Compose file run on:
- **K3s on Hetzner** (low-friction K8s upgrade)
- **AWS ECS Fargate** + RDS PostgreSQL + ElastiCache + SQS + S3
- **Azure Container Apps** + Azure Database for PostgreSQL + Azure Cache for Redis + Service Bus + Blob Storage
- **GCP Cloud Run** + Cloud SQL + Memorystore + Pub/Sub + Cloud Storage

Switching is a connection-string change, not a code change. See `Tech_Stack.md §7.5` for the migration matrix.

---

## 23. Scalability

### 23.1 Vertical
- Read replicas for analytics and reporting
- Materialised views for tier rankings and KPI snapshots
- Denormalised lineage adjacency for fast tree queries
- Photo storage offloaded to S3

### 23.2 Horizontal
- Stateless services behind LB; HPA on CPU + RPS
- Sharding by `organisation_id` post-Phase-3
- Event consumers scale per topic independently

### 23.3 Caching
- Animal profile (TTL 15 min, evict on event)
- Pedigree tree (TTL 1 h, evict on parent-link change)
- Dashboard KPIs (refreshed nightly + on-demand)
- Reports pre-generated daily

### 23.4 Bulk operations
- Imports up to 50k rows handled async
- Async report generation with email-when-ready

---

## 24. Implementation Roadmap

### 24.1 Phase 1 — MVP (months 0–3)
**Goal**: replace the spreadsheet on a phone.
- Animal registration
- Lineage (dam/sire + basic inbreeding warning)
- Calving (RULE-001), Mating (RULE-002), Preg check (RULE-003)
- Performance tier engine + auto-flagging (RULE-007, RULE-017)
- **Mobile app** with offline capture (calving, mating, treatment, weighing)
- Owner + Manager dashboards (mobile + web)
- Performance Ranking + Cow Profile + Bull Profile views
- Underperformer carousel
- Web Herd Analytics dashboard (basic)
- Push + WhatsApp notifications
- Single farm, multi-user with RBAC
- Audit log
- Excel export

### 24.2 Phase 2 — Operations (months 3–6)
- Vaccination, deworming, dipping schedules (RULE-004)
- Inventory module
- Withdrawal period enforcement
- Sales / purchases / deaths / transfers
- Heifer Readiness + Bull Calf Plan reports
- Bookkeeper role
- Bull progeny statistics (full)
- Group / cohort comparison reports
- Mobile glove-mode and voice-note transcription

### 24.3 Phase 3 — Intelligence (months 6–12)
- Full Wright's inbreeding with the seeded lineage rules
- Heat detection → mating recommendation (RULE-009)
- Predictive analytics (at-risk cows, calving forecast, income forecast)
- Bookkeeping integrations (Xero / Sage)
- Pedigree certificates
- Tier-change history with charts
- IoT-ready (smart collars optional)

### 24.4 Phase 4 — Enterprise (months 12–18)
- Weighbridge integration
- Auction platform integration
- Government compliance reporting (SARS, DALRRD)
- Public API + webhooks
- Multi-language UI (Setswana, Afrikaans, Zulu)

### 24.5 Phase 5 — Multi-tenant SaaS (months 18+)
- Self-service onboarding
- Per-customer billing
- Cooperative / family-group support
- Regional expansion (Botswana, Namibia, Zimbabwe)
- Sheep / goat / game modules

---

## 25. Appendices

### Appendix A — Source data summary (real herd basis)
- 40 head live
- Breeds: Boran Cross (2), Brahman × Boran (10), Brahman (13), Brahman Cross (13), Mix Breed (2)
- Sex: 34 F / 6 M
- Confirmed pregnant: 14
- Open: 8
- Heifers maturing: 14
- Bull calves: 6 (4 sire candidates, 2 weaners)
- Tiers (cows): A=5, B=6, C=3, D=3, E=3

### Appendix B — Reference values
- Cow gestation: **283 days**
- Heifer first breeding: **24–30 months** (default 28)
- Calf weaning: **6–8 months** (default 7)
- Optimal calving interval: **365–425 days**
- Cull threshold (CPY): **< 0.5**
- BCS target at breeding: **5–6**
- ADG target (Brahman calf weaning → yearling): **0.6–0.8 kg/day**

### Appendix C — (B) animals in current herd
**Boshomane's daughters** (cannot be bred to his sons): Bali, Madikizela, Smongo, Tlhabi, Lele, Noxy, Poelo, Motlalepula, Mpolokeng, Nomsa, Puleng, Bontle, Lesedi.
**Boshomane's sons**: Rapula, Makaku.
**Non-(B) sire candidates**: Mawick (out of Baizani), Amerko (out of Nandipha Magudumana).

### Appendix D — Inherited assumptions
- Georgina = Springbok (single animal, two names — using **Georgina** as canonical)
- Nomsa's mother = Serena (date match)
- Gijima, Russell, Mzambia, Nyovest, Benny confirmed sold
- Loriza Impi was a considered purchase, **not** a sire
- Mphonyana deceased 02/02/2025 (lightning)
- Kgomotso/Tshidi died as calf, early 2025
- "Borman x Simbra" is a typo for "Boran x Simbra"
- All 14 cows on the Calving Calendar are confirmed pregnant

### Appendix E — Event catalogue (versioned)
- `animal.registered.v1`
- `animal.code_name.assigned.v1`
- `calving.recorded.v1`
- `mating.recorded.v1`
- `pregnancy.confirmed.v1`
- `pregnancy.failed.v1`
- `health.captured.v1`
- `weighing.recorded.v1`
- `sale.completed.v1`
- `purchase.completed.v1`
- `death.recorded.v1`
- `transfer.completed.v1`
- `tier.changed.v1`
- `flag.assigned.v1`
- `flag.resolved.v1`
- `cull.recommended.v1`
- `inbreeding.blocked.v1`
- `inventory.low.v1`
- `reminder.dispatched.v1`
- `kpi.snapshot.created.v1`

### Appendix F — Metric formulas

```
age_years = (today − dob) / 365.25
productive_years = max(0, age_years − 2)
cpy = total_calves / productive_years
calving_interval_avg_months = mean(date_n − date_n−1) / 30.4375
months_since_last_calving = (today − last_calving_date) / 30.4375
expected_calving_date = service_date + 283 days
heifer_ready_date = dob + 28 months
weaning_target_date = calving_date + 7 months
withdrawal_clearance = treatment_date + withdrawal_period_days
inbreeding_F = Σ (1/2)^(n_a + n_b + 1) × (1 + F_C)
adg = (weight_n − weight_n−1) / (date_n − date_n−1)
calving_rate = calves_born / breeding_cows_exposed
conception_rate = confirmed_pregnancies / services_recorded
calf_mortality_rate_30d = calf_deaths_first_30d / live_births
```

### Appendix G — Mobile-first design heuristics (cheat sheet)
- Touch target ≥ 48 dp
- Thumb-reachable primary actions
- One screen per task
- Smart defaults pre-fill ≥ 60% of fields
- Offline-first: every screen works without signal
- Voice notes on every event
- Photo capture defers to Wi-Fi
- Glove mode toggle
- Push notifications respect quiet hours
- No multi-step wizards on critical path

### Appendix H — Open questions
1. Sheep / goats from day one or cattle-only at MVP?
2. WhatsApp Business vs Cloud API budget?
3. Will the operation employ a bookkeeper or owner handles financials?
4. Other family members who will be users?
5. Preferred breed society / stud-book integration?
6. Granularity for grazing / camp management?
7. ML-based predictive features at MVP or rule-based only?
8. App distribution: Google Play / App Store / direct APK for rural-ops?

---

**End of specification.**

*This document is intended to evolve. Sections 9 (analytics), 10 (auto-flagging), and 11 (rules) will be detailed further during sprint planning, with technical design documents produced per bounded context.*
