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

## Referee console (October 2026)

- Implemented from `03` screens 08 and 09 and `06` section 4: element, rule, confirm in three taps; undo for 10 seconds; recent points with an offline queue; menu with recent points, rules, theme, help and exit. Dark by default, switchable.
- The invite is a link: `/games/{id}/referee#t={token}`. The token is in the URL fragment, so it never reaches the server or its logs; the console stores it on the device and removes it from the address bar. `GET /api/games/{id}/referee/me` validates it.
- Only network errors and server errors queue a point offline. A refusal (game not live, unknown rule) is shown and never queued; an invalid token sends the referee back to "you need an invite link".
- Not built: the scope of a referee (a room or a pitch) is P1, so the header shows "Step N of 3" without a group name; the "on stage" badge needs a program source.

## Organizer console (October 2026)

- Shell from `06` section 2: account level (my games) and game level (overview, elements, rules, referees, control, materials, settings). Sidebar from 1024 px, top bar and drawer on phones. The shell is an interactive island and never queries the database: pages publish the game through `OrganizerContext`, because the shell and the page would otherwise share one DbContext concurrently.
- Consent lives inside Elements (a column, a filter and a row action), as in the mockup; the separate consent tab is gone.
- Not built, recorded here: game switcher in the sidebar, "Templates" and "Plan and invoices" (P1), duplicate edition (P1), rule limits such as "once per session" (needs a domain concept), drag and drop ordering (up and down buttons instead), passkeys (P1).
- Rows are cards on phones and a table-like list from 1024 px. Editing opens a bottom sheet on phones and a dialog from 640 px.
- A QR code keeps a fixed dark-on-white contrast in every theme (`.ds-qr`): it is the one place where colors do not follow the theme, because phones cannot scan a light-on-dark code.

## Big screen (October 2026)

- Designed at 1920x1080 and scaled with one CSS unit, so every 16:9 display shows the same layout; other ratios are letterboxed with the stage color. Minimum text is 32 design pixels (avatar initials are decorative and smaller, as in the mockup).
- Rows are 88 px with a 100 px pitch so eight rows fit between header and ticker (the mockup's 96/108 does not fit). The ticker is static and shows the latest three points; the mockup's 60 px/s scrolling needs a per-item width and is deferred.
- Modes: leaderboard, join now, automatic (20 s and 10 s), with pause and full screen. Controls appear with the mouse or a key and hide after 3 s. Before the game starts the join screen is shown.
- The "static mode" organizer setting of the mockup is not built; reduced motion removes the animations.
- The screen reads the ranking from the new public endpoint `GET /api/games/{id}/leaderboard` (the old screen called the participant endpoint without a token and showed nothing).

## Public site, sign in and consent (October 2026)

- The landing lists the use cases from the domain packs (name, description and an optional `icon` in the pack JSON), so a new pack appears without touching the page. The mockup's pricing section is not built: prices are placeholders to confirm.
- The brand wordmark splits the configured brand name at its second capital letter; nothing in the pages names a brand.
- Sign in: the passkey button is removed (P1), as are the terms and privacy links (no such pages yet). The consent page is now interactive (its buttons did nothing before) and shows what the choice means and two example rules; changing an answer is done through the organizer.
- Plugin configuration labels and descriptions (for example the CSV fields) are English-only text from the plugin schema: localizing them needs a change to the plugin contract (ADR).
