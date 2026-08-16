using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FortniteSpriteTracker.DataAccess.Seeding;

namespace FortniteSpriteTracker.DataAccess;

public static class DataAccessServiceCollectionExtensions
{
    public static IServiceCollection AddSpriteTrackerDataAccess(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SpriteTrackerDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));
        services.AddHostedService<DatabaseInitializer>();
        services.AddScoped<CatalogSeeder>();

        return services;
    }
}
