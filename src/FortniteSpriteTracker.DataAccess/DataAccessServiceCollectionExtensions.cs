using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
