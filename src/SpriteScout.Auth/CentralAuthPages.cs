using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace SpriteScout.Auth;

internal static class CentralAuthPages
{
    private static string Encode(string value) => HtmlEncoder.Default.Encode(value);

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

    private static IResult Page(HttpContext context, string title, string heading, string body, int status = 200)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        context.Response.Headers.ContentSecurityPolicy =
            $"default-src 'none'; style-src 'nonce-{nonce}'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Results.Content($$"""
            <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{Encode(title)}}</title><style nonce="{{nonce}}">
            :root { color-scheme:dark; font-family:system-ui,-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif; background:#101c26; color:#f0f5f5; }
            * { box-sizing:border-box; } body { margin:0; min-height:100vh; display:grid; place-items:center; padding:24px; background:radial-gradient(ellipse at top,#193d43,transparent 65%),#101c26; }
            main { width:100%; max-width:460px; padding:36px; border:1px solid #39505c; border-radius:24px; background:#14232e; box-shadow:0 24px 80px #0005; }
            .brand { display:flex; align-items:center; gap:10px; font-weight:800; letter-spacing:.02em; color:#94ebc2; margin-bottom:32px; }
            .mark { font-size:26px; } h1 { font-size:28px; line-height:1.2; margin:0 0 18px; } h2 { font-size:15px; margin:0 0 8px; }
            p { line-height:1.6; margin:0 0 16px; } .intro { color:#d2dee4; } .note { font-size:13px; color:#aebfc9; margin:20px 0 0; }
            .notice { padding:18px; border-radius:12px; background:#203440; margin:24px 0; } .notice p:last-child { margin:0; }
            form { display:grid; gap:12px; } .button,button { display:block; width:100%; padding:14px 18px; border:1px solid transparent; border-radius:12px; font:inherit; font-weight:700; text-align:center; text-decoration:none; background:#94ebc2; color:#102c26; cursor:pointer; }
            .secondary { background:transparent; color:#d2dee4; border-color:#526a78; } .button:hover,button:hover { filter:brightness(1.08); } :focus-visible { outline:3px solid #fff; outline-offset:4px; }
            @media(max-width:480px) { main { padding:28px 24px; } }
            </style></head><body><main aria-labelledby="page-heading"><div class="brand"><span class="mark" aria-hidden="true">✦</span>SPRITE SCOUT</div>
            <h1 id="page-heading">{{Encode(heading)}}</h1>{{body}}</main></body></html>
            """, "text/html; charset=utf-8", statusCode: status);
    }
}
