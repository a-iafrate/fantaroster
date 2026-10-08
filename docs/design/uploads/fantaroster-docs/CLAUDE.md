# CLAUDE.md — Claude Code

Shared rules for every agent live in AGENTS.md and are imported here. Keep this file short: put shared rules in AGENTS.md, not here.

@AGENTS.md

## Project context

Read these when the task needs product or planning context:

- Product, glossary, data model, architecture: @docs/project-description.md
- Phases, tasks, Definition of Done: @docs/development-plan.md

Read `docs/design-prompt.md` and the files in `docs/adr/` only when the task concerns UI design or an architectural decision.

## Claude Code workflow

- For tasks that touch more than one project, start in plan mode: list files to change and tests to add, wait for approval, then implement.
- Work on one development-plan task at a time and reference its phase and checkbox in the PR description.
- After every change run, in this order: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`. Fix all warnings; do not suppress them.
- Before adding a NuGet package, check the latest stable version with `dotnet list package --outdated` after adding it to `Directory.Packages.props`, and explain why it is needed.
- Never edit applied EF Core migrations; create a new one.
- When you change behavior or architecture, update the relevant doc or add an ADR in the same change.
- Personal, machine-specific notes go in `CLAUDE.local.md` (git-ignored), never in this file.
