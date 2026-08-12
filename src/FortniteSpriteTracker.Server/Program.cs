using FortniteSpriteTracker.Server.Data;
using FortniteSpriteTracker.Server.Endpoints;
using FortniteSpriteTracker.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var databaseConnectionString = builder.Configuration.GetConnectionString("sprite-tracker")
    ?? throw new InvalidOperationException(
        "The 'ConnectionStrings:sprite-tracker' configuration value is required.");

builder.Services.AddDbContext<SpriteTrackerDbContext>(options =>
    options.UseNpgsql(
        databaseConnectionString,
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));
builder.Services
    .AddDataProtection()
    .SetApplicationName("FortniteSpriteTracker")
    .PersistKeysToDbContext<SpriteTrackerDbContext>();
builder.Services.AddHostedService<DatabaseInitializer>();
builder.Services.AddScoped<CurrentUserService>();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleAuthenticationConfigured =
    !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret);

var authentication = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "sprite-scout-auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.LoginPath = "/auth/login";
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

if (googleAuthenticationConfigured)
{
    authentication.AddGoogle(options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.Events.OnRemoteFailure = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("GoogleAuthentication");
            logger.LogError(context.Failure, "Google authentication callback failed.");
            context.Response.Redirect("/auth/error");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });
}

builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/error", (HttpContext context) =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (exception is not null)
    {
        context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("UnhandledException")
            .LogError(exception, "An unhandled request exception occurred.");
    }

    return Results.Problem(
        title: "The request could not be completed.",
        detail: "Check the live application logs for the underlying error.",
        statusCode: StatusCodes.Status500InternalServerError);
}).AllowAnonymous();

app.MapGet("/auth/error", () => Results.Problem(
    title: "Google sign-in could not be completed.",
    detail: "The failure was written to the live application logs.",
    statusCode: StatusCodes.Status500InternalServerError)).AllowAnonymous();

app.MapAuthenticationEndpoints(googleAuthenticationConfigured);
app.MapProfileEndpoints();
app.MapCollectionEndpoints();
app.MapDefaultEndpoints();
app.MapFallbackToFile("index.html");

app.Run();
