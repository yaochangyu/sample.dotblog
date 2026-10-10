using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Certificates;
using AuthSpike.Hosting;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>16 單：管理員核准的整體驗收。端到端流程經真實 mTLS 與 HTTP 路徑；記錄型步驟只檢查紀錄文字。</summary>
[Binding]
public sealed class AdminApprovalAcceptanceSteps
{
    private const string ClientId = "orders-client";
    private const string SubmitRelativePath = "client-certificate-requests";
    private const string AdminRequestsRelativePath = "admin/client-certificate-requests";
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/16-admin-approval-acceptance.md";
    private const string GlossaryRelativePath = "GLOSSARY.md";
    private const string AdrRelativePath = "docs/adr/0001-lab-poc-exceptions.md";
    private const string FeaturesRelativePath = "poc/AuthSpike.Tests/Features";

    private X509Certificate2? _newCertificate;
    private string? _requestId;
    private ApiResponse? _submission;
    private ApiResponse? _adminResponse;
    private ApiResponse? _business;
    private string? _token;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    /// <summary>端到端情境中新提交的候選憑證（只提交公開部分，私鑰留在測試端）。</summary>
    private X509Certificate2 NewCertificate => _newCertificate ??= SpikeCertificates.CreateSelfSignedClientCertificate($"{ClientId}-e2e-{Runtime.EnvironmentName}-{Guid.NewGuid():N}");

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

    [Given("16 單的實作紀錄可讀取")]
    public void GivenIssueRecordReadable()
    {
        File.Exists(Path.Combine(RepoRoot, IssueRelativePath)).Should().BeTrue();
    }

