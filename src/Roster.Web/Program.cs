using Microsoft.EntityFrameworkCore;
using Roster.Application.Ports;
using Roster.Application.Services;
using Roster.Infrastructure;
using Roster.Infrastructure.Data;
using Roster.Plugins.Abstractions;
using Roster.Plugins.Csv;
using Roster.Plugins.Sessionize;
using Roster.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<RosterDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("sqldb")));

// Application
builder.Services.AddScoped<IResyncService, ResyncService>();
builder.Services.AddSingleton(TimeProvider.System);

// Infrastructure
builder.Services.AddInfrastructure();

// Plugins
builder.Services.AddSingleton<IElementSourcePlugin, CsvElementSourcePlugin>();
builder.Services.AddSessionizePlugin();

// Jobs
builder.Services.AddHostedService<Roster.Web.Jobs.ResyncBackgroundJob>();

// Domain Packs
builder.Services.AddSingleton<Roster.DomainPacks.IDomainPackLoader, Roster.DomainPacks.DomainPackLoader>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
