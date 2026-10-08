# Current Status
We have successfully applied the design system tokens to the Organizer Console's primary screens: the Dashboard ("Le mie partite") and the Create Game Wizard.
All hardcoded pixel spacings and sizes in `CreateGame.razor.css` and `Dashboard.razor.css` have been replaced with the official CSS tokens (`var(--space-*)`, `var(--text-*)`, etc.).
We also resolved various layout and localization bugs in the Create Game Wizard (e.g., adding the missing Captain Multiplier input on Step 2, localizing dynamic exception messages, fixing `string.Format` formatting for `IStringLocalizer`).
The project currently builds successfully with 0 warnings and 0 errors, and `dotnet format` checks pass.

# Next Steps
Please pick up development from this point. The immediate next step is to continue with Phase 3 (Organizer Console) feature completion.
Now that the Dashboard and Create Game Wizard visuals are built, focus on one of the following remaining features:
- **Element management**: edit, hide, mark as not selectable, show source state and consent status.
- **Consent flow**: generate consent invitations, public consent page, and status visibility.
- **Rule editor**: add, edit, reorder, delete rules (only while Draft/Open).
- **Lifecycle controls**: open joins, go live, end game, archive.

Choose one of these functional blocks to implement next (creating the necessary Razor components and application services).

# Guidelines
- Ensure strict adherence to the project rules in `AGENTS.md` and `GEMINI.md`.
- Keep in mind the generic-first rule.
- Do not introduce build warnings (`TreatWarningsAsErrors` is true).
- Follow the design mockups exactly.
- To test UI changes efficiently, start the app with `dotnet run --project src/Roster.Web` (instead of using the Aspire AppHost).

