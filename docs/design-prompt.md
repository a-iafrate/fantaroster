# Design Prompt — FantaRoster / ImagiRoster

> **How to use this file:** paste everything below the line into the design tool, and attach these files as context:
> - `README.md` (repository root)
> - `docs/project-description.md` (product, users, requirements, glossary, use cases)
> - `docs/development-plan.md` (render modes, phases, constraints)
>
> The design output is stored in **`docs/design/`** (structure described in `docs/design/README.md`) and feeds the `Roster.Ui` component library and Phase 7 of the development plan.

---

## Your role

You are the design lead for a new product. You will create the **visual identity, design system and key screens** for a live, social fantasy game platform that ships under **two brands from one codebase**. The client has rejected proposals that felt templated before: make deliberate, opinionated choices that come from this product's world, and spend your boldness in one place.

Read the attached documents first. `project-description.md` is the source of truth for features, users and terminology. If anything below conflicts with it, ask before deciding.

## The product in one paragraph

Anyone can create a **fantasy game about anything**: a wedding, an amateur football tournament, a TV show night, a tech conference, a school trip, a family Christmas. The organizer chooses what is "in play" (guests, teams, contestants, speakers, even songs or dishes), imports them from external sources, and sets **bonuses and penalties**. Participants join with a QR code and a nickname (no account), build a **roster** of picks with one **captain** (double points), and watch a **live leaderboard** while referees assign points as things happen. One organizer or sponsor pays; the whole group plays for free.

## Generic by design (most important constraint)

This is a **generic tool**. Tech conferences are only the first market, chosen for go-to-market reasons; they must not shape the identity or the components. The design system must work equally well for a wedding, a five-a-side tournament, a TV show night and a conference.

- **No context in the identity.** Logos, palettes, illustrations and patterns must not reference conferences, technology, football or any single use case.
- **Context comes from the domain pack.** Words like "speaker", "team", "guest", "room" or "pitch" are supplied by the active domain pack. Design labels as slots (e.g. "Pick your {elements}", "{Group}: Room A / Pitch 2") and show them filled in with at least two different contexts.
- **Elements are not always people.** An element can be a person, a team, a song, a dish. The element card must handle a photo, a crest or logo, or no image at all (initials or a generated pattern).
- **Neutral defaults.** When no domain pack applies, the Generic pack uses plain words ("pick", "player", "group").

## The two brands

| | **FantaRoster** | **ImagiRoster** |
|---|---|---|
| Market | Italy (launch) | International |
| Default language | Italian | English |
| Tagline | Il fanta di qualsiasi cosa | Fantasy for anything |
| Cultural root | Italian "fanta" culture: fantacalcio with friends, FantaSanremo, banter, stadium chants, the group chat that never sleeps | Imagination: building a team that does not exist, "what if" games among friends and colleagues |

**Requirement:** one shared design system (structure, components, spacing, type scale, interaction patterns) with a **brand layer** on top (colors, logo, illustration accents, tone of voice). Switching brand must only swap tokens and assets, never layouts or components. Both brands must feel like siblings, not clones, and neither may look like the Coca-Cola "Fanta" brand.

## Where the product is used (design for these conditions)

| Surface | Who | Device and conditions | What matters most |
|---|---|---|---|
| **Participant app** (PWA) | Attendees, guests, friends | Phone, one hand, crowded and noisy venue, bright daylight or dark hall, unstable connection | Join in seconds; "where am I in the ranking?" at a glance; delight when points arrive |
| **Referee console** | Volunteers, organizers | Phone, one hand, during a talk in a dark hall, at the side of a pitch in daylight, at a party table; must not distract others | Assign a point in **three taps**; undo; never assign by mistake; low-glare theme for dark rooms and strong contrast for daylight |
| **Organizer console** | Organizers | Laptop before the event, phone during it | Fast setup through a wizard; clarity and control; data-dense but calm |
| **Big screen** | The whole audience | Projector or TV (conference hall, bar during a tournament, living room at a party), 1920×1080, read from 15 meters, often washed out by ambient light | Legibility from distance; a single memorable moment when ranks change |
| **Public pages** | Visitors, people in play | Any device | Explain the product in ten seconds; consent page that feels respectful |

