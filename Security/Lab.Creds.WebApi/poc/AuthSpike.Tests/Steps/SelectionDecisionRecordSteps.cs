using System.Xml.Linq;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>檢查 01 單的決策紀錄，以及紀錄所指向的實際設定檔。</summary>
[Binding]
public sealed class SelectionDecisionRecordSteps
{
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/01-auth-selection-and-contract.md";

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".scratch")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("找不到專案根目錄（.scratch）。");
        }
    }

    private static string IssueText => File.ReadAllText(Path.Combine(RepoRoot, IssueRelativePath));

    private static string PocText(string relativePath) => File.ReadAllText(Path.Combine(RepoRoot, "poc", relativePath));

    private static string AuthSpikeProjectText => PocText(Path.Combine("AuthSpike", "AuthSpike.csproj"));

    [Given("01 單的實作紀錄可讀取")]
    public void GivenIssueRecordIsReadable()
    {
        File.Exists(Path.Combine(RepoRoot, IssueRelativePath)).Should().BeTrue();
    }

    [Then("實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("授權伺服器的 client_credentials 與 mTLS 用戶端認證由 OpenIddict 啟用")]
    public void ThenAuthServerEnablesOpenIddictFlows()
    {
        var host = PocText(Path.Combine("AuthSpike", "AuthServer", "AuthServerHost.cs"));
        host.Should().Contain("AddOpenIddict()");
        host.Should().Contain("AllowClientCredentialsFlow()");
        host.Should().Contain("EnableSelfSignedTlsClientAuthentication()");
    }

    [Then("授權伺服器未使用 Client Secret")]
    public void ThenAuthServerDoesNotUseClientSecret()
    {
        var host = PocText(Path.Combine("AuthSpike", "AuthServer", "AuthServerHost.cs"));
        host.Should().NotContain("ClientSecret");
    }

    [Then("OpenIddict 伺服器套件的授權條件為 {string}")]
    public void ThenOpenIddictLicenseIs(string expectedLicense)
    {
        var packageRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var nuspec = Path.Combine(packageRoot, "openiddict.aspnetcore", "7.7.1", "openiddict.aspnetcore.nuspec");

        File.Exists(nuspec).Should().BeTrue("套件 nuspec 應存在於 NuGet 快取");
        var license = XDocument.Load(nuspec).Descendants()
            .Single(element => element.Name.LocalName == "license")
            .Value;
        license.Should().Be(expectedLicense);
    }

    [Then("專案參照 OpenIddict.AspNetCore 與 OpenIddict.Validation.SystemNetHttp 7.7.1")]
    public void ThenProjectReferencesOpenIddictPackages()
    {
        AuthSpikeProjectText.Should().Contain("Include=\"OpenIddict.AspNetCore\" Version=\"7.7.1\"");
        AuthSpikeProjectText.Should().Contain("Include=\"OpenIddict.Validation.SystemNetHttp\" Version=\"7.7.1\"");
    }

    [Then("專案未引入 Code First 的 OpenAPI 產生套件")]
    public void ThenProjectHasNoCodeFirstOpenApiPackages()
    {
        AuthSpikeProjectText.Should().NotContain("Swashbuckle");
        AuthSpikeProjectText.Should().NotContain("Microsoft.AspNetCore.OpenApi");
    }

    [Then(@"建立訂單契約檔以 POST \/orders 定義 createOrder")]
    public void ThenContractDefinesCreateOrder()
    {
        var contract = PocText(Path.Combine("AuthSpike", "doc", "openapi.yml"));
        contract.Should().Contain("/orders:");
        contract.Should().Contain("operationId: createOrder");
    }

    [Then("01 單 Gateway 驗收項目已勾選")]
    public void ThenGatewayCheckboxIsChecked()
    {
        IssueText.Should().Contain("- [x] 確認 Gateway 候選與信任契約");
    }
}
