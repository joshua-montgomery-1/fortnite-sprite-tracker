using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace SpriteScout.Auth;

public static class AuthPages
{
    public static IResult WebsiteLogin(HttpContext context, IAntiforgery antiforgery, string returnUrl, bool available) =>
        new AuthPageResult(new(AuthPageKind.WebsiteLogin)
        {
            Available = available,
            Form = available ? Form(context, antiforgery, [new("returnUrl", returnUrl)]) : null
        }, available ? 200 : 503);

    public static IResult WebsiteError(HttpContext context) => Error(context, "/auth/login");

    public static AuthFormModel Form(
        HttpContext context, IAntiforgery antiforgery, IEnumerable<AuthFormField> fields, bool consent = false)
    {
        var token = antiforgery.GetAndStoreTokens(context);
        return new(context.Request.Path, [.. fields, new(token.FormFieldName, token.RequestToken!)], consent);
    }

    internal static IResult Login(HttpContext context, string? application, AuthFormModel? form, bool available = true) =>
        new AuthPageResult(new(AuthPageKind.Login) { Application = application, Form = form, Available = available },
            available ? 200 : 503);

    internal static IResult Error(HttpContext context, string retry, bool expired = false) =>
        new AuthPageResult(new(AuthPageKind.Error) { Retry = retry, Expired = expired }, 400);

    internal static IResult SignedIn(HttpContext context) => new AuthPageResult(new(AuthPageKind.SignedIn));

    internal static IResult Consent(HttpContext context, string application, bool offline, AuthFormModel form, string redirectUri) =>
        new AuthPageResult(new(AuthPageKind.Consent) { Application = application, Offline = offline, Form = form },
            formRedirect: redirectUri);

    public static string SafeRetry(AuthOptions settings, string? candidate)
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
}
