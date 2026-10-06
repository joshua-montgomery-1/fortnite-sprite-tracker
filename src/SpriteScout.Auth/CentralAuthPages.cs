using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SpriteScout.Auth;

public static class CentralAuthPages
{
    private static readonly string[] Artwork = ["air_basic.webp", "water_basic.webp", "fire_basic.webp", "earth_basic.webp", "zeropoint_basic.webp",
        "duck_basic.webp", "fishy_basic.webp", "llama_basic.webp", "peely_basic.webp", "ghost_basic.webp"];
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
            <form method="post" action="{Encode(context.Request.Path)}">
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

    internal static IResult Consent(HttpContext context, string application, bool offline, string form) => Page(context,
        "Connect to Sprite Scout", "Approve your connection", $"""
        <p class="intro"><strong>{Encode(application)}</strong> wants to connect to your Sprite Scout account.</p>
        <div class="notice"><h2>Account access</h2><p>Read your display name and public profile identifier.</p>
        <p>This connection cannot change your collection.</p>
        {(offline ? "<p>It can stay connected for up to 30 days.</p>" : "")}</div>
        {form}
        """);

    internal static string SafeRetry(CentralAuthOptions settings, string? candidate)
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

    private static IResult Page(HttpContext context, string title, string heading, string body, int status = 200) =>
        new ArtworkPage(title, heading, body, status);

    private sealed record ArtworkPage(string Title, string Heading, string Body, int Status) : IResult
    {
        public async Task ExecuteAsync(HttpContext context)
        {
            var source = context.RequestServices.GetService<IAuthArtworkSource>();
            var artwork = source is null ? [] : await source.GetAsync(context.RequestAborted);
            await Render(context, Title, Heading, Body, Status, artwork).ExecuteAsync(context);
        }
    }

    private static IResult Render(HttpContext context, string title, string heading, string body, int status, IReadOnlyList<AuthArtwork> artwork)
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
        var sprites = selected.Count == 0 ? Artwork.Select(file => "/images/sprites/" + file).ToArray() : selected.ToArray();
        Random.Shared.Shuffle(sprites);
        var scenery = string.Join("", sprites.Select((file, index) =>
            $"<div class=\"sprite-layer layer-{index}\"><img src=\"{Encode(file)}\" alt=\"\" decoding=\"async\"></div>"));
        var variation = string.Join("", sprites.Select((_, index) =>
            $".layer-{index} {{ margin-left:{Random.Shared.Next(-28, 29)}px; margin-top:{Random.Shared.Next(-32, 33)}px; --size:{Random.Shared.Next(15, 23)}vw; --tilt:{Random.Shared.Next(-10, 11)}deg; --depth:{Random.Shared.Next(-140, 41)}px; --turn:{Random.Shared.Next(-8, 9)}deg; animation-duration:{Random.Shared.Next(14, 25)}s; animation-delay:-{Random.Shared.Next(0, 24)}s; }}"));
        var imageOrigins = string.Join(" ", sprites.Select(path => Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme == "https" ? uri.GetLeftPart(UriPartial.Authority) : "").Distinct());
        context.Response.Headers.ContentSecurityPolicy =
            $"default-src 'none'; style-src 'self' 'nonce-{nonce}'; script-src 'nonce-{nonce}'; img-src 'self' {imageOrigins}; font-src 'self'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'";
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
            .sprite-layer { position:absolute; width:clamp(120px,var(--size,19vw),280px); transform-style:preserve-3d; animation:drift 16s ease-in-out infinite alternate; }
            .sprite-layer img { display:block; width:100%; height:auto; opacity:.22; filter:saturate(.8) drop-shadow(0 28px 20px color-mix(in srgb,var(--purple) 25%,transparent)); mask-image:radial-gradient(ellipse,#000 30%,transparent 85%); }
            .layer-0 { top:9%; left:1%; --tilt:-8deg; --depth:30px; }
            .layer-1 { top:4%; right:3%; --tilt:6deg; --depth:-150px; animation-delay:-5s; filter:blur(1px); }
            .layer-2 { bottom:3%; right:2%; --tilt:-6deg; --depth:40px; animation-delay:-9s; }
            .layer-3 { bottom:5%; left:2%; --tilt:8deg; --depth:-100px; animation-delay:-13s; filter:blur(1px); }
            .layer-4 { top:36%; left:-3%; --tilt:5deg; --depth:-60px; animation-delay:-3s; }
            .layer-5 { top:37%; right:-2%; --tilt:-8deg; --depth:20px; animation-delay:-11s; }
            .layer-6 { top:-6%; left:27%; --tilt:7deg; --depth:-100px; animation-delay:-7s; }
            .layer-7 { top:-4%; right:25%; --tilt:-5deg; --depth:-50px; animation-delay:-14s; }
            .layer-8 { bottom:-8%; left:28%; --tilt:-8deg; --depth:0px; animation-delay:-2s; }
            .layer-9 { bottom:-8%; right:25%; --tilt:5deg; --depth:-100px; animation-delay:-10s; }
            {{variation}}
            @keyframes drift { from { transform:translate3d(0,0,var(--depth)) rotateY(var(--tilt)) rotateZ(var(--turn)); } to { transform:translate3d(12px,-24px,var(--depth)) rotateY(calc(var(--tilt) + 8deg)) rotateZ(calc(var(--turn) + 8deg)); } }
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
            @media(max-width:600px) { .auth-card { padding:28px 24px; } .sprite-layer { width:32vw; } .layer-4 { left:-10%; } .layer-5 { right:-10%; } .back { font-size:10px; } }
            @media(prefers-reduced-motion:reduce) { .sprite-layer { animation:none; transform:rotateY(var(--tilt)); } .button,button { transition:none; } }
            @media(forced-colors:active) { .scenery { display:none; } .auth-card { background:Canvas; border:1px solid CanvasText; } }
            </style></head><body class="auth-page"><header class="auth-nav"><a class="brand" href="/" aria-label="Sprite Scout home"><span class="brandmark" aria-hidden="true">S</span><span>SPRITE<em>SCOUT</em></span></a><a class="back" href="/">← BACK TO FIELD GUIDE</a></header>
            <div class="scenery" aria-hidden="true">{{scenery}}</div><main class="auth-card" aria-labelledby="page-heading"><div class="kicker">YOUR SPRITE SCOUT ACCOUNT</div>
            <h1 id="page-heading">{{Encode(heading)}}</h1>{{body}}</main><footer class="auth-footer">SCOUT. COLLECT. COMPLETE.</footer></body></html>
            """, "text/html; charset=utf-8", statusCode: status);
    }
}
