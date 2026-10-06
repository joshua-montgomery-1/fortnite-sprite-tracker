using Microsoft.AspNetCore.Http;

namespace SpriteScout.Auth;

// The host supplies the document shell for protocol responses and failed form posts.
public interface IAuthPageRenderer
{
    Task RenderAsync(HttpContext context, AuthPageModel model, int status, string? validatedRedirect);
}
