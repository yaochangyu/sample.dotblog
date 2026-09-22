using System.Text.RegularExpressions;
using Lab.API.Signature.Services;

namespace Lab.API.Signature.Tests.Services;

public class ApiKeyGeneratorTests
{
    private static readonly Regex ApiKeyPattern = new(@"^key-[0-9a-f]{32}$");
    private static readonly Regex SecretPattern = new(@"^[0-9a-f]{64}$");

    [Fact]
    public void GenerateApiKey_ReturnsKeyPrefixWithThirtyTwoCharLowercaseHex()
    {
        var generator = new ApiKeyGenerator();

        var apiKey = generator.GenerateApiKey();

        Assert.Matches(ApiKeyPattern, apiKey);
    }

    [Fact]
    public void GenerateSecret_ReturnsSixtyFourCharLowercaseHex()
    {
        var generator = new ApiKeyGenerator();

        var secret = generator.GenerateSecret();

        Assert.Matches(SecretPattern, secret);
    }

    [Fact]
    public void GenerateApiKey_CalledTwice_ReturnsDifferentValues()
    {
        var generator = new ApiKeyGenerator();

        var first = generator.GenerateApiKey();
        var second = generator.GenerateApiKey();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GenerateSecret_CalledTwice_ReturnsDifferentValues()
    {
        var generator = new ApiKeyGenerator();

        var first = generator.GenerateSecret();
        var second = generator.GenerateSecret();

        Assert.NotEqual(first, second);
    }
}
