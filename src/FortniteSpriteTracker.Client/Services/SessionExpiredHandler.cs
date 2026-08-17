using System.Net;

namespace FortniteSpriteTracker.Services;

public sealed class SessionExpiredHandler(AuthenticationNavigation authenticationNavigation)
    : DelegatingHandler
{
    public static readonly HttpRequestOptionsKey<bool> SuppressRedirect =
        new("SpriteScout.SuppressAuthenticationRedirect");

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        var redirectSuppressed = request.Options.TryGetValue(SuppressRedirect, out var suppressRedirect)
            && suppressRedirect;

        if (response.StatusCode == HttpStatusCode.Unauthorized && !redirectSuppressed)
        {
            authenticationNavigation.SignIn();
        }

        return response;
    }
}
