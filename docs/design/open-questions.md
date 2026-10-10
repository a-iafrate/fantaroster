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
