using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SpriteScout.Auth;

public static class AuthPages
{
    private static string Encode(string value) => HtmlEncoder.Default.Encode(value);

    public static IResult WebsiteLogin(HttpContext context, IAntiforgery antiforgery, string returnUrl, bool available)
    {
        var form = available ? Form(context, antiforgery,
            $"<input type=\"hidden\" name=\"returnUrl\" value=\"{Encode(returnUrl)}\">",
            "<button>Continue with Google <span aria-hidden=\"true\">↗</span></button>") : null;
        return Page(context, "Sign in to Sprite Scout", "Your next find starts here.",
            "<p class=\"intro\">Keep your field guide close. Sign in to save your Sprite collection and pick up where you left off, on any device.</p>" +
            (available ? form + "<p class=\"note\">A free account. Your collection, wherever you play.</p>" :
                "<p class=\"notice\" role=\"status\">Sign-in is unavailable right now. Please try again later.</p>"), available ? 200 : 503);
    }

    public static IResult WebsiteError(HttpContext context) => Error(context, "/auth/login");

    internal static string Form(HttpContext context, IAntiforgery antiforgery, string fields, string buttons)
    {
        var token = antiforgery.GetAndStoreTokens(context);
        return $"""
            <form method="post" action="{Encode(context.Request.Path)}" data-enhance="false">
            {fields}<input type="hidden" name="{Encode(token.FormFieldName)}" value="{Encode(token.RequestToken!)}">
            {buttons}</form>
            """;
    }

    internal static IResult Login(HttpContext context, string? application, string? form, bool available = true)
    {
        var introduction = application is null ? "Your Sprite Scout account starts here." :
            $"Sign in to connect Sprite Scout to <strong>{Encode(application)}</strong>.";
        var body = $"<p class=\"intro\">{introduction}</p>" + (available ?
            $"{form}<p class=\"note\">Use your Google account to sign in or create a free Sprite Scout account. You'll review permissions before connecting an application.</p>" :
            "<p class=\"notice\" role=\"status\">Sign-in is unavailable right now. Please try again later.</p>");
        return Page(context, "Sign in to Sprite Scout", "Welcome to Sprite Scout", body, available ? 200 : 503);
    }

    internal static IResult Error(HttpContext context, string retry, bool expired = false) => Page(context,
        "Sign-in needs another try", "Let's try that again", $"""
        <p class="intro" role="alert">{(expired ? "The consent form expired. Start sign-in again." : "Google sign-in couldn't be completed. Your connection hasn't been approved.")}</p>
        <a class="button" href="{Encode(retry)}">Try again</a>
        """, 400);

    internal static IResult SignedIn(HttpContext context) => Page(context, "Signed in to Sprite Scout",
        "You're signed in", "<p class=\"intro\">Return to the application you want to connect to Sprite Scout. You'll review its permissions before granting access.</p>");

    internal static IResult Consent(HttpContext context, string application, bool offline, string form, string redirectUri) => Page(context,
        "Connect to Sprite Scout", "Approve your connection", $"""
        <p class="intro"><strong>{Encode(application)}</strong> wants to connect to your Sprite Scout account.</p>
        <div class="notice"><h2>Account access</h2><p>Read your display name and public profile identifier.</p>
        <p>This connection cannot change your collection.</p>
        {(offline ? "<p>It can stay connected for up to 30 days.</p>" : "")}</div>
        {form}
        """, formRedirect: redirectUri);

