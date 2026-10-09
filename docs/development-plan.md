# Development Plan — FantaRoster / ImagiRoster

> **Version:** 1.0 — October 2026
> **Scope:** from an empty repository to the pilot event (P0), then the first post-pilot release (P1).
> **Read first:** [`project-description.md`](project-description.md) (what and why). This file covers how and when.

---

## 1. Planning assumptions

| Assumption | Value |
|---|---|
| Team | One developer (the founder), helped by AI coding agents |
| Capacity | Side project: estimates are in **focused days** (≈ 6 productive hours). Calendar time depends on weekly availability |
| Platform | .NET 10 (SDK `10.0.401` or later, runtime `10.0.12` or later), C# 14, Aspire 13.x (13.4.6 or later) |
| Packages | Latest **stable** versions, pinned at scaffold time in `Directory.Packages.props`, updated weekly by Dependabot |
| Language | Everything in English: code, comments, commits, docs. User-facing strings are localized (EN, IT) through resources |
| Design | UI phases depend on the design system produced from [`design-prompt.md`](design-prompt.md) and stored in [`design/`](design/README.md) (mockups, tokens, assets). Until it is delivered, UI work uses unstyled components wired to placeholder tokens |
| Target | A working P0 deployed on Azure, rehearsed end to end, before the pilot event |
| Product scope | **Generic tool.** The pilot is a tech conference for go-to-market reasons only; every phase builds a context-neutral core (see `project-description.md` §1.1) |

### Estimate summary

| Phase | Focus | Estimate (focused days) |
|---|---|---|
| 0 | Foundations | 4 |
| 1 | Domain and persistence | 6 |
| 2 | Plugin framework, CSV, Sessionize | 5 |
| 3 | Organizer console | 6 |
| 4 | Participant PWA | 6 |
| 5 | Referee console and real time | 6 |
| 6 | Big screen | 2 |
| 7 | Branding, localization, hardening, deployment | 6 |
| 8 | Pilot rehearsal and event | 2 + event day |
| **P0 total** | | **≈ 43 days** |
| 9 | P1 release | ≈ 25 days |
| 10 | P2 exploration | to be planned after P1 |

Add a **20% buffer** to any calendar plan derived from these numbers.

---

## 2. Working agreements

### 2.0 Generic-first rule (applies to every phase)

The product is a generic tool; the tech conference is only the first use case brought to market. Throughout development:

- No context-specific logic, names or copy in the core projects (`Domain`, `Application`, `Web`, `Web.Client`, `Ui`). Words such as "speaker", "session", "talk", "team" or "match" appear only in domain packs, plugins and test fixtures.
- Every feature is described for at least **three use cases** from `project-description.md` §6 before it is built. If it fits only one, it goes into a domain pack or a plugin.
- Integration and end-to-end tests run the main journeys with **at least two domain packs** (Generic + Tech conferences), plus a **test-only pack** for a non-people context (e.g. teams in an amateur tournament) to catch hidden assumptions.
- Pilot feedback specific to conferences becomes domain-pack or plugin work, never a special case in the core.

### 2.1 Definition of Done (applies to every task)

- [ ] Code compiles with **zero warnings** (warnings are errors).
- [ ] `dotnet format --verify-no-changes` passes.
- [ ] Unit tests added or updated; all tests green.
- [ ] No hard-coded user-facing strings: EN and IT resources updated.
- [ ] Accessibility checked for any UI change (keyboard, focus, contrast, labels).
- [ ] UI changes match the mockups in `docs/design/` (or the difference is recorded in `docs/design/open-questions.md`).
- [ ] Generic-first check passed: no context-specific code or copy in the core; behavior verified with at least two domain packs.
- [ ] Docs updated when behavior, architecture or setup changes; an ADR is added for significant decisions.
- [ ] No new package without a reason written in the PR; only stable versions.
- [ ] Merged through a pull request with a green CI run.

### 2.2 Branching and commits

