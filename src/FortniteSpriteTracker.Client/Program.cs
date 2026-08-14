using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FortniteSpriteTracker.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<BrowserStorage>();
builder.Services.AddScoped<BrowserPrintService>();
builder.Services.AddScoped<AccountClient>();
builder.Services.AddScoped<AccountState>();
builder.Services.AddScoped<CollectionClient>();
builder.Services.AddScoped<PlayerClient>();

await builder.Build().RunAsync();
