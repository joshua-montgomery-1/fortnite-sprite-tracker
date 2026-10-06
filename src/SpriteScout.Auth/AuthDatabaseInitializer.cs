using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SpriteScout.Auth;

public sealed class AuthDatabaseInitializer(IServiceScopeFactory scopeFactory, AuthOptions settings) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync(cancellationToken);
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        foreach (var client in settings.Clients)
        {
            await RegisterClientAsync(manager, client, cancellationToken);
        }
    }

    private async Task RegisterClientAsync(
        IOpenIddictApplicationManager manager, McpClientOptions client, CancellationToken cancellationToken)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            DisplayName = client.DisplayName,
            ApplicationType = client.ApplicationType,
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Explicit,
            Permissions =
            {
                Permissions.Endpoints.Authorization, Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode, Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code, Permissions.Prefixes.Scope + AuthDefaults.AccountReadScope,
                Permissions.Prefixes.Scope + AuthDefaults.AccountWriteScope,
                Permissions.Prefixes.Scope + AuthDefaults.CollectionReadScope,
                Permissions.Prefixes.Scope + AuthDefaults.CollectionWriteScope,
                Permissions.Prefixes.Resource + settings.Resource
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        };
        foreach (var redirectUri in client.RedirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(redirectUri, UriKind.Absolute));
        }
        var existing = await manager.FindByClientIdAsync(descriptor.ClientId, cancellationToken);
        if (existing is null)
            await manager.CreateAsync(descriptor, cancellationToken);
        else
            await manager.UpdateAsync(existing, descriptor, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
