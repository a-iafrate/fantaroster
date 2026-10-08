---
applyTo: "tests/**"
---

# Test rules

- Frameworks: xUnit v3, Shouldly for assertions, NSubstitute for test doubles, bUnit for components, Playwright for .NET for end-to-end, Aspire testing or Testcontainers for integration.
- Test projects mirror `src/` (`Roster.Domain` → `Roster.Domain.Tests`).
- Name tests `Method_State_ExpectedResult` or as a readable sentence with underscores.
- Arrange / Act / Assert, one behavior per test.
- Deterministic time with a fake `TimeProvider`; never `Thread.Sleep` or `Task.Delay` to wait for results.
- No live external services: plugins use recorded fixtures and must pass the shared contract suite in `tests/Roster.Plugins.Tests`.
- Every bug fix starts with a failing test that reproduces it.
- Scoring engine changes need tests for ties, voided entries, captain multiplier, personal bonuses and empty lineups.
