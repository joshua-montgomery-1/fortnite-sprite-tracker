using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FortniteSpriteTracker.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddScoped<AuthenticationNavigation>();
builder.Services.AddTransient<SessionExpiredHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<SessionExpiredHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
});
builder.Services.AddScoped<BrowserStorage>();
builder.Services.AddThemeServices();
builder.Services.AddScoped<BrowserPrintService>();
builder.Services.AddScoped<AccountClient>();
builder.Services.AddScoped<AccountState>();
builder.Services.AddScoped<CollectionClient>();
builder.Services.AddScoped<PlayerClient>();
builder.Services.AddScoped<CatalogClient>();

await builder.Build().RunAsync();