## Scope of work

### 1. Identity (both brands)
- Logo concepts for **FantaRoster** and **ImagiRoster** (wordmark plus a symbol that also works as an app icon at 48 px and as a favicon).
- Color palette per brand (4–6 named colors each), plus shared neutrals and semantic colors (success, warning, danger, info) for both light and dark themes.
- Typography: one or two families shared by both brands, chosen deliberately (not the default families of every web app), with a clear type scale that also works at big-screen sizes. Must support Italian and English characters and be licensable for web use (e.g. open-source fonts).
- Tone of voice for each brand, with 10 sample strings (buttons, empty states, errors, celebration messages) in both English and Italian.

### 2. Design system (shared)
Deliver as **design tokens** plus component specs:
- Tokens: color (base + brand layers + light/dark), typography, spacing, radius, elevation, motion durations and easings, z-index, breakpoints.
- Components with all states (default, hover, focus-visible, active, disabled, loading, error):
  - Buttons (primary, secondary, ghost, destructive), icon buttons, segmented control
  - Text input, search, select, file drop zone (CSV), toggle, checkbox, radio
  - Element card (photo or initials avatar, name, subtitle, group, selected/captain/locked/not-selectable states)
  - Captain badge, points chip (+/−), rule chip
  - Leaderboard row (rank, rank change, nickname, points, "you" highlight) and leaderboard list
  - Activity feed item (point assigned, point voided)
  - Connection status indicator (online, reconnecting, offline with pending items)
  - Toasts, dialogs, bottom sheets, empty states, skeleton loaders
  - Stepper (organizer wizard), tabs, tables (organizer), QR display block
  - Consent card (accept/decline)

### 3. Key screens
Mobile at **390×844**, desktop at **1440×900**, big screen at **1920×1080**. Show both brands for at least screens 1, 4 and 9; the rest in one brand is fine.

Show screens 2, 4 and 15 in **two different use cases** (for example a five-a-side tournament with team crests and a tech conference with speaker photos) to prove that the same components serve both. Use a third context (e.g. a wedding) for at least one more screen.

**Participant (mobile)**
1. Join: game name, organizer, nickname field, join button (from a QR scan).
2. Build your roster: element list with search and group filter, picks counter (e.g. 2/3), captain selection, save.
3. Roster locked / waiting for the game to start.
4. **Live leaderboard**: my position pinned, top players, rank changes, my score breakdown per pick.
5. Activity feed: latest points with rule labels.
6. Point received: the celebration moment when one of my picks scores (and the quieter version for penalties).
7. Final results and shareable card (social image 1200×630 and story 1080×1920).

**Referee (mobile, low-light and daylight themes)**
8. Assign a point: choose element → choose rule → confirm, with undo for 10 seconds.
9. Recent points with void action; offline state with pending items.

**Organizer (desktop, with mobile variants for 13–14)**
10. Create game wizard: choose domain (Generic, Tech conferences, …) → settings → connect source (CSV, Sessionize, …) → review imported elements → rules → publish.
11. Elements management with source state and consent status.
12. Rule editor (bonuses and penalties).
13. Game control: open joins, go live, end game; live stats (players joined, points assigned).
14. Join materials: QR, link, printable A4 poster and badge sticker.

**Big screen**
15. Leaderboard with latest points ticker and join QR; rank-change moment.
16. "Join now" screen shown between sessions.

**Public**
17. Landing page per brand (hero, how it works in three steps, use cases showing the breadth of contexts, pricing teaser). The hero must not suggest the product is for conferences only.
18. Consent page for people in play (a guest, a player, a speaker, a team captain): what it means, accept or decline.

