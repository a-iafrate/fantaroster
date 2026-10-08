# Design

This folder contains the **graphical interface mockups** and the design system produced from [`../design-prompt.md`](../design-prompt.md). It is the visual source of truth for every UI task: components and screens must be implemented to match these files.

> Status: waiting for the design deliverables. Until they arrive, UI work uses unstyled components wired to placeholder tokens (see the development plan).

## Expected structure

```
docs/design/
├─ README.md                 This file
├─ rationale.md              Design concept and principles (one page)
├─ open-questions.md         Questions raised by the design work and their answers
├─ tokens/
│  ├─ tokens.base.json       Shared tokens (spacing, type scale, radius, motion, neutrals)
│  ├─ tokens.fantaroster.json  Brand layer, light and dark
│  ├─ tokens.imagiroster.json  Brand layer, light and dark
│  └─ tokens.css             The same tokens as CSS custom properties
├─ components/               Component specs with states, sizes and spacing
├─ screens/
│  ├─ participant/           Mobile mockups (390×844)
│  ├─ referee/               Mobile mockups, low-light and daylight themes
│  ├─ organizer/             Desktop (1440×900) and mobile mockups
│  ├─ big-screen/            1920×1080 mockups
│  └─ public/                Landing pages and consent page
└─ assets/
   ├─ logos/                 SVG logos for both brands
   ├─ icons/                 App icons (48, 192, 512 px, maskable) and favicons
   └─ social/                Social card templates (1200×630, 1080×1920)
```

## Rules

- File names in English, lowercase, words separated by hyphens (e.g. `screens/participant/04-live-leaderboard-fantaroster.png`).
- Mockups use real content from the use cases in `../project-description.md` §6, in at least two different contexts.
- When a mockup and the documentation disagree, the documentation wins on behavior and the mockup wins on visuals; record the decision in `open-questions.md`.
- Tokens in `tokens/` are the only source for colors, type, spacing and motion in `Roster.Ui`.
