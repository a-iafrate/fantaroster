using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace Roster.E2E;

/// <summary>
/// Starts Roster.Web on a free local port (or uses ROSTER_E2E_BASE_URL) and a headless Chromium
/// shared by every end-to-end test. Chromium is installed on first use.
/// The app runs against a dedicated E2E database (LocalDB by default, or ROSTER_E2E_CONNECTION_STRING),
/// never against the database configured in appsettings.
/// </summary>
public sealed class RosterAppFixture : IAsyncLifetime
{
    private const string BaseUrlVariable = "ROSTER_E2E_BASE_URL";
    private const string ConnectionStringVariable = "ROSTER_E2E_CONNECTION_STRING";
    private const string ScreenshotsVariable = "ROSTER_E2E_SCREENSHOTS";
    private const string DefaultConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=RosterE2E;Trusted_Connection=True;TrustServerCertificate=True";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(2);

    private readonly StringBuilder _serverOutput = new();
    private Process? _server;
    private IPlaywright? _playwright;

    public Uri BaseUrl { get; private set; } = null!;

    public IBrowser Browser { get; private set; } = null!;

    /// <summary>Database used by the app under test; tests seed their games here.</summary>
    public string ConnectionString { get; } = Environment.GetEnvironmentVariable(ConnectionStringVariable) is { Length: > 0 } configured
        ? configured
        : DefaultConnectionString;

    public async Task InitializeAsync()
    {
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright could not install Chromium (exit code {exitCode}).");
        }

        var configured = Environment.GetEnvironmentVariable(BaseUrlVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            await PrepareDatabaseAsync();
            BaseUrl = await StartServerAsync();
        }
        else
        {
            BaseUrl = new Uri(configured);
        }

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }
        _playwright?.Dispose();

        if (_server is { HasExited: false })
        {
            _server.Kill(entireProcessTree: true);
            await _server.WaitForExitAsync();
        }
        _server?.Dispose();
    }

    /// <summary>Creates an isolated browser context (own storage) with the given device settings.</summary>
    public async Task<IPage> NewPageAsync(int width = 390, int height = 844, ColorScheme colorScheme = ColorScheme.Light, string locale = "it-IT")
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl.ToString(),
            ViewportSize = new ViewportSize { Width = width, Height = height },
            ColorScheme = colorScheme,
            Locale = locale,
        });
        return await context.NewPageAsync();
    }

    /// <summary>
    /// Signs an organizer in through the real magic-link flow: asks for a link on the login page, reads it
    /// from the server log (development email sender) and opens it. Only works when the fixture started the app.
    /// </summary>
    public async Task<IPage> NewOrganizerPageAsync(int width = 390, int height = 844, ColorScheme colorScheme = ColorScheme.Light)
    {
        if (_server is null)
        {
            throw new InvalidOperationException("Organizer sign-in needs the app started by the fixture (the magic link is read from its log).");
        }

        var email = $"organizer-{Guid.NewGuid():N}@example.test";
        var page = await NewPageAsync(width, height, colorScheme);
        await page.GotoAsync("/login");
        await page.Locator("#email").FillAsync(email);
        await page.GetByRole(AriaRole.Button, new() { Name = "Inviami il link" }).ClickAsync();

        var link = await WaitForMagicLinkAsync(email);
        await page.GotoAsync(link);
        await page.WaitForURLAsync("**/organizer");
        return page;
    }

    private async Task<string> WaitForMagicLinkAsync(string email)
    {
        var pattern = new System.Text.RegularExpressions.Regex($@"https?://\S+/auth/verify\?\S*{System.Text.RegularExpressions.Regex.Escape(email)}\S*");
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var match = pattern.Match(ServerOutput());
            if (match.Success)
            {
                return System.Net.WebUtility.HtmlDecode(match.Value);
            }
            await Task.Delay(200);
        }
        throw new TimeoutException($"No magic link for {email} in the server log:{Environment.NewLine}{ServerOutput()}");
    }

    /// <summary>Saves a screenshot when ROSTER_E2E_SCREENSHOTS points to a folder (for visual review).</summary>
    public async Task ScreenshotAsync(IPage page, string name, bool fullPage = false)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (Environment.GetEnvironmentVariable(ScreenshotsVariable) is { Length: > 0 } folder)
        {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(folder, name + ".png"), FullPage = fullPage });
        }
    }

    private async Task<Uri> StartServerAsync()
    {
        var port = GetFreePort();
        var url = new Uri($"http://127.0.0.1:{port}/");
        var project = Path.Combine(FindRepositoryRoot(), "src", "Roster.Web", "Roster.Web.csproj");
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif

        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            // Like running from the project folder: the content root and the static web assets
            // (including the scoped CSS bundle) resolve from there.
            WorkingDirectory = Path.GetDirectoryName(project)!,
        };
        foreach (var argument in new[] { "run", "--project", project, "--no-build", "--no-launch-profile", "-c", configuration })
        {
            startInfo.ArgumentList.Add(argument);
        }
        startInfo.Environment["ASPNETCORE_URLS"] = url.ToString();
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["ConnectionStrings__sqldb"] = ConnectionString;
        // Never send real email from tests: without a connection string the app writes the magic link to its log.
        startInfo.Environment["AzureCommunicationServices__ConnectionString"] = string.Empty;
        // Local SignalR: tests must never talk to the Azure SignalR Service configured in appsettings.
        startInfo.Environment["Azure__SignalR__ConnectionString"] = string.Empty;

        _server = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start Roster.Web.");
        _server.OutputDataReceived += (_, e) => AppendOutput(e.Data);
        _server.ErrorDataReceived += (_, e) => AppendOutput(e.Data);
        _server.BeginOutputReadLine();
        _server.BeginErrorReadLine();

        using var http = new HttpClient { BaseAddress = url, Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTimeOffset.UtcNow + StartupTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (_server.HasExited)
            {
                throw new InvalidOperationException($"Roster.Web exited during startup:{Environment.NewLine}{ServerOutput()}");
            }
            try
            {
                using var response = await http.GetAsync(new Uri("/", UriKind.Relative));
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return url;
                }
            }
            catch (HttpRequestException)
            {
                // Not listening yet.
            }
            catch (TaskCanceledException)
            {
                // Startup still in progress.
            }
            await Task.Delay(500);
        }

        throw new TimeoutException($"Roster.Web did not answer on {url} within {StartupTimeout}:{Environment.NewLine}{ServerOutput()}");
    }

    private async Task PrepareDatabaseAsync()
    {
        await using var db = TestData.CreateContext(ConnectionString);
        // Start from an empty schema, but only drop the database this suite owns.
        if (ConnectionString == DefaultConnectionString)
        {
            await db.Database.EnsureDeletedAsync();
        }
        await db.Database.MigrateAsync();
    }

    private void AppendOutput(string? line)
    {
        if (line is null)
        {
            return;
        }
        lock (_serverOutput)
        {
            _serverOutput.AppendLine(line);
        }
    }

    private string ServerOutput()
    {
        lock (_serverOutput)
        {
            return _serverOutput.ToString();
        }
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Roster.slnx")))
            {
                return directory.FullName;
            }
        }
        throw new InvalidOperationException("Roster.slnx not found above the test output folder.");
    }
}
