using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore;
using System.Net.Security;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using OpenIddict.Validation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Lab.Creds.Proof;

public static class ProofHosts
{
    public static WebApplication CreateAuthServer(AuthServerOptions options)
    {
        var builder = WebApplication.CreateBuilder();
        ConfigureKestrel(builder, options.ServerCertificate);

        builder.Services.AddSingleton(options);
        builder.Services.AddDbContextFactory<ProofDbContext>(db =>
        {
            db.UseNpgsql(options.ConnectionString);
            db.UseOpenIddict();
        });
        builder.Services.AddHostedService<ClientSeeder>();

        builder.Services.AddOpenIddict()
            .AddCore(core => core.UseEntityFrameworkCore().UseDbContext<ProofDbContext>())
            .AddServer(server =>
            {
                server.SetTokenEndpointUris("/connect/token")
                    .SetIntrospectionEndpointUris("/connect/introspect");
                server.AllowClientCredentialsFlow();
                server.RegisterScopes(ProofDefaults.SubmitScope);
                server.UseReferenceAccessTokens();
                server.UseClientCertificateBoundAccessTokens();
                server.EnableSelfSignedTlsClientAuthentication();
                server.AddEphemeralEncryptionKey().AddEphemeralSigningKey();
                server.UseAspNetCore().EnableTokenEndpointPassthrough();
            });

        var app = builder.Build();
        app.UseRouting();

        app.MapPost("/connect/token", async (HttpContext context, IOpenIddictApplicationManager applications) =>
        {
            var cancellationToken = context.RequestAborted;
            var request = context.GetOpenIddictServerRequest()!;
            var application = (await applications.FindByClientIdAsync(request.ClientId!, cancellationToken))!;

            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
            identity.SetClaim(Claims.Subject, await applications.GetClientIdAsync(application, cancellationToken));
            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());
            principal.SetResources(options.Audience);
            principal.SetClaim(Claims.ClientId, request.ClientId);
            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(Destinations.AccessToken);
            }

            return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        });

        return app;
    }

    public static WebApplication CreateApi(ApiOptions options)
    {
        var builder = WebApplication.CreateBuilder();
        ConfigureKestrel(builder, options.ServerCertificate, options.Gateway);

        builder.Services.AddOpenIddict()
            .AddValidation(validation =>
            {
                validation.SetIssuer(options.Authority);
                validation.AddAudiences(options.Audience);
                validation.AddSigningCertificate(options.IntrospectionClientCertificate);
                validation.UseIntrospection().SetClientId(options.IntrospectionClientId);
                validation.UseSystemNetHttp().ConfigureHttpClientHandler(handler =>
                    handler.ServerCertificateCustomValidationCallback = (_, certificate, _, errors) =>
                        TrustsServerCertificate(options.TrustedRoots, certificate, errors));
                validation.UseAspNetCore();
            });

        builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        builder.Services.AddAuthorizationBuilder().AddPolicy(SubmitPolicy, policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => context.User.HasScope(ProofDefaults.SubmitScope)));

        var app = builder.Build();
        if (options.Gateway is not null)
        {
            app.UseMiddleware<GatewayClientCertificateMiddleware>();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapPost("/partner/submissions", (ClaimsPrincipal user, PartnerSubmission submission) =>
                Results.Accepted(value: new { clientId = user.GetClaim(Claims.ClientId), partnerName = submission.PartnerName }))
            .RequireAuthorization(SubmitPolicy);

        // Proof-only: reports what the API itself observed (original target, authority, body hash,
        // signature-related headers and the SHA-256 x5t#S256 of the client certificate it validates against).
        app.MapPost("/partner/inspect/{**rest}", async (HttpContext http) =>
            {
                using var body = new MemoryStream();
                await http.Request.Body.CopyToAsync(body, http.RequestAborted);
                var certificate = http.Connection.ClientCertificate;
                var headers = new[] { "content-digest", "signature-input", "signature", "content-type" }
                    .Where(http.Request.Headers.ContainsKey)
                    .ToDictionary(name => name, name => http.Request.Headers[name].ToString());
                return Results.Ok(new
                {
                    host = http.Request.Host.Value,
                    rawTarget = http.Features.Get<IHttpRequestFeature>()!.RawTarget,
                    bodySha256 = Base64UrlEncoder.Encode(SHA256.HashData(body.ToArray())),
                    headers,
                    clientCertificateX5tS256 = certificate is null ? null : Base64UrlEncoder.Encode(SHA256.HashData(certificate.RawData))
                });
            })
            .RequireAuthorization(SubmitPolicy);

        return app;
    }

    private const string SubmitPolicy = "partner-submit";

    private static bool TrustsServerCertificate(
        X509Certificate2Collection trustedRoots, X509Certificate2? certificate, SslPolicyErrors errors)
    {
        if (certificate is null || (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) != SslPolicyErrors.None) return false;
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.AddRange(trustedRoots);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        return chain.Build(certificate);
    }

    // Client certificates are requested but optional at the TLS layer; trust is decided by OpenIddict
    // (registered self-signed certificate / certificate-bound token), never by skipping server validation.
    private static void ConfigureKestrel(
        WebApplicationBuilder builder, X509Certificate2 serverCertificate, GatewayTrustOptions? gateway = null)
        => builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(
            gateway is null ? IPAddress.Loopback : IPAddress.Any, 0, listen =>
            listen.UseHttps(https =>
            {
                https.ServerCertificate = serverCertificate;
                if (gateway is null)
                {
                    https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                    https.AllowAnyClientCertificate();
                    return;
                }

                // mTLS toward the Gateway: only the pinned Gateway certificate may connect.
                https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                https.ClientCertificateValidation = (certificate, _, _) =>
                    CryptographicOperations.FixedTimeEquals(
                        SHA256.HashData(certificate.RawData), SHA256.HashData(gateway.GatewayCertificate.RawData));
            })));
}
