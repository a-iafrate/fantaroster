namespace Roster.E2E;

/// <summary>All end-to-end tests share one running app and one browser.</summary>
[CollectionDefinition(Name)]
public sealed class E2EGroup : ICollectionFixture<RosterAppFixture>
{
    public const string Name = "e2e";
}
