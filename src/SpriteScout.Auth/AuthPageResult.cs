using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SpriteScout.Auth;

internal sealed class AuthPageResult(AuthPageModel model, int status = 200, string? formRedirect = null) : IResult
{
    public Task ExecuteAsync(HttpContext context) => context.RequestServices
        .GetRequiredService<IAuthPageRenderer>().RenderAsync(context, model, status, formRedirect);
}