    internal static string SafeRetry(AuthOptions settings, string? candidate)
    {
        var issuer = new Uri(settings.Issuer);
        if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri) &&
            uri.Scheme == issuer.Scheme && uri.Authority == issuer.Authority &&
            uri.UserInfo.Length == 0 && uri.Fragment.Length == 0 &&
            (uri.AbsolutePath == new Uri(issuer, "connect/authorize").AbsolutePath ||
             uri.AbsolutePath == new Uri(issuer, "login").AbsolutePath))
            return uri.AbsoluteUri;
        return new Uri(issuer, "login").AbsoluteUri;
    }

    private static IResult Page(HttpContext context, string title, string heading, string body, int status = 200, string? formRedirect = null) =>
        new ArtworkPage(title, heading, body, status, formRedirect);

    private sealed record ArtworkPage(string Title, string Heading, string Body, int Status, string? FormRedirect) : IResult
    {
        public async Task ExecuteAsync(HttpContext context)
        {
            var source = context.RequestServices.GetService<IAuthArtworkSource>();
            var artwork = source is null ? [] : await source.GetAsync(context.RequestAborted);
            await Render(context, Title, Heading, Body, Status, artwork, FormRedirect).ExecuteAsync(context);
        }
    }

    private static IResult Render(HttpContext context, string title, string heading, string body, int status, IReadOnlyList<AuthArtwork> artwork, string? formRedirect)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        var groups = artwork.GroupBy(item => item.SeasonId).Select(group =>
        {
            var paths = group.Select(item => item.ImagePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme == "https" ? uri.AbsoluteUri : "/" + path.TrimStart('/'))
                .Distinct().ToArray();
            Random.Shared.Shuffle(paths);
            return paths;
        }).ToArray();
        Random.Shared.Shuffle(groups);
        var selected = new HashSet<string>();
        for (var round = 0; selected.Count < 10 && groups.Any(group => round < group.Length); round++)
            foreach (var group in groups)
                if (round < group.Length && selected.Count < 10) selected.Add(group[round]);
        var sprites = selected.ToArray();
        Random.Shared.Shuffle(sprites);
        var scenery = string.Join("", sprites.Select((file, index) =>
            $"<div class=\"sprite-layer layer-{index}\"><img src=\"{Encode(file)}\" alt=\"\" decoding=\"async\"></div>"));
        var positions = new List<(int X, int Y)>();
        var variation = string.Join("", sprites.Select((_, index) =>
        {
            // Scatter through the whole scene, allowing some artwork behind the card.
            // A small minimum distance prevents an accidental pile of sprites.
            var point = (X: Random.Shared.Next(2, 99), Y: Random.Shared.Next(8, 98));
            for (var attempt = 0; attempt < 30 && positions.Any(existing =>
                Math.Pow(existing.X - point.X, 2) + Math.Pow(existing.Y - point.Y, 2) < 225); attempt++)
                point = (Random.Shared.Next(2, 99), Random.Shared.Next(8, 98));
            positions.Add(point);
            var scale = Random.Shared.Next(72, 106);
            return $".layer-{index} {{ left:{point.X}%; top:{point.Y}%; --size:{Random.Shared.Next(15, 25)}vw; --tilt:{Random.Shared.Next(-12, 13)}deg; --depth:{Random.Shared.Next(-200, 51)}px; --travel-z:{Random.Shared.Next(45, 111)}px; --turn:{Random.Shared.Next(-12, 13)}deg; --scale-from:{scale}; --scale-to:{scale + Random.Shared.Next(14, 29)}; --travel-x:{Random.Shared.Next(-25, 26)}px; --travel-y:{Random.Shared.Next(-30, 31)}px; animation-duration:{Random.Shared.Next(18, 31)}s; animation-delay:-{Random.Shared.Next(0, 30)}s; }}";
        }));
        var imageOrigins = string.Join(" ", sprites.Select(path => Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme == "https" ? uri.GetLeftPart(UriPartial.Authority) : "").Distinct());
        // Consent receives only the callback already validated by OpenIddict.
        var callbackOrigin = Uri.TryCreate(formRedirect, UriKind.Absolute, out var callback) && callback.Scheme is "http" or "https"
            ? callback.GetLeftPart(UriPartial.Authority) : "";
        context.Response.Headers.ContentSecurityPolicy =
            $"default-src 'none'; style-src 'self' 'nonce-{nonce}'; script-src 'nonce-{nonce}'; img-src 'self' {imageOrigins}; font-src 'self'; form-action 'self' https://accounts.google.com {callbackOrigin}; frame-ancestors 'none'; base-uri 'none'";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Results.Content($$"""
            <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{Encode(title)}}</title>
            <script nonce="{{nonce}}">(()=>{let p='system';try{p=JSON.parse(localStorage.getItem('sprite-scout-theme-preference'))||p}catch{}if(!['system','light','dark'].includes(p))p='system';const m=matchMedia('(prefers-color-scheme: dark)');const apply=()=>document.documentElement.dataset.theme=p==='system'?(m.matches?'dark':'light'):p;apply();m.addEventListener('change',apply)})();</script>
            <link rel="stylesheet" href="/css/app.css"><style nonce="{{nonce}}">
            body.auth-page { position:relative; min-height:100svh; display:grid; place-items:center; padding:90px 24px 64px; isolation:isolate; background:var(--paper); }
            .auth-nav { position:absolute; inset:0 0 auto; padding:25px 5vw; display:flex; align-items:center; justify-content:space-between; z-index:3; }
            .back { font:11px var(--font-mono); color:var(--text-muted); text-decoration:none; }
            .scenery { position:fixed; inset:0; z-index:-1; overflow:hidden; pointer-events:none; perspective:1000px; background:radial-gradient(ellipse at 75% 15%,color-mix(in srgb,var(--purple) 12%,transparent),transparent 55%),radial-gradient(ellipse at 20% 85%,color-mix(in srgb,var(--lime) 20%,transparent),transparent 50%); }
            .scenery::before { content:""; position:absolute; inset:0; background-image:radial-gradient(var(--hero-grid) 1px,transparent 1px); background-size:22px 22px; mask-image:radial-gradient(ellipse at center,transparent 22%,#000 80%); }
            .sprite-layer { position:absolute; translate:-50% -50%; width:clamp(120px,var(--size,19vw),300px); transform-style:preserve-3d; animation:drift 24s ease-in-out infinite alternate; }
            .sprite-layer img { display:block; width:100%; height:auto; opacity:.22; filter:saturate(.8) drop-shadow(0 28px 20px color-mix(in srgb,var(--purple) 25%,transparent)); mask-image:radial-gradient(ellipse,#000 30%,transparent 85%); }
            {{variation}}
            @keyframes drift { from { transform:translate3d(0,0,var(--depth)) rotateY(var(--tilt)) rotateZ(var(--turn)) scale(calc(var(--scale-from) / 100)); } to { transform:translate3d(var(--travel-x),var(--travel-y),calc(var(--depth) + var(--travel-z))) rotateY(calc(var(--tilt) + 8deg)) rotateZ(calc(var(--turn) + 8deg)) scale(calc(var(--scale-to) / 100)); } }
            .auth-card { width:min(100%,470px); padding:38px; border:1px solid var(--border); border-radius:14px; background:color-mix(in srgb,var(--surface) 94%,transparent); backdrop-filter:blur(18px); box-shadow:8px 8px 0 color-mix(in srgb,var(--lime) 60%,transparent),0 24px 70px var(--shadow); }
            .kicker { font:10px var(--font-mono); letter-spacing:1.5px; color:var(--purple); margin:0 0 22px; }
            .auth-card h1 { font-size:clamp(32px,4vw,44px); font-weight:950; letter-spacing:-2px; line-height:1.05; margin:0 0 22px; text-wrap:balance; }
            .auth-card h2 { font-size:14px; margin:0 0 8px; }
            .auth-card p { line-height:1.7; margin:0 0 20px; } .intro { color:var(--text-muted); font-size:15px; }
            .auth-card .note { font-size:12px; color:var(--text-muted); margin:24px 0 0; }
            .notice { padding:18px; border:1px solid var(--border); border-radius:8px; background:var(--surface-muted); margin:24px 0; } .notice p:last-child { margin:0; }
            form { display:grid; gap:14px; } .button,button { display:block; width:100%; padding:15px 18px; border:1px solid transparent; border-radius:7px; font:inherit; font-size:14px; font-weight:800; text-align:center; text-decoration:none; background:var(--action-bg); color:var(--action-text); box-shadow:4px 4px 0 var(--lime); cursor:pointer; transition:translate .2s,box-shadow .2s; }
            .secondary { background:transparent; color:var(--ink); border-color:var(--border); box-shadow:none; } .button:hover,button:hover { translate:0 -2px; } :focus-visible { outline:3px solid var(--purple); outline-offset:5px; }
            .auth-footer { position:absolute; bottom:22px; padding:0; margin:0; border:0; background:none; font:10px var(--font-mono); color:var(--text-muted); letter-spacing:1px; }
            @media(max-width:600px) { .auth-card { padding:28px 24px; } .sprite-layer { width:32vw; } .back { font-size:10px; } }
            @media(prefers-reduced-motion:reduce) { .sprite-layer { animation:none; transform:translateZ(var(--depth)) rotateY(var(--tilt)) rotateZ(var(--turn)) scale(calc(var(--scale-from) / 100)); } .button,button { transition:none; } }
            @media(forced-colors:active) { .scenery { display:none; } .auth-card { background:Canvas; border:1px solid CanvasText; } }
            </style></head><body class="auth-page"><header class="auth-nav"><a class="brand" href="/" aria-label="Sprite Scout home"><span class="brandmark" aria-hidden="true">S</span><span>SPRITE<em>SCOUT</em></span></a><a class="back" href="/">← BACK TO FIELD GUIDE</a></header>
            <div class="scenery" aria-hidden="true">{{scenery}}</div><main class="auth-card" aria-labelledby="page-heading"><div class="kicker">YOUR SPRITE SCOUT ACCOUNT</div>
            <h1 id="page-heading">{{Encode(heading)}}</h1>{{body}}</main><footer class="auth-footer">SCOUT. COLLECT. COMPLETE.</footer></body></html>
            """, "text/html; charset=utf-8", statusCode: status);
    }
}