- Trunk-based: short-lived branches from `main`, merged by squash.
- Branch names: `feat/…`, `fix/…`, `chore/…`, `docs/…`.
- [Conventional Commits](https://www.conventionalcommits.org/) for commit and PR titles.
- Tags `v0.x.y` for deployable builds; `v1.0.0` after the pilot.

### 2.3 Architecture Decision Records

Stored in `docs/adr/NNNN-title.md`. Initial ADRs to write during Phase 0:

| ADR | Decision |
|---|---|
| 0001 | Modular monolith with clean layering (`Domain`, `Application`, `Infrastructure`, `Web`) |
| 0002 | Render modes: SSR public, Interactive Server organizer, Interactive WebAssembly participant/referee/big screen |
| 0003 | Commands over HTTP, SignalR for push only |
| 0004 | Leaderboard derived from score entries; points snapshotted on entries |
| 0005 | Single API replica for the pilot; Azure SignalR Service for connections |
| 0006 | Brand as configuration resolved by host |
| 0007 | Plugin contract versioning and in-process loading for v1 |
| 0008 | No MediatR, AutoMapper or FluentAssertions 8+ (licensing); Shouldly and NSubstitute for tests |
| 0009 | Azure SQL Database as the primary store |
| 0010 | Generic core: context-specific behavior lives only in domain packs and plugins |

---

## 3. Phases

Each phase lists its goal, tasks, deliverables and exit criteria. Tasks are written so that they can be handed to an AI coding agent one at a time.

### Phase 0 — Foundations (≈ 4 days)

**Goal:** a repository where any change can be built, tested and run locally with one command.

**Tasks**
- [ ] Create the repository structure (`src/`, `tests/`, `docs/`, `docs/adr/`, `docs/design/`, `.github/`).
- [ ] Add `global.json` pinning the SDK (`10.0.401`, `rollForward: latestFeature`).
- [ ] Create `Roster.slnx` and all projects listed in the project description (§13.3), with project references following the dependency rules.
- [ ] Add `Directory.Build.props`: `net10.0`, `LangVersion` latest, `Nullable` enable, `ImplicitUsings` enable, `TreatWarningsAsErrors` true, `AnalysisLevel` latest-recommended, deterministic builds.
- [ ] Add `Directory.Packages.props` (Central Package Management) with the latest stable versions of every package used.
- [ ] Add `.editorconfig` (file-scoped namespaces, `var` usage, naming rules, `using` placement) and `.gitattributes`.
- [ ] Set up `Roster.AppHost` with SQL Server, Azurite, a local mail catcher and the `Roster.Web` project.
- [ ] Set up `Roster.ServiceDefaults` (OpenTelemetry, health checks, HTTP resilience, service discovery).
- [ ] GitHub Actions: build, test, `dotnet format` check, vulnerable-package check (`dotnet list package --vulnerable`).
- [ ] Dependabot for NuGet and GitHub Actions (weekly).
- [ ] Add the AI agent files (`AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, `.github/copilot-instructions.md`, `.github/instructions/*`).
- [ ] Write ADRs 0001–0010.
- [ ] Send the design prompt and documentation to the design tool (see `design-prompt.md`); store the deliverables in `docs/design/`.

**Deliverables:** solution skeleton, CI pipeline, local run through Aspire, ADRs.

**Exit criteria**
- `dotnet build` and `dotnet test` succeed on a clean clone.
- Running the AppHost starts the web app, database, storage emulator and mail catcher, visible in the Aspire dashboard.
- CI is green on `main`.

---

### Phase 1 — Domain and persistence (≈ 6 days)

**Goal:** the core model and the scoring engine, fully tested, persisted with EF Core.

**Tasks**
- [x] Implement entities and value objects in `Roster.Domain`: `Game`, `Element`, `Rule`, `Participant`, `Lineup`, `ScoreEntry`, `Referee`, `SourceBinding`, `ConsentInvitation` (P1 entities `Report` and `SponsorBonus` stubbed).
- [x] Implement the game lifecycle (`Draft → Open → Live → Ended → Archived`) with guarded transitions.
- [x] Implement lineup rules: size, captain, uniqueness, consent and selectability checks, lock on `Live`.
- [x] Implement the **scoring engine** as a pure function: score entries + lineups + multiplier → ranked leaderboard (standard competition ranking).
- [x] Use `TimeProvider` for all time-dependent logic.
- [x] Unit tests for every rule, transition and scoring edge case (ties, voided entries, captain, personal bonuses, empty lineups).
- [x] `RosterDbContext` in `Roster.Infrastructure`: configurations, `RowVersion` concurrency tokens, unique indexes (nickname per game, external ID per binding, idempotency key per game).
- [x] First migration; migrations applied by a dedicated step in the AppHost for development.
- [x] Repository/query ports in `Roster.Application`; EF Core implementations in `Roster.Infrastructure` (no lazy loading, `AsNoTracking` for reads).
- [x] Integration tests against a real SQL Server (Aspire testing or Testcontainers), using fixtures from at least two contexts (people-based and team-based elements).

**Deliverables:** domain model, scoring engine, database schema.

**Exit criteria**
- Scoring engine coverage ≥ 95% of branches.
- Integration tests create a game, add elements and lineups, assign and void points, and read the expected leaderboard.

---

### Phase 2 — Plugin framework, CSV and Sessionize (≈ 5 days)

**Goal:** elements arrive from external sources and can be resynced safely.

**Tasks**
- [x] Define `Roster.Plugins.Abstractions`: `IElementSourcePlugin`, `PluginCapabilities`, `ConfigSchema`, `PluginConfig`, `ImportedElement`, `ImportResult`, `ValidationResult`; mark the assembly version as the contract version.
- [x] Plugin registry in `Roster.Infrastructure` (DI-based discovery, lookup by plugin ID).
- [x] **CSV plugin:** delimiter detection, header row, column mapping (name, subtitle, image URL, group, external ID), validation with line-level warnings, size limits.
- [x] **Sessionize plugin:** typed `HttpClient` with resilience; read `https://sessionize.com/api/v2/{endpointId}/view/All`; map speakers (and optionally sessions) to elements with stable external IDs; surface a clear error when the endpoint is not enabled. Never call undocumented endpoints.
- [x] Resync service: match by external ID, add/update/mark missing, never delete elements with lineups or points; produce a change summary.
- [x] Background job for scheduled resync (optional per binding).
- [x] **Plugin contract test suite** in `Roster.Plugins.Tests`, run against every plugin with recorded sample data (no live network in CI).
- [x] Domain packs: JSON schema and loader in `Roster.DomainPacks`; create the `generic` pack first (the foundation), then `tech-conference` (terminology EN/IT, default rules from the project description §6.1, recommended plugins). Add a test-only `amateur-tournament` pack (rules from §6.2) used by tests.
- [x] Make sure every context-specific UI word (element name in singular/plural, group label, consent wording) is defined by the pack, with sensible defaults in `generic`.

**Deliverables:** two working plugins, resync, two domain packs.

**Exit criteria**
- Importing the same source twice produces no duplicates.
- Changing an element name in the sample data (a speaker in Sessionize, a team in CSV) updates the element and keeps existing lineups.
- The CSV plugin imports both a guest list and a team list with no code changes.
- Contract tests pass for both plugins.

---

### Phase 3 — Organizer console (≈ 6 days)

**Goal:** an organizer can create, configure and publish a game from a laptop.

**Tasks**
- [x] Organizer authentication: ASP.NET Core Identity with **magic link** sign-in (email via Azure Communication Services; output to console in development); anti-enumeration and rate limiting.
- [x] Create-game wizard (labels come from the selected domain pack): choose domain pack → name and settings → connect a source (plugin form generated from `ConfigSchema`) → review imported elements → review and edit rules → publish.
- [x] Element management: edit, hide, mark as not selectable, show source state and consent status.
- [x] Consent flow: generate consent invitations (link per element, optional email), public consent page (accept/decline), status visible in the console.
- [x] Rule editor: add, edit, reorder, delete (only while `Draft`/`Open`); points must be non-zero.
- [x] Lifecycle controls: open joins, go live (locks lineups), end game, archive.
- [x] Join assets: join code, join URL, printable QR (PNG/SVG) and a printable poster.
- [x] Referee invitations: create referee links (scoping arrives in P1).
- [x] bUnit tests for the wizard and rule editor; integration tests for the endpoints.

**Deliverables:** organizer console (Interactive Server), magic-link sign-in, consent flow.

**Exit criteria**
- A game is created and published in **under 10 minutes** by someone who has never seen the app (hallway test), in **two scenarios**: a conference from the Sessionize sample and a tournament or party from a CSV file.

---

### Phase 4 — Participant PWA (≈ 6 days)

**Goal:** a participant joins in seconds and follows the game from a phone.

**Tasks**
- [ ] Join flow (Interactive WebAssembly): open link/QR → choose nickname (uniqueness check, profanity filter) → receive a signed participant token stored on the device.
- [ ] Session recovery on the same device; clear "this is your device" messaging.
- [ ] Lineup builder: browse elements (search, group filter), pick N, choose captain, save; read-only once `Live`.
- [ ] Leaderboard: my position pinned, top N, live updates, per-element breakdown of my score.
- [ ] Activity feed: latest points with rule labels.
- [ ] PWA: web manifest, icons per brand, service worker caching the shell; install prompt only after the first visit.
- [ ] Reconnection UX: connection status indicator, automatic resubscribe, refetch on version gap.
- [ ] Performance budget: first load on a mid-range phone over slow 3G under 5 s; subsequent loads under 2 s.
- [ ] Playwright E2E: join → pick lineup → see a point arrive, run with two domain packs (Generic and Tech conferences).
- [ ] Element cards work for people, teams and things (photo, crest or initials fallback).

**Deliverables:** participant experience, installable PWA.

**Exit criteria**
- A first-time user joins and saves a lineup in **under 60 seconds**.
- Killing and restoring the network does not lose the session or the leaderboard state.

---

### Phase 5 — Referee console and real time (≈ 6 days)

**Goal:** points flow from referees to every screen in under two seconds.

**Tasks**
- [ ] SignalR hub `/hubs/game`: group per game, server-to-client events only (`LeaderboardUpdated`, `ScoreEntryAdded`, `ScoreEntryVoided`, `GameStateChanged`), versioned payloads.
- [ ] Azure SignalR Service configuration for production; local SignalR in development.
- [ ] Score endpoints: assign (with idempotency key), void (with reason), list recent; authorization by referee token.
- [ ] Leaderboard recompute on every change and broadcast of the new version (single replica, in-memory cache per game rebuilt from the database on start).
- [ ] Referee console (Interactive WebAssembly): pick element → pick rule → confirm (**three taps**), undo within 10 seconds, recent history with void.
- [ ] One-handed layout: large targets, thumb zone, a low-light theme (dark halls, evening parties) and good daylight contrast (outdoor pitches).
- [ ] **Offline queue:** store pending assignments locally with their idempotency keys; replay on reconnect; show pending state.
- [ ] Rate limiting and audit log for referee actions.
- [ ] Load test (e.g. k6 or NBomber): 1,000 connected clients, 1 point every 5 seconds, p95 propagation < 2 s.

**Deliverables:** real-time pipeline, referee console, load-test report.

**Exit criteria**
- p95 propagation under 2 seconds at 1,000 clients.
- Replaying the offline queue twice produces no duplicate points.

---

### Phase 6 — Big screen (≈ 2 days)

**Goal:** a big-screen view (projector or TV) that keeps the audience engaged between moments: sessions, matches, courses, episodes.

**Tasks**
- [ ] Full-screen read-only view (Interactive WebAssembly): leaderboard, latest points ticker, join QR, game name and brand.
- [ ] Readability from 15 meters on a 1920×1080 projector; high contrast; safe areas for projector overscan.
- [ ] Subtle, single animation when ranks change; respects reduced motion.
- [ ] Optional "in between" rotation: leaderboard → latest points → join QR.
- [ ] Kiosk mode: no cursor, no navigation, auto-reconnect.

**Exit criteria**
- Readable at distance in a real room test; runs 8 hours without memory growth.

---

### Phase 7 — Branding, localization, hardening, deployment (≈ 6 days)

**Goal:** production-ready for one real event, under both brands.

**Tasks**
- [ ] Brand resolution middleware and `BrandOptions` (FantaRoster, ImagiRoster); theme token layers per brand; brand assets (logo, icons, social image).
- [ ] Complete EN and IT resources; culture selection (brand default → user choice); date/number formats.
- [ ] Import the tokens from `docs/design/tokens/` into `Roster.Ui` and apply the mockups in `docs/design/screens/` to every screen. *(Tokens, Login, Public site, Organizer Console Create Game Wizard, and Dashboard applied. Next: Remaining Organizer Console screens)*
- [ ] Security review: OWASP ASVS L1 checklist, rate limits, input validation, upload policies, signed tokens rotation, security headers, CSP compatible with Blazor.
- [ ] Privacy: privacy notice and terms (EN, IT), data retention job (photos 30 days after end), organizer data export and deletion.
- [ ] Accessibility audit (automated with axe in Playwright + manual pass) to WCAG 2.2 AA.
- [ ] Azure provisioning with `azd` from the Aspire app model: Container Apps, Azure SQL, Storage, SignalR Service, Key Vault, Communication Services, Application Insights.
- [ ] Custom domains and TLS for `fantaroster.com` and `imagiroster.com`.
- [ ] CD pipeline: deploy on tag to a `staging` environment, manual approval to `production`.
- [ ] Dashboards and alerts: join rate, hub connections, score latency, error rate.
- [ ] Backup and restore test for the database.

**Exit criteria**
- Both brands served from production with correct theme, language and assets.
- Accessibility and security checklists completed; no open high-severity issues.

---

### Phase 8 — Pilot rehearsal and event (≈ 2 days + event day)

The pilot is a tech conference because of access to organizers; it validates the generic engine in its first market.

**Tasks**
- [ ] Prepare the pilot game with the organizer: import from Sessionize, collect speaker consent, finalize rules.
- [ ] Print QR materials (badges, tables, posters) and prepare the big-screen device.
- [ ] Brief referees (10-minute walkthrough, one per room).
- [ ] Full rehearsal with 10–20 people on the production environment.
- [ ] Event day: on-call monitoring, live dashboard, fallback plan (CSV export of the leaderboard).
- [ ] Measure the metrics defined in the project description (§16).
- [ ] Post-event: organizer interview, feedback from people in play, retrospective, decisions for P1. Classify every request as core (generic) or domain pack/plugin.
- [ ] Plan the second real event in a different use case (tournament or party).

**Exit criteria**
- Pilot completed; metrics and feedback recorded in `docs/pilot-report.md`.

---

### Phase 9 — P1 release (≈ 25 days, after the pilot)

Prioritized by pilot feedback. Initial order:

| # | Feature | Estimate |
|---|---|---|
| 1 | Reports with photos (upload, moderation, approval → score entry) | 4 |
| 2 | Multiple referees with scope (elements or groups) | 2 |
| 3 | Outputs: social card image, CSV export, outgoing webhook | 3 |
| 4 | Sponsor bonus (QR, single claim, sponsor report, consent for contacts) | 3 |
| 5 | Duplicate edition | 1 |
| 6 | Google Sheets and ICS plugins | 3 |
| 7 | Second public domain pack (Amateur sports or Events and parties) and a second real event outside conferences | 2 |
| 8 | Payments for Premium and Pro plans | 5 |
| 9 | Passkeys for organizers | 2 |

### Phase 10 — P2 exploration

To be planned after P1: score plugins starting from the incoming webhook, third-party plugin loading and isolation, .NET MAUI app, public rulebook library, more languages, multi-replica scoring.

---

## 4. Testing strategy

| Level | Tooling | Scope |
|---|---|---|
| Unit | xUnit v3, Shouldly, NSubstitute | Domain rules, scoring engine, application services |
| Component | bUnit | Razor components in `Roster.Ui`, `Roster.Web`, `Roster.Web.Client` |
| Contract | xUnit v3 | Every plugin against the shared contract suite |
| Integration | Aspire testing / Testcontainers | API + database + SignalR end to end on the server |
| End to end | Playwright for .NET | Critical journeys: create game, join, lineup, score, void, big screen |
| Load | k6 or NBomber | Hub fan-out and score propagation |
| Accessibility | axe (via Playwright) + manual | Every screen |

Rules: no live external calls in CI (recorded fixtures); deterministic time through `TimeProvider`; tests run in parallel; integration and E2E journeys run with at least two domain packs.

---

## 5. Risks to the plan

| Risk | Mitigation |
|---|---|
| Design arrives late | UI built on tokens and unstyled components; styling applied in Phase 7 |
| Real-time performance below target | Load test early in Phase 5; fall back to throttled leaderboard broadcasts (e.g. every 500 ms) |
| PWA quirks in Blazor Web App | Spike in Phase 4 on day one; keep the participant shell minimal |
| Pilot date moves earlier | Cut Phase 6 extras and P1 items; P0 core (Phases 1–5, 7) is the minimum |
| Library breaking changes | Dependabot weekly, upgrade in small PRs, never mix upgrades with features |
| Core drifts toward conferences | Generic-first rule (§2.0), DoD check, tests with multiple domain packs, ADR 0010 |

---

## 6. Milestones

| Milestone | Definition |
|---|---|
| **M0 — Walking skeleton** | End of Phase 0: one command runs the whole system locally |
| **M1 — Engine** | End of Phase 2: games with imported elements and correct scoring, API only |
| **M2 — Playable** | End of Phase 5: organizer, participants and referees can play a full game locally, in at least two different use cases |
| **M3 — Production** | End of Phase 7: deployed under both brands, hardened |
| **M4 — Pilot** | End of Phase 8: real event completed and measured |
| **M5 — v1.1** | End of Phase 9: P1 features released |
