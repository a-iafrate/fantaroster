# FantaRoster · ImagiRoster

**Create a fantasy game about anything.** Pick what's in play, set bonuses and penalties, invite your group, and follow a live leaderboard while it happens.

| Brand | Market | Domain | Tagline |
|---|---|---|---|
| **FantaRoster** | Italy | [fantaroster.com](https://fantaroster.com) | Il fanta di qualsiasi cosa |
| **ImagiRoster** | International | [imagiroster.com](https://imagiroster.com) | Fantasy for anything |

One product, one codebase, two brands. The internal codename is **`Roster`**: the code never depends on a brand.

> **Status:** pre-development. Documentation and plan are ready; implementation starts with Phase 0 of the [development plan](docs/development-plan.md).

---

## What it does

- **Any context, curated domains.** Weddings, tournaments, TV show nights, conferences, school trips, office challenges: start from a domain pack with ready-made rules and terminology, or from the generic pack.
- **Import, don't type.** Source plugins bring in what's in play: CSV for anything, Sessionize for conferences, more to come.
- **Join with a nickname.** Participants scan a QR code and play. No account, no app store.
- **Rosters and captains.** Each player picks a roster and a captain who scores double.
- **Referees in three taps.** Volunteers assign bonuses and penalties from their phone, even with flaky Wi‑Fi.
- **Live everywhere.** Leaderboards update in real time on phones and on the big screen.
- **Respectful by design.** People in play give consent, and rules celebrate moments, never judge quality.

## Use cases

FantaRoster is a **generic tool**: the same engine runs any game where a group follows something together and scores what happens.

| Use case | What players pick | Example rule |
|---|---|---|
| Amateur tournament | Teams | Comeback win +8 |
| Wedding or party | Guests | Catches the bouquet +10 |
| TV show night | Contestants | Standing ovation +5 |
| Tech conference | Speakers | The demo fails and is rescued live +10 |
| Office challenge | Teams | Meeting ends early +5 |

Tech conferences are the **first market** (the pilot), chosen for access to organizers; they are one use case among many. See all [use cases](docs/project-description.md#6-use-cases) and the [generic-first principle](docs/project-description.md#11-core-principle-generic-first).

---

## Tech stack

| Area | Technology |
|---|---|
| Runtime | .NET 10 (LTS), C# 14 |
| Web | ASP.NET Core 10, Blazor Web App (SSR, Interactive Server, Interactive WebAssembly), Minimal APIs |
| Real time | SignalR, Azure SignalR Service |
| Data | EF Core 10, Azure SQL Database |
| Storage and email | Azure Blob Storage, Azure Communication Services Email |
| Orchestration | Aspire 13.x |
| Hosting | Azure Container Apps (provisioned with `azd`) |
| Observability | OpenTelemetry, Azure Monitor |
| Tests | xUnit v3, bUnit, Shouldly, NSubstitute, Playwright for .NET |
| CI/CD | GitHub Actions, Dependabot |

All packages use their **latest stable** versions through Central Package Management.

---

## Repository layout

```
.
├─ README.md                      This file
├─ AGENTS.md                      Shared instructions for AI coding agents
├─ CLAUDE.md                      Claude Code entry point (imports AGENTS.md)
├─ GEMINI.md                      Antigravity entry point (includes AGENTS.md)
├─ .github/
│  ├─ copilot-instructions.md     GitHub Copilot instructions
│  ├─ instructions/               Path-specific Copilot instructions
│  └─ workflows/                  CI/CD
├─ docs/
│  ├─ project-description.md      What and why (source of truth)
│  ├─ development-plan.md         How and when
│  ├─ design-prompt.md            Brief for the design work
│  ├─ design/                     UI mockups, design tokens and brand assets
│  └─ adr/                        Architecture Decision Records
├─ src/                           Application projects (see below)
├─ tests/                         Test projects
├─ Roster.slnx
├─ global.json
├─ Directory.Build.props
└─ Directory.Packages.props
```

### Projects

| Project | Responsibility |
|---|---|
| `Roster.AppHost` | Aspire app model: local orchestration and Azure deployment |
| `Roster.ServiceDefaults` | Telemetry, health checks, resilience |
| `Roster.Domain` | Entities, scoring engine, domain rules |
| `Roster.Application` | Use cases, ports, DTOs |
| `Roster.Infrastructure` | EF Core, storage, email, plugin host, jobs |
| `Roster.Plugins.Abstractions` | Public plugin contracts |
| `Roster.Plugins.Csv` / `.Sessionize` | Element source plugins |
| `Roster.DomainPacks` | Domain pack definitions and loader |
| `Roster.Ui` | Design system components |
| `Roster.Web` | Host, endpoints, hubs, organizer console, branding |
| `Roster.Web.Client` | Participant PWA, referee console, big screen (WebAssembly) |

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) — `10.0.401` or later (pinned in `global.json`)
- A container runtime (Docker Desktop or Podman) for SQL Server, Azurite and the local mail catcher
- Optional: the Aspire CLI, Visual Studio 2026 or VS Code with C# Dev Kit
- For end-to-end tests: Playwright browsers (`pwsh tests/Roster.E2E/bin/Debug/net10.0/playwright.ps1 install`)

### Run locally

```bash
git clone <repository-url>
cd <repository-folder>
dotnet restore
dotnet run --project src/Roster.AppHost
```

The Aspire dashboard opens with the web app, database, storage emulator and mail catcher. Magic-link emails for organizer sign-in appear in the mail catcher.

To test both brands locally, map two host names to `127.0.0.1` (e.g. `fantaroster.localhost` and `imagiroster.localhost`) or use the brand override setting in `appsettings.Development.json`.

### Test

```bash
dotnet test                                   # all tests
dotnet test tests/Roster.Domain.Tests         # one project
dotnet format --verify-no-changes             # formatting check (also in CI)
```

### Deploy

```bash
azd auth login
azd up            # provisions Azure resources from the Aspire app model and deploys
```

Production deployments run from GitHub Actions on tags, through a staging environment.

---

## Working with AI coding agents

This repository is set up for **Claude Code**, **Google Antigravity** and **GitHub Copilot**. All three read the same rules from [`AGENTS.md`](AGENTS.md):

| Tool | Entry point |
|---|---|
| Claude Code | `CLAUDE.md` (imports `AGENTS.md`) |
| Antigravity | `GEMINI.md` (includes `AGENTS.md`); `AGENTS.md` is also read natively |
| GitHub Copilot | `.github/copilot-instructions.md` and `.github/instructions/*.instructions.md` |

Change shared rules in `AGENTS.md` only; keep tool-specific files thin.

---

## Contributing

- Everything is written in **English**: code, comments, commits, docs. User-facing text is localized (EN, IT) through resources.
- Conventional Commits, short-lived branches, squash merges.
- Follow the Definition of Done in the [development plan](docs/development-plan.md#21-definition-of-done-applies-to-every-task).
- Significant decisions go into an ADR under `docs/adr/`.

## License

Proprietary. All rights reserved by IT-Impresa. (To be confirmed.)
