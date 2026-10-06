using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SpriteScout.Auth;

internal sealed class GoogleAuthEvents(
    AuthIdentityService accounts,
    IEnumerable<IAccountProfileProvisioner> profileProvisioners,
    AuthOptions settings) : OAuthEvents
{
    public override async Task CreatingTicket(OAuthCreatingTicketContext context)
    {
        var subject = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Google did not return a subject.");
        var cancellationToken = context.HttpContext.RequestAborted;
        var accountId = await accounts.GetOrCreateGoogleAsync(subject, cancellationToken);
        var displayName = context.Principal?.FindFirstValue(ClaimTypes.Name);
        foreach (var provisioner in profileProvisioners)
        {
            await provisioner.GoogleSignedInAsync(accountId, subject, displayName, cancellationToken);
        }

        var identity = new ClaimsIdentity(AuthDefaults.SessionScheme);
        identity.AddClaim(new Claim(Claims.Subject, accountId.ToString()));
        context.Principal = new ClaimsPrincipal(identity);
    }

    public override Task RemoteFailure(RemoteFailureContext context)
    {
        var retry = AuthPages.SafeRetry(settings, context.Properties?.RedirectUri);
        var errorUrl = new Uri(new Uri(settings.Issuer), "error").AbsoluteUri;
        context.Response.Redirect(QueryHelpers.AddQueryString(errorUrl, "returnUrl", retry));
        context.HandleResponse();
        return Task.CompletedTask;
    }
}
