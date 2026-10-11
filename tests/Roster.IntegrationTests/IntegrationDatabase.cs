using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Roster.Infrastructure.Data;

namespace Roster.IntegrationTests;

/// <summary>
/// One empty database per test run, created and migrated on first use and dropped when the process exits.
/// It is LocalDB by default; set ROSTER_TEST_CONNECTION_STRING to any SQL Server (for CI) and a database with a
/// unique name is created there. Tests never touch the database configured in appsettings.json.
/// </summary>
public static class IntegrationDatabase
{
    private const string ConnectionStringVariable = "ROSTER_TEST_CONNECTION_STRING";
    private const string DefaultServer = @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";

    private static readonly Lazy<string> Lazy = new(Create);

    public static string ConnectionString => Lazy.Value;

    public static RosterDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RosterDbContext>().UseSqlServer(ConnectionString).Options);

    private static string Create()
    {
        var server = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        var builder = new SqlConnectionStringBuilder(string.IsNullOrWhiteSpace(server) ? DefaultServer : server)
        {
            InitialCatalog = $"RosterIntegration_{Guid.NewGuid():N}",
        };
        var connectionString = builder.ConnectionString;

        using (var context = new RosterDbContext(new DbContextOptionsBuilder<RosterDbContext>().UseSqlServer(connectionString).Options))
        {
            context.Database.Migrate();
        }

        AppDomain.CurrentDomain.ProcessExit += (_, _) => Drop(connectionString);
        return connectionString;
    }

    private static void Drop(string connectionString)
    {
        try
        {
            using var context = new RosterDbContext(new DbContextOptionsBuilder<RosterDbContext>().UseSqlServer(connectionString).Options);
            context.Database.EnsureDeleted();
        }
        catch (SqlException)
        {
            // Nothing to clean up if the server is already gone.
        }
    }
}
