# Project Description — FantaRoster / ImagiRoster

> **Status:** validated on paper, to be proven with a pilot event.
> **Version:** 1.0 — October 2026.
> **Brands:** **FantaRoster** (Italy, `fantaroster.com`) and **ImagiRoster** (international, `imagiroster.com`). One product, one codebase, two brands.
> **Internal codename:** `Roster` (used for the solution, namespaces and infrastructure, so the code never depends on a brand).

This document is the **source of truth** for what the product is and why it exists. The development plan (`development-plan.md`) describes *how* and *when* it gets built; the design prompt (`design-prompt.md`) describes what the design work must produce.

---

## Table of contents

1. [Summary](#1-summary) — including the [generic-first principle](#11-core-principle-generic-first)
2. [Origin of the idea](#2-origin-of-the-idea)
3. [The problem](#3-the-problem)
4. [Market and competition](#4-market-and-competition)
5. [Key concepts and glossary](#5-key-concepts-and-glossary)
6. [Use cases](#6-use-cases)
7. [Goals and non-goals](#7-goals-and-non-goals)
8. [Personas and user stories](#8-personas-and-user-stories)
9. [Functional requirements](#9-functional-requirements)
10. [Game rules and ethics](#10-game-rules-and-ethics)
11. [Business model](#11-business-model)
12. [Naming and brand](#12-naming-and-brand)
13. [Technical architecture](#13-technical-architecture)
14. [Non-functional requirements](#14-non-functional-requirements)
15. [Risks and mitigations](#15-risks-and-mitigations)
16. [Validation plan](#16-validation-plan)
17. [Open questions](#17-open-questions)
18. [References](#18-references)

---

## 1. Summary

FantaRoster is a **general-purpose platform to create a fantasy game about anything**: a conference, a party, an amateur tournament, a TV show, a school trip, a day at the office. The organizer chooses what is "in play", defines bonuses and penalties, and invites the group. Each participant builds a **roster** of picks, and a live leaderboard follows what actually happens.

Two things set it apart:

1. **Domains.** Ready-made packages for specific contexts, with rules, terminology, templates and data sources already configured. Tech conferences will be the **first domain taken to market**, because the founder (a technical speaker and long-time Microsoft MVP) has direct access to organizers. This is a go-to-market choice, **not a product boundary**: conferences are one use case among many.
2. **Source plugins.** Elements in play are not typed by hand: they come from external sources through plugins (CSV and spreadsheets for anything, calendars, Sessionize for conferences, more later). The same mechanism will later deliver automatic scoring.

The revenue model follows one simple shape: **one organizer (or a sponsor) pays, the whole group plays for free.**

### 1.1 Core principle: generic first

FantaRoster is a **generic tool**. Every part of the product, from the data model to the UI copy, must work for any context: a conference, a wedding, a five-a-side tournament, a TV show night, a school trip, a family Christmas. This principle applies to the whole development, not only to the first release.

What this means in practice:

- **The core knows no context.** `Roster.Domain`, `Roster.Application`, `Roster.Web`, `Roster.Web.Client` and `Roster.Ui` contain no logic, names, copy or assumptions tied to a specific use case. Words such as "speaker", "session", "talk", "team", "match" or "bride" never appear in core code or core UI strings.
- **Context lives at the edges.** Everything context-specific belongs to a **domain pack** (terminology, default rules, templates, consent wording, big-screen hints) or to a **plugin** (where the elements come from). Adding a new use case means adding a domain pack, not changing the core.
- **Elements are not always people.** An element can be a person, a team, a song, a dish, a house in a reality show. Avatars, consent and images must handle all of these.
- **Every feature passes the "three contexts" test.** Before building a feature, describe how it works in at least three different use cases from section 6. If it only makes sense for one, it belongs in a domain pack or a plugin, or it is not built.
- **Tests prove it.** Integration and end-to-end tests run the same journeys with at least two different domain packs.
- **The first market is not the product.** Conference-specific needs discovered during the pilot are implemented as domain-pack or plugin features, never as special cases in the core.

---

## 2. Origin of the idea

The idea came out of a search for a product with a **small price for many users**, no bureaucracy, and value that a general-purpose AI assistant cannot replace (it needs shared real-time state, a group, and a physical event).

1. **The "fanta" format has huge demand in Italy.** FantaSanremo grew from 47 players at the first edition to 3.8 million players and more than 5 million teams ([Il Fatto Quotidiano](https://www.ilfattoquotidiano.it/autori/fantasanremo/)). There are dedicated "fanta" games for TV shows, minor sports and even cryptocurrencies, each one built as its own app from scratch.
2. **Many fanta games are still run by hand**, with PDF rulebooks, online forms and scores reported through Instagram direct messages (example: [Fanta Podebrady](https://www.atleticamagazine.it/wp-content/uploads/2025/05/Regolamento-Fanta-Podebrady-2025.pdf)).
3. **People pay per event.** [FantaSposi](https://www.fantasposi.it/foglio-informativo-fantasposi/) sells a "premium game" per wedding, with custom bonuses, a shared gallery and an AI assistant to assign points.
4. **A generic engine already exists: [Fantunque](https://fantunque.com/).** It is the direct competitor (section 4). The product must win on curated domains, plugins and minimal friction.

---

## 3. The problem

**Organizers** (of an event, a party, a tournament, a community):
- Creating a fanta game today means building an app from scratch or running everything by hand across spreadsheets, forms and chats.
- Even generic tools require typing every element by hand and inventing the rulebook from zero.
- Keeping a group engaged for hours or days is hard.

**Participants:**
- There is no light, shared ritual that gives a reason to follow the event and talk about it together.
- Registrations and app downloads are a barrier at a party or a conference.

**Sponsors:**
- They want visibility and real interactions; a shared game is a natural channel.

---

## 4. Market and competition

### 4.1 Fantunque — direct competitor

Website: **https://fantunque.com/** (checked on October 7, 2026)

What it does:
- Lets anyone create a fanta game about anything: choose what is in play, invent bonuses and penalties, invite the group.
- Offers 8 ready-made templates: convention, team building, onboarding, office life, festival, amateur tournament, exam session, project work. The "Fanta Convention" template turns keynotes, demos and moments of the day into predictions.
- Invitations via link, code, QR or a ready-made WhatsApp message.
- Game creation and live control from the browser only; a free iOS and Android app for participants.
- Email account required for organizers **and** participants.
- Points are assigned by the organizer from the control room; participants can propose events from the app.
- A format can be duplicated for a new edition.
- A dedicated section for companies.
- No public pricing at the time of the check.

**Reading:** a well-designed, general-purpose product oriented to companies and universities. Elements are entered by hand or from a template; there is no evidence of imports from external sources. It looks very recent (search engines barely index it), so the market has no winner yet.

**Implication:** since FantaRoster is also general-purpose, competition is head-on. The difference must be built on three levers: **curated domains, source plugins, account-free participation.**

### 4.2 Vertical fanta games (FantaSposi, FantaSanremo, …)

Each one is tied to a single context. They confirm demand and willingness to pay. For FantaRoster they are also a pattern: every successful vertical is a candidate **domain** of the platform.

### 4.3 Event platforms with gamification (e.g. Whova)

Apps such as [Whova](https://whova.com/) have leaderboards and contests, but they reward activity **inside the app** (posts, shared articles, meetups), and sponsor booth interactions become sales leads. They are complete, expensive platforms. Not direct competitors, but they prove that "sponsors pay for interaction" works.

### 4.4 Positioning

| | Fantunque | Vertical fanta | Whova & similar | **FantaRoster** |
|---|---|---|---|---|
| Context | Anything | One only | Commercial conferences | **Anything, with curated domains** |
| Elements in play | Manual or template | Predefined | — | **Imported through plugins** |
| Rulebook | Generic templates | Fixed | — | **Domain packs** |
| Participant access | Email account | Account | Account | **Nickname only** |
| Automatic scoring | No | Depends | In-app activity | **Score plugins (later)** |
| Big screen | — | — | Depends | **Dedicated view** |
| Sponsors | — | — | Digital booths | **Sponsor bonuses in the game** |

---

## 5. Key concepts and glossary

### 5.1 Glossary (UI term ↔ code term)

The code uses brand-neutral English names. The UI uses localized, friendly terms.

| Code term | UI (EN) | UI (IT) | Meaning |
|---|---|---|---|
| `Game` | Game | Fanta | One fantasy game instance (e.g. "DevConf 2027 fanta") |
| `DomainPack` | Domain | Ambito | Package of terminology, default rules, templates and recommended plugins for a context |
| `Element` | Player (domain-specific: speaker, contestant, team…) | Elemento in gioco (speaker, concorrente, squadra…) | Something participants can pick |
| `Rule` | Bonus / penalty | Bonus / malus | A scored event with positive or negative points |
| `Participant` | Player | Giocatore | A person playing the game (nickname only) |
| `Lineup` | Roster | Rosa | The participant's picks, including the captain |
| `ScoreEntry` | Point | Punto | One application of a rule to an element (or to a participant) |
| `Referee` | Referee | Arbitro | A person allowed to assign points |
| `Report` | Report a moment | Segnala un momento | A participant-submitted moment, approved or rejected by a referee |
| `SourceBinding` | Data source | Sorgente dati | Link between a game and a source plugin, with its configuration |
| `SponsorBonus` | Sponsor bonus | Bonus sponsor | A QR that gives a personal bonus once per participant |
| `Brand` | — | — | Brand identity resolved per host (FantaRoster or ImagiRoster) |

> Note: `DomainPack` is used instead of `Domain` to avoid confusion with the DDD domain layer (`Roster.Domain`).

### 5.2 Domains (domain packs)

A domain pack makes the general platform immediately useful in one context. It contains:

- **Terminology** per culture (what elements, referees and participants are called)
- **Default rulebooks** with typical bonuses and penalties
- **Templates** ready to customize
- **Recommended plugins** to import elements
- **Ethical constraints** for that context (e.g. consent of people in play)
- **Big-screen layout** hints

Planned domains (the order reflects go-to-market, not importance: the **Generic** pack is the foundation every other pack builds on):

| Domain | Typical elements | Source plugins | Priority |
|---|---|---|---|
| **Generic** | Anything | CSV, quick entry | **P0 — foundation** |
| **Tech conferences** | Speakers, sessions | Sessionize, CSV, ICS | **P0 — first market (pilot)** |
| Events and parties | Guests, moments | CSV, quick entry | P1 |
| Amateur sports | Teams, players, matches | CSV, ICS | P1 |
| TV and shows | Contestants, artists | CSV, quick entry | Later |
| School and university | Projects, exam sessions (never real grades) | CSV | Later |
| Companies | Teams, convention sessions | CSV, ICS, Sessionize | Later |

### 5.3 Plugins

Plugins connect the platform to external sources. There are three families:

| Family | What it does | Examples | Priority |
|---|---|---|---|
| **Element sources** | Import what is in play, and resync when the source changes | Sessionize, CSV/Excel, Google Sheets, ICS calendar | P0 (Sessionize + CSV), P1 (others) |
| **Score sources** | Turn external events into point proposals | Generic incoming webhook, sports results, build events for hackathons | P2 |
| **Outputs** | Take data out | Leaderboard export, social card, outgoing webhook | P1 |

The **generic incoming webhook** is the most powerful extension point: any external system can post an event ("team X scored", "the build failed") and the platform turns it into a point assignment, applied automatically or queued for a referee.

---

## 6. Use cases

The platform serves **many use cases with the same core**. The table below is a sample, not a boundary: any situation where a group can follow something together and score what happens is a valid use case. Each row becomes a domain pack (or simply uses the Generic pack).

| Use case | Elements in play | Example bonuses and penalties | Typical source | Particular needs |
|---|---|---|---|---|
| **Tech conference** | Speakers | Demo fails and is rescued live (+10); runs over time (−3) | Sessionize, CSV | Consent of speakers; parallel rooms; sponsors |
| **Wedding or party** | Guests, couple, relatives | Catches the bouquet (+10); first on the dance floor (+5); speech longer than 5 minutes (−2) | Guest list (CSV, quick entry) | Private, small group; photos of moments |
| **Amateur tournament** (five-a-side, padel, volleyball) | Teams or players | Goal (+3); comeback win (+8); yellow card (−2) | CSV, ICS calendar | Multi-day; later, automatic results through a score plugin |
| **TV show night** (talent show, song festival, reality) | Contestants, artists, songs | Standing ovation (+5); cries on stage (+3); wardrobe malfunction (+4) | CSV, quick entry | Many episodes; very large public games |
| **Office and team building** | Colleagues, teams | Says "let's take this offline" (+2); meeting ends early (+5) | CSV | Never about performance or evaluation; opt-in only |
| **School trip or summer camp** | Groups, cabins, teachers | Group arrives on time (+3); wins the treasure hunt (+10) | CSV | Minors involved: no personal data, organizer-only setup |
| **Family holidays** (Christmas, New Year) | Relatives | Brings up politics (−3); falls asleep on the sofa (+4) | Quick entry | Tiny group, free plan, zero setup |
| **Hackathon** | Teams | Build passes on first push (+5); live demo works (+10) | CSV; later build webhooks | Natural fit for score plugins |
| **Local festival or village fair** | Stands, events, contests | Longest queue (+3); runs out of food first (+5) | CSV | Sponsors from local businesses |

### 6.1 Detailed example A — tech conference (first market)

Chosen as the **first market** because the founder has direct access to community organizers (user groups, community days, MVP-run events). It is one use case among those above.

**Scenario.** A one-day community conference, 3 parallel rooms, 24 sessions, about 400 attendees. The agenda is published on Sessionize.

**Before the event**
- The organizer creates a game from the "Tech conferences" domain pack and connects the **Sessionize plugin**: speakers and sessions are imported.
- If the agenda changes (a speaker replaced, a session moved), the plugin resyncs it.
- Speakers receive an invitation to consent to being "in play" (whoever declines is excluded).
- Participants join with a QR code (on the badge, on tables or in the confirmation email), choose a nickname and pick **3 speakers**, one of them as **captain** (double points).

**During the event**
- One or more referees (the organizer or a volunteer per room) assign bonuses and penalties from the referee console on their phone.
- Participants can **report a moment** with a photo; a referee approves or rejects it with one tap.
- Between sessions, the big screen shows the leaderboard and the latest points.

**After the event**
- Final leaderboard, award ceremony at the closing session, shareable card for LinkedIn and X.
- The organizer duplicates the format for the next edition.

**Default rulebook of the "Tech conferences" pack** — light-hearted and observable, **never about the quality of the talk**:

| Event | Points |
|---|---|
| The demo fails and the speaker rescues it live | **+10** |
| Live coding works on the first try | **+8** |
| The speaker says "it works on my machine" | **+5** |
| An audience question the speaker cannot answer, and admits it | **+5** |
| The "Questions?" slide appears before half time | **+3** |
| Spontaneous applause mid-talk | **+6** |
| The speaker quotes another speaker of the day | **+4** |
| A meme in the slides | **+2** |
| The speaker runs over by more than 5 minutes | **−3** |
| The HDMI cable does not work on the first try | **−2** |
| Room Wi‑Fi drops during the demo | **0** *(not their fault: no points)* |

**Sponsor bonus.** Every booth has a QR code. Scanning it gives a **personal bonus** (e.g. +10) added to the participant's total, independently of their roster. The sponsor sees the number of scans and, only with the participant's explicit consent, their contact details. The same feature works for a local festival (stands), a tournament (club sponsors) or a wedding (none).

### 6.2 Detailed example B — amateur five-a-side tournament

Shows that the same core serves a completely different context, with no conference concepts involved.

**Scenario.** A two-weekend five-a-side tournament among 12 teams of friends and colleagues; about 150 players and supporters. The calendar is a shared spreadsheet.

- The organizer creates a game from the Generic pack (or the Amateur sports pack in P1) and imports the 12 teams from **CSV**.
- Elements are **teams, not people**: they get a crest or initials instead of a photo; consent is given by each team captain.
- Supporters pick **2 teams** and a captain; lineups lock at the first kick-off.
- One referee per pitch assigns points from the pitch side, often outdoors in daylight.
- The leaderboard lives across two weekends; the big screen at the bar shows it between matches.

| Event | Points |
|---|---|
| Goal | **+3** |
| Comeback win | **+8** |
| Clean sheet | **+4** |
| Goal from outside the box | **+5** |
| Yellow card | **−2** |
| Arrives late to kick-off | **−3** |

### 6.3 What the examples prove

The same entities (`Game`, `Element`, `Rule`, `Lineup`, `ScoreEntry`), the same screens and the same real-time pipeline serve both examples. Only the **domain pack** (words, default rules, consent wording) and the **plugin** (Sessionize vs CSV) change.

---

## 7. Goals and non-goals

### Goals (version 1)

1. **Engagement:** at least 30% of attendees join the game at the pilot event (stretch: 50%).
2. **Attention over time:** at least half of the players open the leaderboard 3 or more times during the event.
3. **Fast setup thanks to plugins:** an organizer creates a complete game from an external source in less than 10 minutes.
4. **Repeat usage:** the pilot organizer states they would use it again.
5. **Proven generality:** before the pilot, the full journey works with two domain packs (Generic and Tech conferences) with no code differences; after the pilot, a real event in a different use case (e.g. a tournament or a party).

### Non-goals (version 1)

- **No context-specific logic in the core**, ever (see §1.1).
- **No third-party plugin marketplace.** v1 plugins are built in-house; the contract is designed to open up later.
- **No automatic scoring in v1.** Referees assign points; score plugins arrive in P2.
- **No evaluation of the quality of people in play.** Reputational risk is too high (section 10).
- **No native app at launch.** A PWA is enough and reduces friction; a .NET MAUI app comes later.
- **No cash prizes or betting mechanics**, to stay clear of gambling regulations.

---

## 8. Personas and user stories

### Personas

| Persona | Context | Device | Key need |
|---|---|---|---|
| **Organizer** | Prepares the game days before; supervises during the event | Laptop before, phone during | Fast setup, control, low effort |
| **Participant** | Attendee at a crowded, noisy venue, or friend at a party | Phone, one hand, poor connectivity | Join in seconds, understand the score at a glance |
| **Referee** | Volunteer following what happens (a talk, a match, a ceremony, a TV episode) | Phone, one hand, in the dark or in daylight | Assign a point in three taps, fix mistakes |
| **Person in play** | Anyone who is an element: speaker, guest, player, contestant, team captain | Phone or email | Decide whether to take part, see their funny moments |
| **Sponsor** | Booth at the event | Printed QR + report | Visits and measurable engagement |
| **Plugin developer** | Internal first, external later | IDE | Simple, documented contract |

### User stories

**Organizer**
- As an organizer, I want to **choose a domain** so that I start from rules and terminology that fit my context.
- As an organizer, I want to **connect an external source** to import the elements in play without typing them.
- As an organizer, I want **source changes to be reflected in the game**, so I do not have to redo them by hand.
- As an organizer, I want to **delegate scoring** to one or more referees.
- As an organizer, I want to **void a wrong point**.
- As an organizer, I want to **duplicate the format** for the next edition.

**Participant**
- As a participant, I want to **join with a QR code and a nickname**, without creating an account.
- As a participant, I want to **pick my elements and a captain** before the game starts.
- As a participant, I want to **report a moment with a photo**, so the game does not depend only on the referees.
- As a participant, I want to **share my final placement**.

**Person in play**
- As a person in play, I want to **decide whether to take part**, so I never end up in a leaderboard against my will.

**Sponsor**
- As a sponsor, I want **a QR code with a bonus**, so that I attract visits naturally, and I want to know how many people used it.

**Plugin developer**
- As a developer, I want **a simple, documented contract** to add a new source without touching the rest of the platform.
- As a developer, I want to **test a plugin in isolation** with sample data.

---

## 9. Functional requirements

### P0 — required for the pilot

| Requirement | Acceptance criteria |
|---|---|
| Generic game creation | The organizer creates a game with name, elements, lineup size, captain yes/no and captain multiplier, and rules with non-zero points. |
| Domain packs | At least the "Generic" and "Tech conferences" domain packs exist, with terminology (EN, IT), default rulebook and recommended plugins. All context-specific words in the UI come from the active pack. |
| Domain-neutral core | The same end-to-end journey (create, import, join, lineup, score, void, leaderboard) passes with the Generic pack + CSV and with the Tech conferences pack + Sessionize, with no context-specific code in the core. |
| Element source contract | A single interface for element sources; adding a source requires no change to the core. |
| Sessionize plugin | Given a Sessionize API endpoint ID, speakers and sessions are imported with their external IDs. |
| CSV plugin | Elements can be imported from CSV with column mapping. |
| Resync | A new import updates existing elements by external ID, without duplicates and without losing lineups or points. |
| Account-free join | A participant joins via link or QR, picks a nickname unique within the game, and gets their session back on the same device. |
| Lineup and captain | Lineups lock when the game goes live. |
| Referee console | A referee assigns a bonus or penalty in at most 3 taps; the update reaches the leaderboard in under 2 seconds (p95). |
| Void | Any point can be voided while the game is live; history is kept. |
| Live leaderboard and big screen | Real-time leaderboard on phone and browser; full-screen page with leaderboard, latest points and join QR. |
| Consent | People in play can accept or decline; declined elements are not selectable. |
| Two brands | The same deployment serves FantaRoster and ImagiRoster, resolved by host, with brand name, logo, theme tokens and default culture. |
| Localization | All user-facing text available in English and Italian. |

### P1 — right after the pilot

| Requirement | Acceptance criteria |
|---|---|
| Google Sheets and ICS plugins | Import from a shared spreadsheet and from an ICS calendar. |
| Reports with photos | A participant submits a report with an optional photo; no photo becomes public without approval. |
| Multiple referees | The organizer invites referees with a link, optionally scoped to a subset of elements (e.g. one room, one pitch, one group). |
| Outputs | Social card, CSV leaderboard export, outgoing webhook at game end. |
| Sponsor bonus | Sponsor QR with a personal bonus, once per participant. |
| Duplicate edition | Elements, rules and plugin bindings are copied; lineups and scores start from zero. |
| Second domain | One more domain pack (Amateur sports or Events and parties). |
| Payments | Paid plans (Premium per game, Pro per event) through a payment provider. |
| Passkeys for organizers | Organizers can add a passkey in addition to magic links. |

### P2 — to be anticipated in the architecture, not built now

- **Score plugins**, starting from the generic incoming webhook.
- **Third-party plugins** loaded dynamically, with review and isolation.
- .NET MAUI app for participants and referees.
- Public library of user-created rulebooks.
- More languages.

---

## 10. Game rules and ethics

These apply to every domain, with extra rules where needed.

- **Explicit consent from people in play**, with the option to leave at any time.
- **Bonuses about moments, never about quality** of people: default rulebooks only contain light-hearted, observable events.
- **Participant leaderboard first.** Per-element totals can be hidden.
- **Moderation** of photos and text before publication.
- **No mandatory personal data** for participants: a nickname is enough.
- **Sensitive domains** (school, university, work): never real grades, evaluations or individual performance.
- **Sponsors and privacy:** contact details go to a sponsor only with explicit, separate consent (GDPR).
- **No gambling:** no money, no paid entries tied to prizes, no odds.

---

## 11. Business model

*Prices are hypotheses to be validated.*

| Plan | For whom | Price hypothesis |
|---|---|---|
| **Free** | Small groups, community events | Free up to ~30 participants for private games, ~300 for community events, with a "powered by" mark |
| **Premium game** | Parties, weddings, tournaments, groups of friends | €3–9 per game, depending on participants |
| **Pro event** | Commercial conferences, corporate events | €99–299 per event |
| **Sponsor** | Event sponsors | Bonus and report package, sold through the organizer |

**Why it works:** the free plan spreads the product; every game shows it to tens or hundreds of people, including future organizers. Curated domains and plugins justify the upgrade.

---

## 12. Naming and brand

### 12.1 Decision

Two brands, one product: same code and platform, two identities.

| Market | Name | Domain | Tagline |
|---|---|---|---|
| **Italy** (launch) | **FantaRoster** | fantaroster.com | Il fanta di qualsiasi cosa |
| **International** (after Italy) | **ImagiRoster** | imagiroster.com | Fantasy for anything |

**Why two names.** "Fanta" is the prefix that immediately communicates the format in Italy (FantaSanremo, FantaSposi), but abroad it means nothing and evokes Coca-Cola's soft drink. "Roster" (a team's lineup) is common to both worlds. ImagiRoster keeps the second half and replaces the first with imagination: you create something that does not exist, in the spirit of "fantasy".

**Consequence for the code:** the brand is configuration, never code. Brand name, logo, theme tokens, default culture, support email and legal pages are resolved per host.

### 12.2 Rejected names

| Name | Reason |
|---|---|
| FantaConf | Too tied to conferences |
| FantaHub | Generic, contains "Fanta" |
| LeagueForge | "League" ties it to sports and does not convey creating something that does not exist |
| FantasyHub | Already used: the "Fantasy Hub" app launched in the UK by CheckdMedia in 2019 |
| FantasyForge | Evokes the fantasy genre (dragons, role-playing); already used by an iOS AI story app |
| FantasyRoster (.net) | "Fantasy roster" is a generic fantasy-sports term, almost impossible to defend as a trademark; a `.net` domain leaks traffic |
| MetaRoster | Risk of confusion with the Meta trademark |

### 12.3 Checks before public launch

- [ ] Domains `fantaroster.com`, `fantaroster.it`, `imagiroster.com`
- [ ] Trademark search on TMview / EUIPO and USPTO, classes 9 (software) and 41 (entertainment)
- [ ] Opinion from an IP consultant on the "Fanta" prefix (opposition risk)
- [ ] Same or similar names on App Store and Google Play
- [ ] Social handles (Instagram, LinkedIn, X, TikTok)

---

## 13. Technical architecture

### 13.1 Platform baseline

| Item | Choice |
|---|---|
| Runtime | **.NET 10** (LTS). Verified baseline in October 2026: SDK `10.0.401`, runtime `10.0.12` (September 2026 servicing). Always use the latest patch. |
| Language | C# 14 |
| Solution format | `.slnx` |
| Local orchestration | **Aspire 13.x** (13.4.6 or later; use the latest stable at scaffold time) |
| Package management | Central Package Management (`Directory.Packages.props`), latest **stable** versions, updated weekly by Dependabot |

> Version policy: this document fixes the major versions. Exact package versions are pinned at scaffold time to the latest stable release and kept current through Dependabot. Never introduce preview packages without an explicit decision.

### 13.2 Building blocks

| Component | Choice |
|---|---|
| Web host | ASP.NET Core 10, **Blazor Web App** |
| Render modes | Static SSR for public pages; **Interactive Server** for the organizer console; **Interactive WebAssembly** for participant, referee and big-screen views (they must survive network blips) |
| PWA | Web manifest and service worker added to the Blazor Web App for the participant and referee experience |
| API | ASP.NET Core Minimal APIs, built-in validation, OpenAPI document generation |
| Real time | ASP.NET Core SignalR; **Azure SignalR Service** in production |
| Data | EF Core 10 + **Azure SQL Database** (SQL Server container locally through Aspire) |
| Files | **Azure Blob Storage** (Azurite locally) for report photos and generated cards |
| Email | **Azure Communication Services Email** for organizer magic links; a local mail catcher in development |
| Identity | ASP.NET Core Identity for organizers (magic link; passkeys in P1); signed anonymous tokens for participants; scoped invite tokens for referees |
| Secrets | Azure Key Vault (plugin secrets, signing keys) |
| Observability | OpenTelemetry via Aspire service defaults; Azure Monitor / Application Insights in production |
| Hosting | **Azure Container Apps**, provisioned with `azd` from the Aspire app model |
| CI/CD | GitHub Actions |
| UI | Custom Razor component library (`Roster.Ui`) built on design tokens; no third-party UI kit, so the design stays distinctive. Mockups, tokens and brand assets live in `docs/design/` |
| Localization | `IStringLocalizer` with `.resx` resources, cultures `en` and `it` |
| Tests | xUnit v3, bUnit, Shouldly, NSubstitute, Aspire testing / Testcontainers, Playwright for .NET |

**Libraries deliberately avoided:** MediatR and AutoMapper (both moved to commercial licensing), FluentAssertions 8+ (commercial license). Plain application services and explicit mapping are enough for this size.

### 13.3 Solution structure

```
Roster.slnx
├─ src/
│  ├─ Roster.AppHost/                 Aspire app model (local orchestration, azd deployment)
│  ├─ Roster.ServiceDefaults/         OpenTelemetry, health checks, resilience, service discovery
│  ├─ Roster.Domain/                  Entities, value objects, scoring engine, domain rules (no dependencies)
│  ├─ Roster.Application/             Use cases, ports (interfaces), DTOs, authorization policies
│  ├─ Roster.Infrastructure/          EF Core, Blob, email, Key Vault, plugin host, background jobs
│  ├─ Roster.Plugins.Abstractions/    Public plugin contracts (versioned, packable as NuGet)
│  ├─ Roster.Plugins.Csv/             CSV element source
│  ├─ Roster.Plugins.Sessionize/      Sessionize element source
│  ├─ Roster.DomainPacks/             Domain pack definitions (JSON) and loader
│  ├─ Roster.Ui/                      Razor class library: design system components and tokens
│  ├─ Roster.Web/                     Blazor Web App host: endpoints, hubs, organizer console, branding
│  └─ Roster.Web.Client/              WebAssembly: participant PWA, referee console, big screen
└─ tests/
   ├─ Roster.Domain.Tests/
   ├─ Roster.Application.Tests/
   ├─ Roster.Plugins.Tests/           Contract tests run against every plugin
   ├─ Roster.Ui.Tests/                bUnit component tests
   ├─ Roster.IntegrationTests/        API + database + SignalR through Aspire testing
   └─ Roster.E2E/                     Playwright end-to-end tests
```

**Dependency rules:** `Domain` depends on nothing. `Application` depends on `Domain` and `Plugins.Abstractions`. `Infrastructure` implements `Application` ports. `Web` composes everything. Plugins depend only on `Plugins.Abstractions`.

### 13.4 Data model

| Entity | Key fields |
|---|---|
| `Organizer` | Identity user, display name, preferred culture, plan |
| `Game` | Id, slug, name, `DomainPackId`, brand, culture, state, lineup size, captain enabled, captain multiplier, join code, created/started/ended timestamps, `RowVersion` |
| `SourceBinding` | Id, `GameId`, `PluginId`, configuration (JSON), secret reference (Key Vault), last sync time and result |
| `Element` | Id, `GameId`, `SourceBindingId?`, `ExternalId?`, name, subtitle, image URL, group (room, pitch, group, category), metadata (JSON), consent status, source state (`Active`, `MissingFromSource`), selectable flag |
| `Rule` | Id, `GameId`, label, points (non-zero integer), category, target (`Element` or `Participant`) |
| `Participant` | Id, `GameId`, nickname (unique per game), token hash, joined at |
| `Lineup` | `ParticipantId`, picked element IDs, captain element ID, submitted at, locked flag |
| `ScoreEntry` | Id, `GameId`, `ElementId?`, `ParticipantId?`, `RuleId`, points snapshot, source (`Referee`, `ReportApproval`, `Sponsor`, `Plugin`), created by, created at, idempotency key, status (`Valid`, `Voided`), voided by/at, note |
| `Referee` | Id, `GameId`, display name, invite token hash, scope (element IDs or groups) |
| `Report` | Id, `GameId`, `ParticipantId`, `ElementId`, suggested `RuleId?`, text, photo blob name, status (`Pending`, `Approved`, `Rejected`), resulting `ScoreEntryId?` |
| `SponsorBonus` | Id, `GameId`, sponsor name, points, QR token, scan count |
| `ConsentInvitation` | Id, `ElementId`, token hash, contact (email, optional), sent at, answered at |

**Game lifecycle:** `Draft` → `Open` (joins and lineups) → `Live` (lineups locked, scoring) → `Ended` (final leaderboard) → `Archived`.

**Scoring:**
- Element score = sum of valid `ScoreEntry.Points` targeting the element.
- Participant score = Σ over the lineup of element score × (captain ? multiplier : 1) + valid personal entries (e.g. sponsor bonuses).
- Ranking uses standard competition ranking (1, 2, 2, 4).
- The leaderboard is **derived** from score entries: voiding a point marks it `Voided` and triggers a recompute. Points are snapshotted on the entry, so editing a rule never rewrites history.

### 13.5 Plugin architecture

**Principles**
- The core never knows concrete sources: it depends only on the contract.
- Each plugin declares an ID, a display name, capabilities and a **configuration schema**, from which the UI generates its form.
- Imported elements always carry an **external ID**, which makes resync possible.
- In v1, plugins are in-house projects registered through dependency injection. The contract is versioned so it can be opened to third parties later.

**Element source contract (draft)**

```csharp
namespace Roster.Plugins.Abstractions;

public interface IElementSourcePlugin
{
    string Id { get; }                        // e.g. "sessionize", "csv"
    string DisplayName { get; }
    PluginCapabilities Capabilities { get; }  // Import, Resync, Preview
    ConfigSchema GetConfigSchema();           // fields required (e.g. Sessionize endpoint ID)

    Task<ValidationResult> ValidateConfigAsync(PluginConfig config, CancellationToken cancellationToken);
    Task<ImportResult> ImportAsync(PluginConfig config, CancellationToken cancellationToken);
}

public sealed record ImportedElement(
    string ExternalId,
    string Name,
    string? Subtitle,       // e.g. session title, team city, song title
    string? ImageUrl,       // e.g. a photo, a team crest, an artwork
    string? Group,          // e.g. room, pitch, group, category
    IReadOnlyDictionary<string, string> Metadata);

public sealed record ImportResult(
    IReadOnlyList<ImportedElement> Elements,
    IReadOnlyList<string> Warnings);
```

**Score source contract (P2, draft)**

```csharp
public interface IScoreSourcePlugin
{
    string Id { get; }

    // Translates an external event into point proposals that the game applies
    // automatically or queues for a referee, depending on configuration.
    Task<IReadOnlyList<ScoreProposal>> HandleAsync(ExternalEvent externalEvent, CancellationToken cancellationToken);
}
```

**Resync flow**
1. A background job calls `ImportAsync`.
2. Elements are matched by `ExternalId`.
3. New → added; changed → updated; missing → marked `MissingFromSource` (never deleted if they have lineups or points).
4. The organizer sees a summary of changes.

**Planned plugins**

| Plugin | Family | Notes |
|---|---|---|
| Sessionize | Element source | Public API, no key: `https://sessionize.com/api/v2/{endpointId}/view/All` (also `Speakers`, `Sessions`, `GridSmart`). The organizer must create and enable an API endpoint in Sessionize; enabling UTC for the schedule is recommended. Undocumented endpoints (e.g. speaker emails) must never be used. |
| CSV / Excel | Element source | Column mapping in the UI |
| Google Sheets | Element source | Shared sheet, read-only |
| ICS | Element source | Calendar events as elements (sessions, matches) |
| Incoming webhook | Score source | Signed endpoint per game; P2 |
| Outgoing webhook, CSV export, social card | Outputs | P1 |

### 13.6 Real-time design

- **Commands go through HTTP**, not through the hub: `POST /api/games/{id}/score-entries` (with an idempotency key), `POST /api/games/{id}/score-entries/{entryId}/void`, etc.
- **The hub only pushes**: clients join a group per game (`game:{id}`) and receive `LeaderboardUpdated`, `ScoreEntryAdded`, `ScoreEntryVoided`, `GameStateChanged`.
- `LeaderboardUpdated` carries a monotonically increasing version; clients ignore stale versions and refetch on gaps.
- **Referee offline queue:** assignments made without connectivity are stored locally with their idempotency key and replayed on reconnect.
- **MVP scale-out decision:** the API runs as a single replica for the pilot; Azure SignalR Service handles client connections. Multi-replica scoring (outbox + distributed cache) is a later decision, recorded as an ADR.

### 13.7 API surface (initial)

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/games` | Create a game (organizer) |
| `POST` | `/api/games/{id}/sources` | Bind a source plugin |
| `POST` | `/api/games/{id}/sources/{bindingId}/sync` | Import / resync elements |
| `POST` | `/api/games/{id}/state` | Change lifecycle state |
| `POST` | `/api/join/{joinCode}` | Join as participant (nickname) |
| `PUT` | `/api/games/{id}/lineup` | Save lineup and captain |
| `GET` | `/api/games/{id}/leaderboard` | Current leaderboard |
| `POST` | `/api/games/{id}/score-entries` | Assign a point (referee) |
| `POST` | `/api/games/{id}/score-entries/{entryId}/void` | Void a point |
| `POST` | `/api/games/{id}/reports` | Report a moment (P1) |
| `POST` | `/api/sponsor/{token}` | Claim a sponsor bonus (P1) |
| Hub | `/hubs/game` | Real-time push |

### 13.8 Branding

- `BrandOptions` per brand: name, tagline, default culture, logo assets, theme token file, support email, legal URLs.
- A middleware resolves the brand from the request host (`fantaroster.*` → FantaRoster, `imagiroster.*` → ImagiRoster) with a configurable fallback.
- Theme tokens are CSS custom properties: a shared base layer plus one brand layer per brand.

---

## 14. Non-functional requirements

| Area | Requirement |
|---|---|
| Performance | Score update visible on clients in < 2 s (p95); leaderboard API < 300 ms (p95); 1,000 concurrent clients per game at the pilot scale |
| Resilience | Automatic reconnection; referee offline queue; pages usable on slow 3G |
| Accessibility | WCAG 2.2 AA; full keyboard support on desktop; large touch targets; respects reduced motion |
| Security | Rate limiting on join, reports and sponsor claims; signed tokens; SAS URLs for blobs; upload type and size validation; OWASP ASVS level 1 as baseline |
| Privacy | GDPR: data minimization, nickname-only participants, photo retention limited (default 30 days after the game ends), explicit sponsor consent, data export and deletion for organizers |
| Observability | Traces, metrics and logs through OpenTelemetry; dashboards for join rate, score latency, hub connections |
| Localization | No hard-coded user-facing strings; EN and IT complete at launch |
| Maintainability | Nullable enabled, warnings as errors, analyzers on, `dotnet format` clean, ADRs for significant decisions |

---

## 15. Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Head-on competition with Fantunque | High | Differentiate on curated domains, plugins and account-free join; enter through tech communities where the founder has direct access |
| Too generic, no domain done well | High | Generic core, but one curated domain pack at a time; conferences first as go-to-market |
| Overfitting the core to the first market | High | Generic-first principle (§1.1), "three contexts" test for every feature, end-to-end tests with two domain packs, a second real use case right after the pilot |
| People in play feel judged | High | Consent, light-hearted rules only, per-element totals can be hidden |
| Source APIs change or disappear | Medium | Isolated contract, CSV always available as fallback, contract tests for every plugin |
| Referees cannot keep up | Medium | Participant reports, multiple referees, three-tap console |
| Low join rate at the event | High | QR on the badge, on-stage announcement, a prize for the winner |
| Unstable Wi‑Fi | Medium | Light pages, automatic reconnection, offline queue for referees |
| "Fanta" trademark opposition | Medium | IP opinion before investing in the brand; ImagiRoster ready as fallback |

---

## 16. Validation plan

### Pilot event (tech conference)

A tech conference is chosen for the pilot because of **access**, not because the product is about conferences: an event where the founder is a speaker or knows the organizer well.

**Metrics**
- % of attendees who join (success: ≥ 30%)
- % of players who open the leaderboard 3+ times (success: ≥ 50%)
- Setup time from import to publication (success: < 10 minutes)
- Number of participant reports (P1)
- Organizer interview: would they do it again? Would they pay? How much?
- Feedback from people in play (speakers): did they enjoy it or feel uncomfortable?

### After the pilot
1. A second real event in a **different use case** (amateur tournament or party) using CSV or ICS, to prove generality outside conferences.
2. First paid Pro event, first sponsor, first Premium private games.

---

## 17. Open questions

| Question | Owner | Blocking? |
|---|---|---|
| Which event is the pilot, and when? | Founder | Yes |
| Are "FantaRoster" and "ImagiRoster" free as domains and trademarks? Is there opposition risk on "Fanta"? | Founder / IP consultant | Yes, before public launch |
| Which second use case for a real event after the pilot? | Founder | No |
| Should third-party plugins be part of the licensing model? | Founder | No |
| Is a dedicated privacy notice needed for photos and sponsor contacts? | Legal | Yes, for P1 |
| Which payment provider for P1? | Founder | No |
| Should we approach Fantunque for a partnership instead of competing? | Founder | No |

---

## 18. References

- **Fantunque** — generic fanta engine: https://fantunque.com/ (FAQ: https://fantunque.com/faq)
- **FantaSposi** — wedding fanta with a premium game: https://www.fantasposi.it/foglio-informativo-fantasposi/
- **FantaSanremo** — growth of the format: https://www.ilfattoquotidiano.it/autori/fantasanremo/
- **Fanta Podebrady** — fanta run by hand: https://www.atleticamagazine.it/wp-content/uploads/2025/05/Regolamento-Fanta-Podebrady-2025.pdf
- **Whova** — gamification in event apps: https://whova.com/
- **Fantasy Hub (CheckdMedia, 2019)** — name conflict for "FantasyHub": https://sbcnews.co.uk/europe/2019/08/28/checkdmedia-launches-fantasy-hub-seeking-to-disrupt-all-fantasy-football-engagements/
- **Sessionize API** — https://sessionize.com/playbook/api
- **.NET 10 downloads** — https://dotnet.microsoft.com/en-us/download/dotnet/latest