    [Given("為 {string} 以新憑證提交待核准的登錄申請")]
    public async Task GivenSubmitsNewCertificate(string clientId)
    {
        var body = JsonSerializer.Serialize(new { clientId, publicCertificatePem = NewCertificate.ExportCertificatePem() });
        using var client = SignedHttp.CreateClient(Runtime, null);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Runtime.AuthServer.Issuer, SubmitRelativePath))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        _submission = await SignedHttp.SendAsync(client, request);
        _submission.Status.Should().Be(HttpStatusCode.Accepted, _submission.Body);
        using var document = JsonDocument.Parse(_submission.Body);
        _requestId = document.RootElement.GetProperty("requestId").GetString();
    }

    [Then("端到端新憑證取得 Token 被拒絕")]
    public async Task ThenNewCertificateTokenRejected()
    {
        var (issued, _) = await AdministratorInterfaceSteps.RequestTokenAsync(NewCertificate, ClientId);
        issued.Should().BeFalse("待核准或已撤銷的憑證不能取得 Token");
    }

    [Then("端到端信任名單不包含新憑證指紋")]
    public async Task ThenTrustListExcludesNewCertificate()
    {
        AdministratorInterfaceSteps.RegisteredThumbprints(await AdministratorInterfaceSteps.TrustListBodyAsync())
            .Should().NotContain(NewCertificate.Thumbprint);
    }

    [When("端到端管理員核准新憑證的登錄申請")]
    public async Task WhenAdministratorApprovesNewCertificate()
    {
        _adminResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Post,
            $"{AdminRequestsRelativePath}/{_requestId}/approve");
    }

    [When("端到端管理員退役 {string} 的原始憑證")]
    public async Task WhenAdministratorRetiresOriginalCertificate(string clientId)
    {
        _adminResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Post,
            $"admin/clients/{clientId}/certificates/{Runtime.ClientCertificate(clientId).Thumbprint}/retire");
    }

    [When("端到端管理員撤銷 {string} 的新憑證")]
    public async Task WhenAdministratorRevokesNewCertificate(string clientId)
    {
        _adminResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Post,
            $"admin/clients/{clientId}/certificates/{NewCertificate.Thumbprint}/revoke");
    }

    [Then("端到端管理操作回應為 {int}")]
    public void ThenEndToEndAdministratorResponseIs(int expected)
    {
        _adminResponse.Should().NotBeNull("情境應先執行管理操作");
        ((int)_adminResponse!.Status).Should().Be(expected, _adminResponse.Body);
    }

    [Then("端到端信任名單中 {string} 包含新憑證指紋")]
    public async Task ThenTrustListIncludesNewCertificate(string clientId)
    {
        using var document = JsonDocument.Parse(await AdministratorInterfaceSteps.TrustListBodyAsync());
        document.RootElement.GetProperty("clients").EnumerateArray()
            .Single(client => client.GetProperty("clientId").GetString() == clientId)
            .GetProperty("certificateThumbprints").EnumerateArray()
            .Select(thumbprint => thumbprint.GetString())
            .Should().Contain(NewCertificate.Thumbprint);
    }

    [Then("端到端新憑證取得 Token 並以該 Token 建立訂單，回應為 {int}")]
    public async Task ThenNewCertificateOrdersWithToken(int expected)
    {
        var (issued, token) = await AdministratorInterfaceSteps.RequestTokenAsync(NewCertificate, ClientId);
        issued.Should().BeTrue("已核准的新憑證應能取得 Token");
        _token = token;

        var json = $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"book","quantity":1}""";
        using var request = await SignedHttp.BuildSignedAsync(
            HttpMethod.Post,
            new Uri($"https://localhost:{Runtime.OrdersApi.Port}/orders"),
            _token,
            Runtime.SigningKey(ClientId),
            json,
            Guid.NewGuid().ToString());
        using var client = SignedHttp.CreateClient(Runtime, NewCertificate);
        _business = await SignedHttp.SendAsync(client, request);
        ((int)_business.Status).Should().Be(expected, _business.Body);
    }

    [Then("端到端原始憑證取得 Token 被拒絕")]
    public async Task ThenOriginalCertificateTokenRejected()
    {
        var (issued, _) = await AdministratorInterfaceSteps.RequestTokenAsync(Runtime.ClientCertificate(ClientId), ClientId);
        issued.Should().BeFalse("已退役的原始憑證不能取得 Token");
    }

    [Then("端到端稽核紀錄不含私鑰標記與本次取得的 access_token")]
    public async Task ThenEndToEndAuditHasNoSecrets()
    {
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Get, "admin/audit-records");
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        // 非空斷言：確認紀錄確實存在，避免空清單讓「不含」成立。
        response.Body.Should().Contain("certificate.revoke");
        response.Body.Should().NotContain("PRIVATE KEY");
        _token.Should().NotBeNullOrEmpty("情境應先取得 Token");
        response.Body.Should().NotContain(_token!);
    }

    [Then("GLOSSARY 包含詞條 {string}")]
    public void ThenGlossaryContainsTerm(string term)
    {
        File.ReadAllText(Path.Combine(RepoRoot, GlossaryRelativePath)).Should().Contain(term);
    }

    [Then("GLOSSARY 的管理員詞條區分於 {string} 與 {string}")]
    public void ThenGlossaryAdministratorEntryDistinguishes(string first, string second)
    {
        var glossary = File.ReadAllText(Path.Combine(RepoRoot, GlossaryRelativePath));
        var start = glossary.IndexOf("**管理員**", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, "GLOSSARY 應定義管理員");
        var entry = glossary[start..];
        var end = entry.IndexOf("\n\n", StringComparison.Ordinal);
        entry = end > 0 ? entry[..end] : entry;
        entry.Should().Contain(first);
        entry.Should().Contain(second);
    }

    [Then("ADR 0001 包含 {string}")]
    public void ThenAdrContains(string expected)
    {
        File.ReadAllText(Path.Combine(RepoRoot, AdrRelativePath)).Should().Contain(expected);
    }

    [Then("16 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("16 單第 {int} 項驗收已勾選")]
    public void ThenAcceptanceItemChecked(int index)
    {
        var boxes = Regex.Matches(IssueText, @"^- \[( |x)\] ", RegexOptions.Multiline);
        boxes.Count.Should().BeGreaterThanOrEqualTo(index);
        boxes[index - 1].Groups[1].Value.Should().Be("x");
    }

    [Then("16 單的 AC 對應表列出 {int} 列")]
    public void ThenAcMappingRowCount(int expected)
    {
        AcMappingRows().Should().HaveCount(expected);
    }

    [Then("16 單的 AC 對應表中每個情境名稱都存在於 feature 檔")]
    public void ThenAcMappingScenarioNamesExist()
    {
        var features = string.Join('\n', Directory.GetFiles(Path.Combine(RepoRoot, FeaturesRelativePath), "*.feature")
            .Select(File.ReadAllText));
        foreach (var scenarioName in AcMappingRows().Select(row => row.ScenarioName))
        {
            var pattern = $@"^\s*Scenario( Outline)?: {Regex.Escape(scenarioName)}\s*$";
            Regex.IsMatch(features, pattern, RegexOptions.Multiline)
                .Should().BeTrue($"AC 對應表的情境 \"{scenarioName}\" 應存在於 feature 檔");
        }
    }

    private static List<(string Ac, string ScenarioName)> AcMappingRows()
    {
        return Regex.Matches(IssueText, @"^\| (AC-\d{2}) \| (.+?) \|\s*$", RegexOptions.Multiline)
            .Select(match => (match.Groups[1].Value, match.Groups[2].Value.Trim()))
            .ToList();
    }
}
