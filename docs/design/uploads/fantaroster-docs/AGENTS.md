# AGENTS.md — Shared instructions for AI coding agents

This file is the **single source of truth** for every AI coding agent working on this repository (Claude Code, Google Antigravity, GitHub Copilot). Tool-specific files only add tool-specific behavior.

## Project in one paragraph

A platform to create a **fantasy game about anything**. Organizers import "elements in play" through plugins, define bonuses and penalties; participants join with a nickname, build a lineup with a captain, and follow a live leaderboard while referees assign points. It ships under two brands from one codebase: **FantaRoster** (Italy) and **ImagiRoster** (international). The internal codename is **`Roster`**.

Read before any non-trivial task:
- `docs/project-description.md` — product, glossary, data model, architecture (source of truth)
- `docs/development-plan.md` — phases, tasks, Definition of Done
- `docs/adr/` — architecture decisions

## Non-negotiable rules

1. **English everywhere**: code, identifiers, comments, commit messages, docs, test names. User-facing text is never hard-coded: it lives in `.resx` resources for `en` and `it`.
2. **.NET 10, C# 14**, `net10.0` target. SDK pinned in `global.json`.
3. **Latest stable packages only**, managed centrally in `Directory.Packages.props`. Never put a `Version` attribute in a `.csproj`. Never add preview packages. Ask before adding any new package and state why.
4. **No brand in code.** Never hard-code "FantaRoster" or "ImagiRoster" in logic, namespaces or components. Brand values come from `BrandOptions`.
5. **Zero warnings.** Warnings are errors; do not suppress analyzers without a justification comment.
6. **Do not use** MediatR, AutoMapper, FluentAssertions 8+ or Moq. Use plain application services, explicit mapping, Shouldly and NSubstitute.
7. **Never call undocumented third-party endpoints** (e.g. Sessionize speaker emails).
8. **Respect the domain ethics**: no feature may rate the quality of people in play; consent is required before an element becomes selectable.

## Commands

```bash
dotnet restore
dotnet build                                   # must produce zero warnings
dotnet test                                    # all tests
dotnet test tests/<Project>                    # one test project
dotnet format --verify-no-changes              # formatting gate used by CI
dotnet run --project src/Roster.AppHost        # run everything locally (Aspire)
dotnet ef migrations add <Name> -p src/Roster.Infrastructure -s src/Roster.Web
dotnet list package --outdated                 # check for newer stable versions
dotnet list package --vulnerable               # security gate used by CI
```

## Repository map

| Path | Purpose |
|---|---|
| `src/Roster.AppHost` | Aspire app model |
| `src/Roster.ServiceDefaults` | OpenTelemetry, health checks, resilience |
| `src/Roster.Domain` | Entities, value objects, scoring engine. **No dependencies** |
| `src/Roster.Application` | Use cases, ports (interfaces), DTOs, authorization policies |
| `src/Roster.Infrastructure` | EF Core, Blob Storage, email, Key Vault, plugin host, background jobs |
| `src/Roster.Plugins.Abstractions` | Public plugin contracts (versioned) |
| `src/Roster.Plugins.*` | Plugin implementations; depend only on `Plugins.Abstractions` |
| `src/Roster.DomainPacks` | Domain pack JSON definitions and loader |
| `src/Roster.Ui` | Razor class library: design system components and tokens |
| `src/Roster.Web` | Blazor Web App host: endpoints, SignalR hubs, organizer console, branding |
| `src/Roster.Web.Client` | WebAssembly: participant PWA, referee console, big screen |
| `tests/*` | Test projects mirroring `src/` |

**Dependency direction:** `Domain` ← `Application` ← `Infrastructure` ← `Web`. Never reference `Infrastructure` or `Web` from `Domain` or `Application`.

## Glossary (code ↔ UI)

`Game` (UI: game / fanta) · `DomainPack` (domain / ambito) · `Element` (player in play: speaker, team…) · `Rule` (bonus / penalty) · `Participant` (player) · `Lineup` (roster / rosa) · `ScoreEntry` (point) · `Referee` · `Report` · `SourceBinding` · `SponsorBonus` · `Brand`.

Use `DomainPack`, never `Domain`, for the product concept (to avoid confusion with the domain layer).

## C# conventions

