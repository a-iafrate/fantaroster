# GitHub Copilot instructions

The full shared rules for AI agents are in `AGENTS.md` at the repository root. Product context is in `docs/project-description.md`; phases and the Definition of Done are in `docs/development-plan.md`. Path-specific rules are in `.github/instructions/`.

## Project

A platform to create a fantasy game about anything, shipped under two brands from one codebase: **FantaRoster** (Italy) and **ImagiRoster** (international). Internal codename: **`Roster`**. Organizers import elements in play through plugins and define bonuses and penalties; participants join with a nickname, build a lineup with a captain, and follow a live leaderboard while referees assign points.

## Always

- Write everything in **English**: code, identifiers, comments, commit messages, docs. User-facing text goes into `.resx` resources (`en`, `it`), never hard-coded.
- Target **.NET 10** (`net10.0`) and C# 14.
- Use the **latest stable** NuGet packages through Central Package Management (`Directory.Packages.props`). Never add `Version` attributes in `.csproj` files. No preview packages.
- Never hard-code a brand name; read brand values from `BrandOptions`.
- Keep the build at **zero warnings**.
- Follow the layering: `Domain` ← `Application` ← `Infrastructure` ← `Web`. Plugins depend only on `Roster.Plugins.Abstractions`.
- Use `TimeProvider` for time, `DateTimeOffset` in UTC, `decimal` for money, `CancellationToken` on every async method.

## Never

- MediatR, AutoMapper, FluentAssertions 8+ or Moq. Use plain services, explicit mapping, Shouldly and NSubstitute.
- Undocumented third-party endpoints.
- Features that rate the quality of people in play, or that skip their consent.
- Secrets or connection strings in code or config files.

## Glossary

`Game` (fanta) · `DomainPack` (domain/ambito; never call it `Domain`) · `Element` (player in play) · `Rule` (bonus/penalty) · `Participant` (player) · `Lineup` (roster/rosa) · `ScoreEntry` (point) · `Referee` · `Report` · `SourceBinding` · `SponsorBonus` · `Brand`.

## Before finishing a change

Run `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes`. Update docs or add an ADR in `docs/adr/` when behavior or architecture changes. Use Conventional Commits for commit messages.