**System states** for all relevant screens: empty, loading, error, offline, game not started, game ended.

## Content to use

Use real content from `project-description.md` §6 (use cases), never lorem ipsum. Use **at least three contexts** across the screens:
- **Amateur five-a-side tournament** (§6.2): 12 teams with crests or initials, pitches as groups, rules such as "Goal +3", "Comeback win +8", "Yellow card −2".
- **Tech conference** (§6.1): speakers with photos, rooms as groups, rules such as "The demo fails and the speaker rescues it live +10", "It works on my machine +5", "Runs over by more than 5 minutes −3".
- **Wedding or party** (§6 table): guests, rules such as "Catches the bouquet +10", "First on the dance floor +5".
- Nicknames should feel real and varied (e.g. "LaPrimaFila", "async_awaiter", "Zia Carla", "DemoGodz").

## Constraints from the implementation

- The UI is built with **Blazor (.NET 10)** as a custom Razor component library with **CSS isolation** and **CSS custom properties** for tokens. No third-party UI kit: every component must be buildable with semantic HTML and CSS.
- Tokens must be delivered so they can become **CSS custom properties**: a shared base layer, one layer per brand, light and dark variants (e.g. `--color-surface`, `--color-brand-primary`, `--space-4`, `--radius-m`).
- Icons: a single open-source icon set, used consistently; specify which one.
- Element images come from external sources (a person's photo, a team crest, an artwork) and may be missing or low quality: design a robust fallback (initials, generated pattern) that works for people and for things.
- Element names can be long (Italian surnames, session titles): show truncation and wrapping rules.
- All user-facing text is localized (EN and IT): allow at least 30% text expansion.

## Quality bar

- **Accessibility:** WCAG 2.2 AA contrast for text and UI, visible focus, touch targets at least 44×44 px, no information conveyed by color alone (rank changes need shape or text too), respects `prefers-reduced-motion`.
- **Motion:** one orchestrated moment per surface at most (the point-received celebration on mobile, the rank change on the big screen). Everything else responds to user actions only.
- **Legibility in hostile conditions:** daylight on phones and outdoor pitches, dark halls for referees, washed-out projectors and TVs.

## Avoid

- Generic defaults that would fit any app: warm cream backgrounds with a serif display and terracotta accent; near-black with a single acid-green accent; identical rounded cards with the same soft shadow everywhere; gradient washes as decoration.
- Template chrome: tracked-out all-caps labels above every heading, meta strings joined with middle dots, arrows appended to every link, monospace for small labels.
- Sports-betting aesthetics (odds, slot-machine effects, casino colors): this is a friendly game, never gambling.
- Visual references to a single context (circuit boards, code brackets, footballs, wedding rings) in the shared identity: the product is generic.
- Anything resembling Coca-Cola's Fanta (orange soft-drink colors, bubbly logotype).
- Copy that apologizes or sells; prefer plain verbs and sentence case. A button says exactly what happens ("Save roster", then the toast says "Roster saved").

## Deliverables and format

All deliverables will be stored in the repository under `docs/design/`, following the structure in `docs/design/README.md` (tokens, components, screens per surface, assets). Name files in English, lowercase, with hyphens.

1. A short **design rationale** (one page): the concept, why it fits these two brands and their users, and the one memorable element.
2. **Token files**: `tokens.base.json`, `tokens.fantaroster.json`, `tokens.imagiroster.json` (light and dark), plus the same as CSS custom properties.
3. **Component specs** with states, sizes and spacing.
4. **Screen designs** listed above, with annotations for behavior and responsive rules.
5. **Logo and icon assets** in SVG (and PNG for app icons: 48, 192, 512 px, maskable variant).
6. **Social card templates** (1200×630, 1080×1920) per brand.
7. A list of **open questions** you need answered.

Work in two passes: first propose the concept and token plan (palette, type, layout principles) for both brands and review it against this brief; then produce the full system and screens.
