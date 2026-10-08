---
applyTo: "**/*.razor,**/*.razor.cs,**/*.razor.css"
---

# Blazor rules

- Render modes: static SSR for public pages, Interactive Server for the organizer console, Interactive WebAssembly for participant, referee and big-screen views. Declare render modes explicitly.
- Components in `Roster.Ui` are brand-agnostic: style them with CSS isolation (`.razor.css`) and design tokens (CSS custom properties such as `var(--color-surface)`). No inline styles, no hard-coded colors, sizes or fonts.
- Every user-facing string comes from `IStringLocalizer<T>` resources in English and Italian. Leave room for 30% text expansion.
- Accessibility (WCAG 2.2 AA): semantic HTML, labels for every input, visible focus, touch targets of at least 44×44 px, rank changes conveyed with text or shape and not only color, honor `prefers-reduced-motion`.
- Keep components small: parameters in, events out (`EventCallback`). Put logic in services, not in markup.
- Participant and referee views must tolerate connection loss: show the connection state, reconnect automatically, and refetch when a pushed version is missing.
- The referee console must allow assigning a point in three taps and undoing it for 10 seconds.
- Never read brand names in components; use the cascading brand information provided by the host.
