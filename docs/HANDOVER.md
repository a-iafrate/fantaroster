# Project Handover & Current Status

**Date:** October 2026  
**Project:** FantaRoster / ImagiRoster  
**Current Phase:** Ready to start **Phase 5 (Referee console and real time)**

## What has been done so far

We have successfully completed all tasks up to **Phase 4 (Participant PWA)**. The core implementation includes:

1. **Foundations & Domain (Phases 0-1):** 
   - Solid modular monolith architecture (`Domain`, `Application`, `Infrastructure`, `Web`, `Web.Client`).
   - Scoring engine, EF Core persistence with SQL Server, domain packs loader.

2. **Plugins & Organizer Console (Phases 2-3):**
   - Plugin framework with Sessionize and CSV imports.
   - Interactive Server Blazor app for the Organizer console, game creation wizard, and element management.
   - Magic-link sign-in and full game lifecycle management (Draft -> Open -> Live -> Ended).

3. **Participant PWA (Phase 4):**
   - **Interactive WebAssembly** client for participants (`Roster.Web.Client`).
   - Join flow with nickname selection (saving token on device via `ParticipantAuthState`).
   - Dedicated `/join/{joinCode}` endpoint doing automatic redirect to `/games/{GameId}/join`.
   - Complete Lineup Builder using the generic `ElementCard` (which handles fallback images).
   - Real-time ready Leaderboard and Activity Feed views (currently pulling via REST, ready to be hooked to SignalR in Phase 5).
   - **PWA Setup:** Configured dynamic `manifest.webmanifest` generated via `PwaEndpoints`, service worker (`service-worker.js`), icons (`favicon.svg`, `favicon.png`), and a JS-interop-based `ConnectionStatus` UI indicator for offline scenarios.

## Where we are right now

- The code compiles with **0 errors and 0 warnings** (TreatWarningsAsErrors is ON).
- The solution successfully runs via the Aspire `Roster.AppHost`.
- All `docs/development-plan.md` tasks for Phase 4 are checked off.

## Next Steps (Prompt for the next AI)

The next AI agent should start working on **Phase 5 — Referee console and real time**. 

### Instructions for the Next Agent:
1. Review `AGENTS.md` and `docs/development-plan.md` (specifically Phase 5).
2. The main goal is to flow points from referees to every screen in under two seconds.
3. Start by planning and implementing the **SignalR hub (`/hubs/game`)** in `Roster.Web` to push events (`LeaderboardUpdated`, `ScoreEntryAdded`, `ScoreEntryVoided`, `GameStateChanged`).
4. Implement the score endpoints (assign, void, list recent) with authorization via a referee token.
5. Create the **Referee Console** (Interactive WebAssembly) focusing on a one-handed layout (three taps to assign a score).
6. Implement the **Offline Queue** for the referee console using local storage/indexedDB to replay pending assignments on reconnect.
7. Remember the **0 warnings** policy, generic-first domain rules (no context-specific names in core code), and ensure all tests stay green.

You can launch the app locally using:
```bash
dotnet run --project src/Roster.Web
```
Or via Aspire AppHost.
