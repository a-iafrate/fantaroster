using Microsoft.EntityFrameworkCore;
using Roster.Domain.Elements;
using Roster.Domain.Games;
using Roster.Domain.Participants;
using Roster.Domain.Referees;
using Roster.Domain.Scores;
using Roster.Domain.Sources;

namespace Roster.Infrastructure.Data;

public sealed class RosterDbContext : DbContext
{
    public RosterDbContext(DbContextOptions<RosterDbContext> options) : base(options)
    {
    }

    public DbSet<Game> Games => Set<Game>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<Element> Elements => Set<Element>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Lineup> Lineups => Set<Lineup>();
    public DbSet<ScoreEntry> ScoreEntries => Set<ScoreEntry>();
    public DbSet<Referee> Referees => Set<Referee>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<SponsorBonus> SponsorBonuses => Set<SponsorBonus>();
    public DbSet<SourceBinding> SourceBindings => Set<SourceBinding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RosterDbContext).Assembly);
    }
}
