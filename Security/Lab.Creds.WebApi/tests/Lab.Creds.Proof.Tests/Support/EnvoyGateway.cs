using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Lab.Creds.Proof.Tests.Support;

// Envoy runs as an isolated container (pinned release and image digest) in front of the Gateway-facing API.
public sealed class EnvoyGateway : IAsyncDisposable
{
    // Official release v1.39.3; the digest is the multi-platform image index published for that tag.
    public const string Image = "envoyproxy/envoy:v1.39.3@sha256:dd85940439de19a0b6ae8419610363ea0ad351d9a994ea007161c206ec1e1865";

    private const int IngressPort = 10000;
    private IContainer? _container;

    public Uri Uri { get; private set; } = null!;

    public static async Task<string> RenderConfigAsync(
        TestCertificates certificates, int apiPort, IEnumerable<X509Certificate2> allowedClients, CancellationToken cancellationToken)
    {
        var template = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Support", "envoy.template.yaml"), cancellationToken);
        var allowlist = string.Join(Environment.NewLine,
            new[] { "              verify_certificate_hash:" }
                .Concat(allowedClients.Select(c => $"              - \"{Convert.ToHexString(SHA256.HashData(c.RawData))}\"")));

        return template
            .Replace("{{SERVER_CERT_PEM}}", Indent(certificates.Server.ExportCertificatePem(), 18))
            .Replace("{{SERVER_KEY_PEM}}", Indent(PrivateKeyPem(certificates.Server), 18))
            .Replace("{{GATEWAY_CERT_PEM}}", Indent(certificates.Gateway.ExportCertificatePem(), 16))
            .Replace("{{GATEWAY_KEY_PEM}}", Indent(PrivateKeyPem(certificates.Gateway), 16))
            .Replace("{{ROOT_PEM}}", Indent(certificates.Root.ExportCertificatePem(), 16))
            .Replace("{{CLIENT_ALLOWLIST}}", allowlist)
            .Replace("{{API_PORT}}", apiPort.ToString());
    }

    public async Task StartAsync(string config, CancellationToken cancellationToken)
    {
        _container = new ContainerBuilder(Image)
            .WithExtraHost("host.docker.internal", "host-gateway")
            .WithPortBinding(IngressPort, true)
            .WithResourceMapping(System.Text.Encoding.UTF8.GetBytes(config), "/etc/envoy/envoy.yaml")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("starting main dispatch loop"))
            .Build();
        await _container.StartAsync(cancellationToken);
        Uri = new Uri($"https://localhost:{_container.GetMappedPublicPort(IngressPort)}");
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => _container is null ? Task.CompletedTask : _container.StopAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }

    private static string PrivateKeyPem(X509Certificate2 certificate)
        => (certificate.GetECDsaPrivateKey() ?? throw new InvalidOperationException("ECDSA key expected")).ExportPkcs8PrivateKeyPem();

    private static string Indent(string pem, int spaces)
        => string.Join(Environment.NewLine, pem.Trim().Split('\n').Select(line => new string(' ', spaces) + line.TrimEnd('\r')));
}
