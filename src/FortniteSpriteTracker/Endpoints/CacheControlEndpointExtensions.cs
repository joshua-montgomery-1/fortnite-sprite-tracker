using Microsoft.Net.Http.Headers;

namespace FortniteSpriteTracker.Server.Endpoints;

public static class CacheControlEndpointExtensions
{
    public static RouteHandlerBuilder WithPublicCache(
        this RouteHandlerBuilder builder,
        TimeSpan maxAge)
    {
        builder.AddEndpointFilter(async (context, next) =>
        {
            var result = await next(context);
            context.HttpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = maxAge
            };
            return result;
        });
        return builder;
    }
}
