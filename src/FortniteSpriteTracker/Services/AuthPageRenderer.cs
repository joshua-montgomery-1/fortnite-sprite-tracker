using FortniteSpriteTracker.Components;
using Microsoft.AspNetCore.Http.HttpResults;
using SpriteScout.Auth;

namespace FortniteSpriteTracker.Server.Services;

public sealed class AuthPageRenderer : IAuthPageRenderer
{
    public Task RenderAsync(HttpContext context, AuthPageModel model, int status, string? validatedRedirect) =>
        new RazorComponentResult<App>(new { AuthPage = model, AuthRedirect = validatedRedirect })
        {
            StatusCode = status,
            PreventStreamingRendering = true
        }.ExecuteAsync(context);
}
