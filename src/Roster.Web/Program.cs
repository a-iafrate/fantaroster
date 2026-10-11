using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Roster.Application.Ports;
using Roster.Application.Services;
using Roster.Infrastructure;
using Roster.Infrastructure.Data;
using Roster.Plugins.Abstractions;
using Roster.Plugins.Csv;
using Roster.Plugins.Sessionize;
using Microsoft.AspNetCore.Localization;
using Roster.Web.Brand;
using Roster.Web.Components;
using Roster.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<Roster.Web.Options.BrandOptions>(builder.Configuration.GetSection(Roster.Web.Options.BrandOptions.SectionName));
builder.Services.AddSingleton<BrandResolver>();
builder.Services.AddScoped<IBrandAccessor, BrandAccessor>();

builder.Services.AddDbContext<RosterDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("sqldb")));

// Application
builder.Services.AddScoped<IResyncService, ResyncService>();
builder.Services.AddScoped<IGameCreationService, GameCreationService>();
builder.Services.AddScoped<GameLifecycleService>();
builder.Services.AddScoped<GameDuplicationService>();
builder.Services.AddScoped<GameSettingsService>();
builder.Services.AddScoped<ElementManagementService>();
builder.Services.AddScoped<RuleManagementService>();
builder.Services.AddScoped<ConsentManagementService>();
builder.Services.AddScoped<RefereeManagementService>();
builder.Services.AddScoped<ParticipantManagementService>();
builder.Services.AddScoped<LeaderboardService>();
builder.Services.AddScoped<ActivityFeedService>();
builder.Services.AddScoped<GameOverviewService>();
builder.Services.AddScoped<Roster.Web.Components.Layout.OrganizerContext>();
builder.Services.AddScoped<ScoreManagementService>();
builder.Services.AddSingleton<Roster.Application.Games.Queries.LeaderboardStateStore>();
builder.Services.AddSingleton(TimeProvider.System);

// The Azure SignalR Service carries every connection, the Blazor circuits of the organizer console included. Its free tier
// stops answering (HTTP 429 "Connection count reaches limit") once 20 connections are open or the daily quota is used, and the
// pages then look dead: buttons do nothing. Locally the app uses the built-in SignalR unless Azure:SignalR:UseInDevelopment is true.
var azureSignalRConnectionString = builder.Environment.IsDevelopment() && !builder.Configuration.GetValue<bool>("Azure:SignalR:UseInDevelopment")
    ? null
    : builder.Configuration["Azure:SignalR:ConnectionString"];

if (!string.IsNullOrWhiteSpace(azureSignalRConnectionString))
{
    builder.Services.AddSignalR()
        .AddAzureSignalR(options =>
        {
            options.ConnectionString = azureSignalRConnectionString;
        });
}
else
{
    builder.Services.AddSignalR();
}

builder.Services.AddScoped<Roster.Application.Ports.Notifications.IGameNotificationService, Roster.Web.Services.GameNotificationService>();

// Infrastructure
builder.Services.AddInfrastructure();

// Built-in minimal API validation: the data annotations on the request records are enforced at the edge (400 with details).
builder.Services.AddValidation();

// Participant tokens and organizer cookies are protected with keys that rotate on their own (every 90 days; old keys still
// unprotect). The key ring must survive a restart and be shared by every replica, otherwise everyone is signed out:
// point Security:DataProtection:KeysPath at a persistent shared folder; Azure will use Blob Storage + Key Vault (Phase 7 deployment).
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("Roster");
if (builder.Configuration["Security:DataProtection:KeysPath"] is { Length: > 0 } keysPath)
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}
builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.IEmailSender<OrganizerUser>, Roster.Infrastructure.Email.IdentityEmailSender>();

// Authentication & Identity
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme)
    .AddCookie(Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme, options =>
    {
        options.LoginPath = "/login";
    });

builder.Services.AddIdentityCore<OrganizerUser>(options =>
{
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<RosterDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<Microsoft.AspNetCore.Identity.SignInManager<OrganizerUser>>();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<OrganizerUser>, Roster.Infrastructure.Security.OrganizerClaimsPrincipalFactory>();

// Behind a reverse proxy (Container Apps ingress) the client address and the scheme arrive in X-Forwarded-* headers.
// Only trust them when the deployment says it sits behind such a proxy: otherwise anyone could spoof their address.
if (builder.Configuration.GetValue<bool>("Security:TrustForwardedHeaders"))
{
    builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
            | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var rateLimits = builder.Configuration.GetSection(Roster.Web.Security.RateLimitOptions.SectionName).Get<Roster.Web.Security.RateLimitOptions>() ?? new();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = Roster.Web.Security.RateLimiting.Create(rateLimits);
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        return ValueTask.CompletedTask;
    };
});

// Plugins
builder.Services.AddSingleton<IElementSourcePlugin, CsvElementSourcePlugin>();
builder.Services.AddSessionizePlugin();

// Jobs
builder.Services.AddHostedService<Roster.Web.Jobs.ResyncBackgroundJob>();

// Domain Packs
builder.Services.AddSingleton<Roster.DomainPacks.IDomainPackLoader, Roster.DomainPacks.DomainPackLoader>();

// Add services to the container.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped<Roster.Web.Client.Services.ParticipantAuthState>();
builder.Services.AddScoped(sp =>
{
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    var request = accessor.HttpContext?.Request;
    var uri = request != null ? $"{request.Scheme}://{request.Host.Value}/" : "https://localhost/";
    return new HttpClient { BaseAddress = new Uri(uri) };
});

var app = builder.Build();

// Brand first (from the host), then the language: the person's choice (cookie) wins, otherwise the brand's default.
if (app.Configuration.GetValue<bool>("Security:TrustForwardedHeaders"))
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<Roster.Web.Security.SecurityHeadersMiddleware>(
    (IReadOnlyCollection<string>)Roster.Web.Security.ContentSecurityPolicy.ConnectSourcesFor(azureSignalRConnectionString).ToList());
app.UseMiddleware<BrandMiddleware>();

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
};
localizationOptions.AddSupportedCultures(CultureEndpoints.SupportedCultures);
localizationOptions.AddSupportedUICultures(CultureEndpoints.SupportedCultures);
localizationOptions.RequestCultureProviders.Clear();
localizationOptions.RequestCultureProviders.Add(new CookieRequestCultureProvider());
localizationOptions.RequestCultureProviders.Add(new BrandCultureProvider(app.Services.GetRequiredService<BrandResolver>()));

app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
// Friendly pages for browsers only: an API answer (401, 404, 409...) must reach the client as it is. Re-executing a POST
// against a page would turn it into a misleading 400.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments("/hubs")
        && !context.Request.Path.StartsWithSegments("/culture"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Roster.Web.Client._Imports).Assembly);

app.MapAuthEndpoints();
app.MapAccountEndpoints();
app.MapGameActionEndpoints();
app.MapRefereeEndpoints();
app.MapParticipantEndpoints();

app.MapGameEndpoints();
app.MapPwaEndpoints();
app.MapCultureEndpoints();
app.MapScoreEndpoints();
app.MapHub<Roster.Web.Hubs.GameHub>("/hubs/game");

app.Run();


public partial class Program { }



