using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Roster.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register client-side services here
builder.Services.AddScoped<ParticipantAuthState>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
