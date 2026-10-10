using System.Globalization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Roster.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register client-side services here
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddScoped<ParticipantAuthState>();
builder.Services.AddScoped<OfflineScoreStore>();
builder.Services.AddScoped<GameHubClient>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var host = builder.Build();

// Same supported cultures as the server; the browser language picks one, English is the fallback.
var browserLanguage = await host.Services.GetRequiredService<IJSRuntime>().InvokeAsync<string>("rosterGetBrowserLanguage");
var culture = new CultureInfo(browserLanguage.StartsWith("it", StringComparison.OrdinalIgnoreCase) ? "it" : "en");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();
