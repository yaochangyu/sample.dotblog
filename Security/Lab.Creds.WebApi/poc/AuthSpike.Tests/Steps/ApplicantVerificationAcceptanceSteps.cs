using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Certificates;
using AuthSpike.Hosting;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>21 單：申請者驗證的整體驗收。端到端流程經真實 mTLS 與 HTTP 路徑；記錄型步驟只檢查紀錄文字。</summary>
[Binding]
public sealed class ApplicantVerificationAcceptanceSteps
{
    private const string ClientId = "orders-client";
    private const string PartnerClientId = "orders-partner-client";
    private const string CertificateRequestsPath = "admin/client-certificate-requests";
    private const string AuditRecordsPath = "admin/audit-records";
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/21-applicant-verification-acceptance.md";
    private const string GlossaryRelativePath = "GLOSSARY.md";
    private const string AdrRelativePath = "docs/adr/0001-lab-poc-exceptions.md";
    private const string FeaturesRelativePath = "poc/AuthSpike.Tests/Features";

    private readonly List<string> _requestIds = [];
    private X509Certificate2? _firstCandidate;
    private ApiResponse? _lastSubmission;
    private ApiResponse? _adminResponse;
    private string? _token;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    /// <summary>端到端情境中第一筆候選憑證（私鑰留在測試端，核准後用來取得 Token）。</summary>
    private X509Certificate2 FirstCandidate => _firstCandidate ??= NewCandidate();

    private static X509Certificate2 NewCandidate() => SpikeCertificates.CreateSelfSignedClientCertificate($"applicant-acceptance-{Runtime.EnvironmentName}-{Guid.NewGuid():N}");

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

    [Given("21 單的實作紀錄可讀取")]
    public void GivenIssueRecordReadable()
    {
        File.Exists(Path.Combine(RepoRoot, IssueRelativePath)).Should().BeTrue();
    }

    [When("驗收端到端 {string} 以自己的身分提出持有證明有效的新憑證申請")]
    public async Task WhenClientAppliesForItself(string clientId)
    {
        _lastSubmission = await SubmitValidAsync(Runtime.ClientCertificate(clientId), clientId, FirstCandidate);
    }

    [When("驗收端到端匿名提出持有證明有效的新憑證申請")]
    public async Task WhenAnonymousApplies()
    {
        _lastSubmission = await SubmitValidAsync(null, ClientId, NewCandidate());
    }

    [When("驗收端到端 {string} 替 {string} 提出持有證明有效的新憑證申請")]
    public async Task WhenClientAppliesForAnotherClient(string identityClientId, string claimedClientId)
    {
        _lastSubmission = await SubmitValidAsync(Runtime.ClientCertificate(identityClientId), claimedClientId, NewCandidate());
    }

    [When("驗收端到端 {string} 提出缺少持有證明的新憑證申請")]
    public async Task WhenClientAppliesWithoutProof(string clientId)
    {
        _lastSubmission = await ApplicantProofs.SubmitCertificateAsync(
            Runtime,
            Runtime.ClientCertificate(clientId),
            clientId,
            NewCandidate().ExportCertificatePem(),
            null);
        CaptureRequestId(_lastSubmission);
    }

    [When("驗收端到端 {string} 再提出 4 筆持有證明有效的新憑證申請")]
    public async Task WhenClientFillsPendingQueue(string clientId)
    {
        for (var index = 0; index < 4; index++)
        {
            var submission = await SubmitValidAsync(Runtime.ClientCertificate(clientId), clientId, NewCandidate());
            submission.Status.Should().Be(HttpStatusCode.Accepted, $"待核准數未達上限前的第 {index + 2} 筆申請應被受理：{submission.Body}");
        }
    }

    [When("驗收端到端 {string} 提出第 6 筆持有證明有效的新憑證申請")]
    public async Task WhenClientExceedsPendingCap(string clientId)
    {
        _lastSubmission = await SubmitValidAsync(Runtime.ClientCertificate(clientId), clientId, NewCandidate());
    }

    [Then("驗收端到端申請回應為 {int}")]
    public void ThenSubmissionResponseIs(int expected)
    {
        _lastSubmission.Should().NotBeNull("情境應先提出登錄申請");
        ((int)_lastSubmission!.Status).Should().Be(expected, _lastSubmission.Body);
    }

    [Then("驗收端到端 {string} 已受理的申請數為 {int}")]
    public async Task ThenAcceptedSubmissionCountIs(string clientId, int expected)
    {
        (await ApplicantVerificationSteps.AcceptedSubmissionCountAsync(clientId)).Should().Be(expected, "被拒絕的申請不應建立待核准紀錄");
    }

    [Then("驗收端到端新憑證取得 Token 被拒絕")]
    public async Task ThenPendingCertificateCannotGetToken()
    {
        var (issued, _) = await AdministratorInterfaceSteps.RequestTokenAsync(FirstCandidate, ClientId);
        issued.Should().BeFalse("待核准的新憑證不能取得 Token");
    }

