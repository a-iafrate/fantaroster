using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Roster.Application.Ports;
using Roster.Application.Services;
using Roster.Infrastructure;
using Roster.Infrastructure.Data;
using Roster.Plugins.Abstractions;
using Roster.Plugins.Csv;
using Roster.Plugins.Sessionize;
using Roster.Web.Components;
using Roster.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<Roster.Web.Options.BrandOptions>(builder.Configuration.GetSection(Roster.Web.Options.BrandOptions.SectionName));

builder.Services.AddDbContext<RosterDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("sqldb")));

// Application
builder.Services.AddScoped<IResyncService, ResyncService>();
builder.Services.AddScoped<IGameCreationService, GameCreationService>();
builder.Services.AddScoped<GameLifecycleService>();
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

var azureSignalRConnectionString = builder.Configuration["Azure:SignalR:ConnectionString"];

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

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("magic-link", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter("magic-link", _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
    {
        PermitLimit = 3,
        Window = TimeSpan.FromMinutes(1)
    }));
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

var supportedCultures = new[] { "en", "it" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
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
app.MapRefereeEndpoints();
app.MapParticipantEndpoints();

app.MapGameEndpoints();
app.MapPwaEndpoints();
app.MapScoreEndpoints();
app.MapHub<Roster.Web.Hubs.GameHub>("/hubs/game");

app.Run();


public partial class Program { }



