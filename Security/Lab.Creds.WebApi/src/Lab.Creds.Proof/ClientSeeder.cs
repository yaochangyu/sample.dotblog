using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Lab.Creds.Proof;

// Registers each Client identity with its own self-signed TLS certificate (public part only).
internal sealed class ClientSeeder(
    IServiceProvider services, IDbContextFactory<ProofDbContext> contextFactory, AuthServerOptions options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using (var context = await contextFactory.CreateDbContextAsync(cancellationToken))
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        await using var scope = services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var client in options.Clients)
        {
            var key = JsonWebKeyConverter.ConvertFromX509SecurityKey(new X509SecurityKey(client.PublicCertificate));
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = client.ClientId,
                ClientType = ClientTypes.Confidential,
                JsonWebKeySet = new JsonWebKeySet { Keys = { key } },
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.ClientCredentials
                }
            };

            if (client.CanIntrospect)
            {
                descriptor.Permissions.Add(Permissions.Endpoints.Introspection);
            }

            foreach (var scopeName in client.Scopes)
            {
                descriptor.Permissions.Add(Permissions.Prefixes.Scope + scopeName);
                descriptor.Permissions.Add(Permissions.Prefixes.Resource + options.Audience);
            }

            await manager.CreateAsync(descriptor, cancellationToken);
        }

        // Request-signing public keys: registered per Client and kept apart from the mTLS certificate registration.
        await using var keyContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        keyContext.SigningKeys.AddRange(options.Clients.SelectMany(client => client.SigningKeys ?? []));
        await keyContext.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
