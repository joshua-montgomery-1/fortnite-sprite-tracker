using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace SpriteScout.Auth.Tests;

public sealed class AuthRegistrationTests
{
    [Fact]
    public void Production_requires_public_urls_and_explicit_credentials()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var exception = Assert.Throws<InvalidOperationException>(() => Register(configuration));
        Assert.Contains("loopback", exception.Message);

        configuration = Configuration(new Dictionary<string, string?>());
        exception = Assert.Throws<InvalidOperationException>(() => Register(configuration));
        Assert.Contains("SigningPath", exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Production_reuses_the_configured_certificates_across_startups(bool useBase64)
    {
        var signingPath = Path.GetTempFileName();
        var encryptionPath = Path.GetTempFileName();
        try
        {
            WriteCertificate(signingPath, "signing");
            WriteCertificate(encryptionPath, "encryption");
            var configuration = Configuration(new Dictionary<string, string?>
            {
                ["SpriteScoutAuth:Certificates:" + (useBase64 ? "SigningBase64" : "SigningPath")] =
                    useBase64 ? Convert.ToBase64String(File.ReadAllBytes(signingPath)) : signingPath,
                ["SpriteScoutAuth:Certificates:SigningPassword"] = "test-only-password",
                ["SpriteScoutAuth:Certificates:" + (useBase64 ? "EncryptionBase64" : "EncryptionPath")] =
                    useBase64 ? Convert.ToBase64String(File.ReadAllBytes(encryptionPath)) : encryptionPath,
                ["SpriteScoutAuth:Certificates:EncryptionPassword"] = "test-only-password"
            });
            using var first = Register(configuration).BuildServiceProvider();
            using var second = Register(configuration).BuildServiceProvider();
            var firstOptions = first.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
            var secondOptions = second.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
            Assert.Equal(firstOptions.SigningCredentials.Single().Key.KeyId,
                secondOptions.SigningCredentials.Single().Key.KeyId);
            Assert.Equal(firstOptions.EncryptionCredentials.Single().Key.KeyId,
                secondOptions.EncryptionCredentials.Single().Key.KeyId);
        }
        finally
        {
            File.Delete(signingPath);
            File.Delete(encryptionPath);
        }
    }

    [Fact]
    public void Production_rejects_malformed_base64_without_echoing_the_value()
    {
        const string secret = "not-a-valid-base64-secret";
        var exception = Assert.Throws<InvalidOperationException>(() => Register(Configuration(new()
        {
            ["SpriteScoutAuth:Certificates:SigningBase64"] = secret
        })));
        Assert.Contains("SigningBase64", exception.Message);
        Assert.DoesNotContain(secret, exception.ToString());
    }

    [Fact]
    public void Production_rejects_ambiguous_certificate_sources()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Register(Configuration(new()
        {
            ["SpriteScoutAuth:Certificates:SigningPath"] = "unused.pfx",
            ["SpriteScoutAuth:Certificates:SigningBase64"] = "unused"
        })));
        Assert.Contains("exactly one", exception.Message);
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values)
    {
        values["SpriteScoutAuth:Issuer"] = "https://auth.spritescout.example/identity/";
        values["SpriteScoutAuth:Resource"] = "https://spritescout.example/mcp";
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static ServiceCollection Register(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        var environment = new ProductionEnvironment();
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddLogging();
        services.AddAuth(configuration, environment, "Host=localhost;Database=unused");
        return services;
    }

    private static void WriteCertificate(string path, string purpose)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN=auth-test-{purpose}", key,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, "test-only-password"));
    }
    private sealed class ProductionEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = nameof(AuthRegistrationTests);
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
