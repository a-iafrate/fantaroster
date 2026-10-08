# CLAUDE.md — Claude Code

Shared rules for every agent live in AGENTS.md and are imported here. Keep this file short: put shared rules in AGENTS.md, not here.

@AGENTS.md

## Project context

Read these when the task needs product or planning context:

- Product, glossary, data model, architecture: @docs/project-description.md
- Phases, tasks, Definition of Done: @docs/development-plan.md

For any UI task, open the relevant mockups in `docs/design/screens/` and the tokens in `docs/design/tokens/` before writing code. Read `docs/design-prompt.md` and the files in `docs/adr/` only when the task concerns UI design or an architectural decision.

## Claude Code workflow

- For tasks that touch more than one project, start in plan mode: list files to change and tests to add, wait for approval, then implement.
- The product is a generic tool (AGENTS.md rule 9): when planning, state how the change behaves in at least three use cases, and keep context-specific work in domain packs or plugins.
- Work on one development-plan task at a time and reference its phase and checkbox in the PR description.
- After every change run, in this order: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`. Fix all warnings; do not suppress them.
- Before adding a NuGet package, check the latest stable version with `dotnet list package --outdated` after adding it to `Directory.Packages.props`, and explain why it is needed.
- Never edit applied EF Core migrations; create a new one.
- When you change behavior or architecture, update the relevant doc or add an ADR in the same change.
- Personal, machine-specific notes go in `CLAUDE.local.md` (git-ignored), never in this file.
