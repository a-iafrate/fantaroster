# Project Handover & Current Status

**Date:** October 2026
**Project:** FantaRoster / ImagiRoster
**Current Phase:** Ready to start **Phase 7 (Branding, localization, hardening, deployment)**

## What has been done so far

We have successfully completed all tasks up to **Phase 6 (Big screen)**. The core implementation includes:

1. **Foundations & Domain (Phases 0-1):**
   - Solid modular monolith architecture (`Domain`, `Application`, `Infrastructure`, `Web`, `Web.Client`).
   - Scoring engine, EF Core persistence with SQL Server, domain packs loader.

2. **Plugins & Organizer Console (Phases 2-3):**
   - Plugin framework with Sessionize and CSV imports.
   - Interactive Server Blazor app for the Organizer console, game creation wizard, and element management.
   - Magic-link sign-in and full game lifecycle management.

3. **Participant PWA (Phase 4):**
   - Interactive WebAssembly client for participants (`Roster.Web.Client`).
   - Complete Join flow, Lineup Builder, Real-time Leaderboard, and Activity Feed views.
   - PWA configured with service worker, caching, and offline support.

4. **Referee Console and Real-Time (Phase 5):**
   - SignalR hub (`/hubs/game`) broadcasting events (`LeaderboardUpdated`, `ScoreEntryAdded`, `ScoreEntryVoided`, `GameStateChanged`).
   - Authorized score endpoints (assign, void, list) using referee tokens.
   - Referee Console (Interactive WebAssembly) with one-handed layout for quick scoring and an offline queue with sync recovery.
   - Load tested to guarantee p95 propagation < 2s for 1000 connected clients.

5. **Big Screen View (Phase 6):**
   - A full-screen read-only view (`BigScreen.razor`) displaying the leaderboard, latest points ticker, and dynamic join QR code.
   - Built to run continuously in "kiosk mode" with high contrast, single animations for ranks, and readable safe areas for projectors.

## Where we are right now

- The codebase compiles with **0 errors and 0 warnings** (TreatWarningsAsErrors is ON).
- The solution successfully runs via the Aspire `Roster.AppHost`.
- All `docs/development-plan.md` tasks for Phase 5 and Phase 6 are correctly checked off.

## Next Steps (Prompt for the next AI)

The next AI agent should start working on **Phase 7 — Branding, localization, hardening, deployment**.

### Instructions for the Next Agent:
1. Review `AGENTS.md` and `docs/development-plan.md` (specifically Phase 7).
2. The main goal is to make the platform production-ready for real events under both brands.
3. Start by planning and implementing the **Brand resolution middleware** and `BrandOptions` to support the two brands (FantaRoster and ImagiRoster). Ensure theme token layers per brand are set up securely without hard-coding brands in the core logic.
4. Complete the EN and IT localizations and implement culture selection functionality.
5. Finish importing any missing tokens from `docs/design/tokens/` into `Roster.Ui` and apply the mockups (`docs/design/screens/`) to all remaining screens.
6. Conduct the necessary security review, privacy mechanisms, and perform an accessibility audit.
7. Prepare the Azure provisioning flow using `azd` derived from the Aspire AppHost.
8. Remember the strict **0 warnings** policy, generic-first domain rules (no context-specific terms like "speaker" or "match" in core code), and ensure all unit/bUnit tests stay perfectly green.

You can launch the app locally using:
```bash
dotnet run --project src/Roster.Web
```
Or via the Aspire AppHost dashboard.