    [When("驗收端到端管理員核准第一筆申請")]
    public async Task WhenAdministratorApprovesFirstRequest()
    {
        _requestIds.Should().NotBeEmpty("情境應先受理至少一筆申請");
        _adminResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Post,
            $"{CertificateRequestsPath}/{_requestIds[0]}/approve");
    }

    [Then("驗收端到端管理操作回應為 {int}")]
    public void ThenAdministratorResponseIs(int expected)
    {
        _adminResponse.Should().NotBeNull("情境應先執行管理操作");
        ((int)_adminResponse!.Status).Should().Be(expected, _adminResponse.Body);
    }

    [Then("驗收端到端新憑證取得 Token 並建立訂單，回應為 {int}")]
    public async Task ThenApprovedCertificateOrdersWithToken(int expected)
    {
        var (issued, token) = await AdministratorInterfaceSteps.RequestTokenAsync(FirstCandidate, ClientId);
        issued.Should().BeTrue("已核准的新憑證應能取得 Token");
        _token = token ?? throw new InvalidOperationException("已核准的新憑證應取得 Token 字串。");

        var json = $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"book","quantity":1}""";
        using var request = await SignedHttp.BuildSignedAsync(
            HttpMethod.Post,
            new Uri($"https://localhost:{Runtime.OrdersApi.Port}/orders"),
            _token,
            Runtime.SigningKey(ClientId),
            json,
            Guid.NewGuid().ToString());
        using var client = SignedHttp.CreateClient(Runtime, FirstCandidate);
        var business = await SignedHttp.SendAsync(client, request);
        ((int)business.Status).Should().Be(expected, business.Body);
    }

    [Then("驗收端到端 {string} 的未驗證拒絕申請紀錄至少 {int} 筆")]
    public async Task ThenRejectedApplicationRecordsAtLeast(string clientId, int expected)
    {
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Get, AuditRecordsPath);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var document = JsonDocument.Parse(response.Body);
        var rejected = document.RootElement.EnumerateArray()
            .Count(record => record.GetProperty("operation").GetString() == "certificate.submit"
                && record.GetProperty("outcome").GetString() == "rejected"
                && record.GetProperty("clientId").GetString() == clientId);
        rejected.Should().BeGreaterThanOrEqualTo(expected, "401、400、403、429 的申請嘗試都應留下 rejected 紀錄");
    }

    [Then("驗收端到端稽核紀錄不含私鑰標記與取得的 access_token")]
    public async Task ThenEndToEndAuditHasNoSecrets()
    {
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Get, AuditRecordsPath);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        // 非空斷言：確認紀錄確實存在，避免空清單讓「不含」成立。
        response.Body.Should().Contain("certificate.submit");
        response.Body.Should().NotContain("PRIVATE KEY");
        _token.Should().NotBeNullOrEmpty("情境應先取得 Token");
        response.Body.Should().NotContain(_token!);
    }

    [Then("21 單的 GLOSSARY 包含詞條 {string}")]
    public void ThenGlossaryContainsTerm(string term)
    {
        File.ReadAllText(Path.Combine(RepoRoot, GlossaryRelativePath)).Should().Contain(term);
    }

    [Then("21 單的 GLOSSARY 詞條 {string} 區分於 {string} 與 {string}")]
    public void ThenGlossaryEntryDistinguishes(string term, string first, string second)
    {
        var glossary = File.ReadAllText(Path.Combine(RepoRoot, GlossaryRelativePath));
        var start = glossary.IndexOf(term, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"GLOSSARY 應定義 {term}");
        var entry = glossary[start..];
        var end = entry.IndexOf("\n\n", StringComparison.Ordinal);
        entry = end > 0 ? entry[..end] : entry;
        entry.Should().Contain(first);
        entry.Should().Contain(second);
    }

    [Then("21 單的 ADR 0001 包含 {string}")]
    public void ThenAdrContains(string expected)
    {
        File.ReadAllText(Path.Combine(RepoRoot, AdrRelativePath)).Should().Contain(expected);
    }

    [Then("21 單的 ADR 0001 不包含 {string}")]
    public void ThenAdrDoesNotContain(string unexpected)
    {
        File.ReadAllText(Path.Combine(RepoRoot, AdrRelativePath)).Should().NotContain(unexpected);
    }

    [Then("21 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("21 單第 {int} 項驗收已勾選")]
    public void ThenAcceptanceItemChecked(int index)
    {
        var boxes = Regex.Matches(IssueText, @"^- \[( |x)\] ", RegexOptions.Multiline);
        boxes.Count.Should().BeGreaterThanOrEqualTo(index);
        boxes[index - 1].Groups[1].Value.Should().Be("x");
    }

    [Then("21 單的 AC 對應表列出 {int} 列")]
    public void ThenAcMappingRowCount(int expected)
    {
        AcMappingRows().Should().HaveCount(expected);
    }

    [Then("21 單的 AC 對應表中每個情境名稱都存在於 feature 檔")]
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

    /// <summary>送出持有證明有效的新憑證申請；identity 為 null 表示無憑證。受理的申請編號記入清單以供後續核准。</summary>
    private async Task<ApiResponse> SubmitValidAsync(X509Certificate2? identity, string claimedClientId, X509Certificate2 candidate)
    {
        var response = await ApplicantProofs.SubmitCertificateAsync(
            Runtime,
            identity,
            claimedClientId,
            candidate.ExportCertificatePem(),
            ApplicantProofs.CertificateProof(claimedClientId, candidate));
        CaptureRequestId(response);
        return response;
    }

    private void CaptureRequestId(ApiResponse response)
    {
        if (response.Status != HttpStatusCode.Accepted)
        {
            return;
        }

        using var document = JsonDocument.Parse(response.Body);
        _requestIds.Add(document.RootElement.GetProperty("requestId").GetString()!);
    }

    private static List<(string Ac, string ScenarioName)> AcMappingRows()
    {
        return Regex.Matches(IssueText, @"^\| (AC-\d{2}) \| (.+?) \|\s*$", RegexOptions.Multiline)
            .Select(match => (match.Groups[1].Value, match.Groups[2].Value.Trim()))
            .ToList();
    }
}
