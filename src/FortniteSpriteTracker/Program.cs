using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FortniteSpriteTracker;
using FortniteSpriteTracker.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<BrowserStorage>();
builder.Services.AddScoped<BrowserPrintService>();
builder.Services.AddScoped<AccountClient>();
builder.Services.AddScoped<CollectionClient>();
builder.Services.AddScoped<PlayerClient>();

await builder.Build().RunAsync();