- File-scoped namespaces; one public type per file; file name = type name.
- Namespaces follow folders: `Roster.<Project>.<Folder>`.
- `sealed` by default for classes that are not designed for inheritance.
- `record` / `record struct` for DTOs and value objects; immutable where possible.
- Primary constructors for DI in services; keep them small.
- Async all the way: `Async` suffix, `CancellationToken` as the last parameter and passed through.
- Use `TimeProvider` for time, never `DateTime.Now`/`UtcNow` directly. Store times as `DateTimeOffset` in UTC.
- Money: `decimal` only.
- Guard clauses with `ArgumentNullException.ThrowIfNull` and friends.
- Return results for expected failures (validation, not found, conflict); throw only for exceptional situations.
- Logging with structured templates (`LoggerMessage` source generators for hot paths); never log tokens, emails or photos.
- XML doc comments on public APIs in `Roster.Plugins.Abstractions` and `Roster.Application` ports.

## Domain rules to preserve

- Game lifecycle: `Draft → Open → Live → Ended → Archived`; transitions only through domain methods.
- Lineups lock when the game goes `Live`.
- `Rule.Points` is a non-zero integer; `ScoreEntry` snapshots the points at creation.
- The leaderboard is **derived** from valid score entries; voiding marks an entry `Voided`, it never deletes it.
- Participant score = Σ(element score × captain multiplier if captain) + personal entries. Ranking: standard competition (1, 2, 2, 4).
- Elements with `ConsentStatus` other than accepted (when consent is required) are not selectable.
- Resync matches by `ExternalId`; never delete elements that have lineups or points (mark `MissingFromSource`).

## Data access (EF Core 10)

- `RosterDbContext` lives in `Roster.Infrastructure`; configurations in `IEntityTypeConfiguration<T>` classes.
- No lazy loading. Use `AsNoTracking()` for reads and projections to DTOs.
- Concurrency tokens (`RowVersion`) on aggregates edited concurrently.
- Unique indexes: nickname per game, external ID per source binding, idempotency key per game.
- Migrations: descriptive PascalCase names; never edit an applied migration.

## Web, Blazor and real time

- Render modes: static SSR for public pages; **Interactive Server** for the organizer console; **Interactive WebAssembly** for participant, referee and big-screen views.
- Components in `Roster.Ui` are brand-agnostic and use **CSS isolation** and **design tokens** (CSS custom properties). No inline styles, no hard-coded colors or sizes.
- Every user-facing string goes through `IStringLocalizer`. Allow 30% text expansion.
- Accessibility: WCAG 2.2 AA; semantic HTML; visible focus; labels on every input; touch targets ≥ 44×44 px; respect `prefers-reduced-motion`.
- **Commands go through HTTP endpoints; the SignalR hub only pushes** (`LeaderboardUpdated`, `ScoreEntryAdded`, `ScoreEntryVoided`, `GameStateChanged`). Payloads carry a version; clients refetch on gaps.
- Score assignment endpoints require an **idempotency key**.
- Minimal APIs grouped per feature (`MapGroup`), with built-in validation and typed results; produce OpenAPI metadata.

## Plugins

- Contracts live in `Roster.Plugins.Abstractions` and are versioned; breaking changes require an ADR.
- A plugin exposes an ID, display name, capabilities and a configuration schema; it never touches the database.
- External HTTP calls use typed `HttpClient` with the standard resilience handler.
- Every plugin must pass the shared contract test suite in `tests/Roster.Plugins.Tests` using recorded fixtures (no live network in tests).

## Testing

- xUnit v3, Shouldly, NSubstitute, bUnit, Playwright for .NET; Aspire testing or Testcontainers for integration.
- Test names: `Method_State_ExpectedResult` or plain-English sentences with underscores.
- Every bug fix starts with a failing test.
- Deterministic time via a fake `TimeProvider`; no sleeps.
- No real external services in CI.

## Security and privacy

- Participants are anonymous: nickname + signed token. Never ask for more personal data than a feature needs.
- Validate all input at the edge; rate-limit join, report and sponsor endpoints.
- Uploaded images: allow-list of types, size limit, re-encode, private blobs served through short-lived SAS URLs.
- Secrets only in user secrets (local) and Key Vault (Azure). Never commit secrets or connection strings.

## How to work

1. **Plan first** for any change touching more than one project: list the files you will change and the tests you will add, then implement.
2. Keep changes small and focused; one concern per pull request.
3. Run `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` before declaring a task done.
4. Update docs (`docs/*.md`, ADRs) in the same change when behavior or architecture changes.
5. When requirements are ambiguous, check `docs/project-description.md`; if still unclear, ask instead of guessing.
6. Commits and PR titles follow Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`).
