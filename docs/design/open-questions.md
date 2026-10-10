# Design open questions and decisions

Decisions taken while implementing the mockups. When a mockup and the documentation disagree, the documentation wins on behavior and the mockup wins on visuals.

## Theme selection (October 2026)

**Question.** Mockup 06 shows a "Theme" control in the public header and footer, and in the participant, referee and organizer menus, with the values System, Light and Dark. How is the preference stored and which theme does each surface start with?

**Decision.**
- The preference is per device (`localStorage`), applied by `wwwroot/js/theme.js` before first paint. It works in every render mode, including static SSR pages.
- Each tap on a theme control cycles System → Light → Dark. The control shows the current value (the public header chip shows the value instead of the static word "Theme" of the mockup, so the tap gives visible feedback).
- Every surface has its own layout and declares its theme behavior on its root element:

| Surface | Layout | Default | Stored as |
|---|---|---|---|
| Public site, login, organizer console | `MainLayout`, `EmptyLayout`, `OrganizerLayout` | System | `roster-theme` |
| Participant | `ParticipantLayout` | System | `roster-theme` |
| Referee | `RefereeLayout` | Dark (dark halls), switchable | `roster-theme.referee` |
| Big screen | `StageLayout` | Always `stage`, not switchable | — |

- Bootstrap is still loaded for layout utilities while screens move to the design system. Its variables are mapped to the tokens in `wwwroot/app.css`, so remaining Bootstrap components follow the active theme.

## Participant surface (October 2026)

**Structure.** Implemented from `02 Partecipante` and `06 Navigazione` (section 3): app bar with game name, tab title and avatar; four fixed bottom tabs on phones (Leaderboard, Roster, Points, Rules) that become top tabs from 1024 px; avatar menu as a bottom sheet (dropdown on desktop). The shell is `ParticipantShell` in `Roster.Web.Client`.

**Decisions.**
- Before the game goes live only Roster and Rules are shown (as in the mockup); the leaderboard and points tabs appear when the game is live.
- The tab and screen named "Rosa" in Italian is "Roster" in English (the glossary term for a lineup).
- Words that depend on the context (what elements and groups are called) come from the game's domain pack through `GET /api/games/{id}` (`terms`, resolved for the request culture). Core resources never contain them. Neutral placeholders are used in core copy.
- The mockup's "notifications" menu item and the "change nickname" item are not built yet: they need a push channel and a rename endpoint. Recorded here, not in the menu.
- Share card and "save story" on the final screen are P1 (Outputs) and are not built.
- A point that concerns the participant (one of their picks, or a personal bonus) opens a sheet for a bonus and a 4-second banner for a penalty, with no sound or vibration, as in screen 06. Reduced motion removes the animations.
- Participant state lives in one `ParticipantSession` service shared by all tabs and kept current by the game hub; after a reconnection it rejoins the game group and refetches.

## Visual test tooling

`tests/Roster.E2E` (Playwright for .NET) starts the web app against a dedicated LocalDB database (`ROSTER_E2E_CONNECTION_STRING` overrides it) and runs the journeys with two domain packs. Set `ROSTER_E2E_SCREENSHOTS` to a folder to save a screenshot of each step for visual review against the mockups.
