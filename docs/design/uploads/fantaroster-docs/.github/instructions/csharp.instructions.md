---
applyTo: "**/*.cs"
---

# C# rules

- File-scoped namespaces matching the folder path (`Roster.<Project>.<Folder>`); one public type per file.
- Classes are `sealed` unless designed for inheritance. Use `record` for DTOs and value objects.
- Primary constructors for dependency injection; keep services small and focused.
- Async methods end with `Async` and take a `CancellationToken` as the last parameter, passed all the way down.
- Use `TimeProvider` for time; store `DateTimeOffset` in UTC. Use `decimal` for money.
- Guard clauses with `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`.
- Return results for expected failures (validation, not found, conflict); throw only for exceptional cases.
- Structured logging; `LoggerMessage` source generators on hot paths; never log tokens, emails or images.
- Domain invariants live in domain methods (`Game.GoLive()`, `Lineup.SetCaptain()`), not in services or endpoints.
- `ScoreEntry` is never deleted: void it. The leaderboard is always derived from valid entries.
- EF Core: no lazy loading, `AsNoTracking()` for reads, projections to DTOs, configurations in `IEntityTypeConfiguration<T>`.
- Minimal APIs: one `MapGroup` per feature, typed results, built-in validation, OpenAPI metadata; score commands require an idempotency key.
- SignalR hubs only push events to clients; commands go through HTTP endpoints.
- XML doc comments on public types in `Roster.Plugins.Abstractions` and on `Roster.Application` ports.
