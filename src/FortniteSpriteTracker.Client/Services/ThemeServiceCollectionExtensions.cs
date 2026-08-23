using Microsoft.Extensions.DependencyInjection;

namespace FortniteSpriteTracker.Services;

public static class ThemeServiceCollectionExtensions
{
    public static IServiceCollection AddThemeServices(this IServiceCollection services)
    {
        services.AddScoped<ThemeService>();
        return services;
    }
}
