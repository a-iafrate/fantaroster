# GEMINI.md — Google Antigravity

Antigravity reads `AGENTS.md` from the workspace root natively: that file holds all shared rules for this repository and must be followed in full. This file only adds Antigravity-specific behavior. Keep it short; put shared rules in `AGENTS.md`.

If a rule here conflicts with `AGENTS.md`, treat it as a mistake and ask, unless it is explicitly about Antigravity behavior.

## Project context

- Product, glossary, data model, architecture: `docs/project-description.md`
- Phases, tasks, Definition of Done: `docs/development-plan.md`
- Design brief: `docs/design-prompt.md`
- Architecture decisions: `docs/adr/`

Open these files before planning any non-trivial task.

## Antigravity workflow

- **Plan before acting.** For any task touching more than one project, produce an implementation plan first: goal, files to change, tests to add, risks. Wait for approval before editing code.
- **One task at a time.** Map each task to a checkbox in `docs/development-plan.md` and name it in the plan.
- **Verify, then report.** After implementing, run `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes`, and summarize the results in a short walkthrough: what changed, how it was verified, what is left.
- **UI verification.** When a browser is available to the agent, start the app with `dotnet run --project src/Roster.AppHost` and check UI changes on a 390 px mobile viewport and a desktop viewport, in both brands (FantaRoster and ImagiRoster) and both languages (English and Italian). Check keyboard focus and contrast.
- **Terminal safety.** Never run commands that delete data, reset the database outside the local Aspire containers, push to remote branches or deploy (`azd up`, `azd deploy`) without explicit approval.
- **Parallel agents.** When several agents work at the same time, give each one a separate branch and a separate development-plan task; never let two agents edit the same project at once.
