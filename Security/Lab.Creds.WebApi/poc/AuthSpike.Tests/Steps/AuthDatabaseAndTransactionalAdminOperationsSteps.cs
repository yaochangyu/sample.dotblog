using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using AuthSpike.AuthServer;
using AuthSpike.Certificates;
using AuthSpike.Data;
using AuthSpike.Hosting;
using AuthSpike.Signing;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Npgsql;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

[Binding]
public sealed class AuthDatabaseAndTransactionalAdminOperationsSteps
{
    private const string ClientId = "orders-client";
    private RealStorageTestHarness _harness = null!;
    private X509Certificate2? _candidate;
    private SignatureKey? _candidateKey;
    private string? _requestId;
    private ApiResponse? _lastAdminResponse;
    private List<ApiResponse> _concurrentResponses = [];
    private List<ApiResponse> _twoInstanceResponses = [];
    private AuthServerHost? _secondaryAuthServer;
    private string? _issuedTokenBeforeRestart;
    private bool _failureConstraintInjected;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;
    private static CancellationToken DefaultCt => new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

    private X509Certificate2 Candidate => _candidate ??= SpikeCertificates.CreateSelfSignedClientCertificate(
        $"{ClientId}-candidate-{Runtime.EnvironmentName}-{Guid.NewGuid():N}");

    private SignatureKey CandidateKey => _candidateKey ??= new SignatureKey(
        $"key-{Runtime.EnvironmentName}-{Guid.NewGuid():N}",
        ECDsa.Create(ECCurve.NamedCurves.nistP256));

    [Given("真實儲存測試環境已就緒且授權伺服器連線至 {string}")]
    public async Task GivenRealStorageWithAuthDb(string serviceName)
    {
        _harness = await RealStorageTestHarness.GetAsync(DefaultCt);
        await _harness.ResetAuthDbAsync(DefaultCt);
        var connectionString = _harness.AuthDbConnectionString;
        await SpikeEnvironment.UsePostgresAuthAsync(connectionString);
    }

    [Given("呼叫端 {string} 提出憑證登錄申請")]
    public async Task GivenClientSubmitsCertificateRequest(string clientId)
    {
        var response = await ApplicantProofs.SubmitCertificateAsync(
            Runtime,
            Runtime.ClientCertificate(clientId),
            clientId,
            Candidate.ExportCertificatePem(),
            ApplicantProofs.CertificateProof(clientId, Candidate));

        response.Status.Should().Be(HttpStatusCode.Accepted, response.Body);
        using var doc = JsonDocument.Parse(response.Body);
        _requestId = doc.RootElement.GetProperty("requestId").GetString();
    }

    [Given("呼叫端 {string} 提出簽章金鑰登錄申請")]
    public async Task GivenClientSubmitsSigningKeyRequest(string clientId)
    {
        var applicantThumbprint = Runtime.ClientCertificate(clientId).Thumbprint;
        var proof = ApplicantProofs.SigningKeyProofAt(
            clientId,
            applicantThumbprint,
            CandidateKey.KeyId,
            CandidateKey.Key,
            CandidateKey.Key,
            ApplicantProofs.NowUnix(),
            ApplicantProofs.NewNonce());

        var response = await ApplicantProofs.SubmitSigningKeyAsync(
            Runtime,
            Runtime.ClientCertificate(clientId),
            clientId,
            CandidateKey.KeyId,
            CandidateKey.Key.ExportSubjectPublicKeyInfoPem(),
            proof);

        response.Status.Should().Be(HttpStatusCode.Accepted, response.Body);
        using var doc = JsonDocument.Parse(response.Body);
        _requestId = doc.RootElement.GetProperty("requestId").GetString();
    }

    [When("授權伺服器模擬管理操作套用失敗")]
    public async Task WhenSimulateApplyFailure()
    {
        _harness ??= await RealStorageTestHarness.GetAsync(DefaultCt);
        var store = new PostgresAuthStore(_harness.AuthDbConnectionString);
        await store.SetSimulatedFailureConstraintAsync(failAuditAlso: false, table: "auth_certificates", cancellationToken: DefaultCt);
        _failureConstraintInjected = true;
    }

    [When("授權伺服器模擬簽章金鑰管理操作套用失敗")]
    public async Task WhenSimulateSigningKeyApplyFailure()
    {
        _harness ??= await RealStorageTestHarness.GetAsync(DefaultCt);
        var store = new PostgresAuthStore(_harness.AuthDbConnectionString);
        await store.SetSimulatedFailureConstraintAsync(failAuditAlso: false, table: "auth_signing_keys", cancellationToken: DefaultCt);
        _failureConstraintInjected = true;
    }

    [When("授權伺服器模擬管理操作套用失敗且稽核寫入失敗")]
    public async Task WhenSimulateApplyAndAuditFailure()
    {
        _harness ??= await RealStorageTestHarness.GetAsync(DefaultCt);
        var store = new PostgresAuthStore(_harness.AuthDbConnectionString);
        await store.SetSimulatedFailureConstraintAsync(failAuditAlso: true, table: "both", cancellationToken: DefaultCt);
        _failureConstraintInjected = true;
    }

    [When("管理員嘗試核准該申請")]
    public async Task WhenAdminAttemptsApproval()
    {
        _lastAdminResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Post,
            $"admin/client-certificate-requests/{_requestId}/approve");
    }

    [When("管理員嘗試核准該簽章金鑰申請")]
    public async Task WhenAdminAttemptsSigningKeyApproval()
    {
        _lastAdminResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Post,
            $"admin/signing-key-requests/{_requestId}/approve");
    }

    [Then("管理操作失敗且未變更憑證狀態為核准")]
    public async Task ThenOperationFailedAndCertNotApproved()
    {
        _lastAdminResponse.Should().NotBeNull();
        _lastAdminResponse!.Status.Should().Be(HttpStatusCode.InternalServerError);

        var trustResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Get,
            "admin/trust-list");
        trustResponse.Status.Should().Be(HttpStatusCode.OK, trustResponse.Body);
        using (var doc = JsonDocument.Parse(trustResponse.Body))
        {
            var clients = doc.RootElement.GetProperty("clients");
            foreach (var client in clients.EnumerateArray())
            {
                if (client.GetProperty("clientId").GetString() == ClientId)
                {
                    var certs = client.GetProperty("certificateThumbprints");
                    certs.EnumerateArray().Any(t => string.Equals(t.GetString(), Candidate.Thumbprint, StringComparison.OrdinalIgnoreCase))
                        .Should().BeFalse("套用失敗時信任名單不應包含該憑證指紋");
                }
            }
        }

        var reqResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Get,
            $"admin/client-certificate-requests/{_requestId}");
        reqResponse.Status.Should().Be(HttpStatusCode.OK, reqResponse.Body);
        using var reqDoc = JsonDocument.Parse(reqResponse.Body);
        reqDoc.RootElement.GetProperty("status").GetString()
            .Should().Be("pending", "套用失敗時申請紀錄狀態必須維持 pending");
    }

    [Then("管理操作失敗且未變更簽章金鑰狀態為核准")]
    public async Task ThenSigningKeyOperationFailedAndNotApproved()
    {
        _lastAdminResponse.Should().NotBeNull();
        _lastAdminResponse!.Status.Should().Be(HttpStatusCode.InternalServerError);

        var trustResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Get,
            "admin/trust-list");
        trustResponse.Status.Should().Be(HttpStatusCode.OK, trustResponse.Body);
        using (var doc = JsonDocument.Parse(trustResponse.Body))
        {
            var clients = doc.RootElement.GetProperty("clients");
            foreach (var client in clients.EnumerateArray())
            {
                if (client.GetProperty("clientId").GetString() == ClientId)
                {
                    var keys = client.GetProperty("signingKeyIds");
                    keys.EnumerateArray().Any(k => k.GetString() == CandidateKey.KeyId)
                        .Should().BeFalse("套用失敗時信任名單不應包含該金鑰 ID");
                }
            }
        }

        var reqResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Get,
            $"admin/signing-key-requests/{_requestId}");
        reqResponse.Status.Should().Be(HttpStatusCode.OK, reqResponse.Body);
        using var reqDoc = JsonDocument.Parse(reqResponse.Body);
        reqDoc.RootElement.GetProperty("status").GetString()
            .Should().Be("pending", "套用失敗時申請紀錄狀態必須維持 pending");
    }

    [Then("資料庫稽核紀錄未留下該操作的 accepted 紀錄")]
    public async Task ThenAuditHasNoAcceptedRecord()
    {
        var auditResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Get,
            "admin/audit-records");
        auditResponse.Status.Should().Be(HttpStatusCode.OK, auditResponse.Body);
        using var doc = JsonDocument.Parse(auditResponse.Body);
        var audits = doc.RootElement.EnumerateArray()
            .Select(e => (
                Operation: e.GetProperty("operation").GetString(),
                Outcome: e.GetProperty("outcome").GetString()))
            .ToList();
        audits.Should().NotContain(a => a.Outcome == "accepted" && (a.Operation == "certificate.approve" || a.Operation == "signing_key.approve"),
            "套用失敗時 accepted 稽核必須在交易回滾中撤銷");
    }

    [Then("資料庫稽核紀錄留下一筆 failed 稽核")]
    public async Task ThenAuditHasFailedRecord()
    {
        var auditResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Get,
            "admin/audit-records");
        auditResponse.Status.Should().Be(HttpStatusCode.OK, auditResponse.Body);
        using var doc = JsonDocument.Parse(auditResponse.Body);
        var audits = doc.RootElement.EnumerateArray()
            .Select(e => (
                Operation: e.GetProperty("operation").GetString(),
                Outcome: e.GetProperty("outcome").GetString()))
            .ToList();
        audits.Should().ContainSingle(a => a.Outcome == "failed" && (a.Operation == "certificate.approve" || a.Operation == "signing_key.approve"),
            "套用失敗時必須留下一筆 failed 稽核");
    }

    [Then("呼叫端無法以該未核准的憑證取得存取 Token")]
    public async Task ThenUnapprovedCertCannotGetToken()
    {
        using var client = SignedHttp.CreateClient(Runtime, Candidate);
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.BaseUrl, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = ClientId,
            }), DefaultCt);

        response.IsSuccessStatusCode.Should().BeFalse("套用或稽核失敗回滾後，未核准的憑證不得取得 Token");
    }

    [Then("呼叫端仍可以原憑證成功取得存取 Token")]
    public async Task ThenOriginalCertCanStillGetToken()
    {
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(ClientId));
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.BaseUrl, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = ClientId,
            }), DefaultCt);

        response.IsSuccessStatusCode.Should().BeTrue("原已核准的憑證應持續能取得 Token");
    }

    [Then("管理操作回傳 503 且錯誤原因為 {string}")]
    public void ThenAdminReturns503WithReason(string reason)
    {
        _lastAdminResponse.Should().NotBeNull();
        _lastAdminResponse!.Status.Should().Be(HttpStatusCode.ServiceUnavailable, _lastAdminResponse.Body);
        _lastAdminResponse.Body.Should().Contain(reason);
    }

    [When("兩個管理員執行個體併發核准該申請")]
    public async Task WhenTwoInstancesConcurrentlyApprove()
    {
        var secondaryPort = SpikeEnvironment.FreeTcpPort();
        await Runtime.StartSecondaryAuthServerAsync(secondaryPort, cancellationToken: DefaultCt);

        var primaryPort = Runtime.AuthServer.Port;

        var task1 = Task.Run(async () =>
        {
            using var client = SignedHttp.CreateClient(Runtime, Runtime.AdministratorCertificate);
            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://localhost:{primaryPort}/admin/client-certificate-requests/{_requestId}/approve"));
            return await SignedHttp.SendAsync(client, req);
        });

        var task2 = Task.Run(async () =>
        {
            using var client = SignedHttp.CreateClient(Runtime, Runtime.AdministratorCertificate);
            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://localhost:{secondaryPort}/admin/client-certificate-requests/{_requestId}/approve"));
            return await SignedHttp.SendAsync(client, req);
        });

        _twoInstanceResponses = [await task1, await task2];
    }

    [When("兩個管理員執行個體併發分別核准與拒絕該申請")]
    public async Task WhenTwoInstancesConcurrentlyApproveAndReject()
    {
        var secondaryPort = SpikeEnvironment.FreeTcpPort();
        await Runtime.StartSecondaryAuthServerAsync(secondaryPort, cancellationToken: DefaultCt);

        var primaryPort = Runtime.AuthServer.Port;

        var task1 = Task.Run(async () =>
        {
            using var client = SignedHttp.CreateClient(Runtime, Runtime.AdministratorCertificate);
            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://localhost:{primaryPort}/admin/client-certificate-requests/{_requestId}/approve"));
            return await SignedHttp.SendAsync(client, req);
        });

        var task2 = Task.Run(async () =>
        {
            using var client = SignedHttp.CreateClient(Runtime, Runtime.AdministratorCertificate);
            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://localhost:{secondaryPort}/admin/client-certificate-requests/{_requestId}/reject"));
            return await SignedHttp.SendAsync(client, req);
        });

        _twoInstanceResponses = [await task1, await task2];
    }

    [Then("一個執行個體回應核准成功且另一個執行個體回應 409")]
    public void ThenOneSucceedsAndOneReturns409()
    {
        _twoInstanceResponses.Should().HaveCount(2);
        var statuses = _twoInstanceResponses.Select(r => r.Status).ToList();
        statuses.Should().Contain(HttpStatusCode.OK);
        statuses.Should().Contain(HttpStatusCode.Conflict);
    }

    [Then("一個執行個體回應成功且另一個執行個體回應 409")]
    public void ThenOneSucceedsAndOtherReturns409()
    {
        _twoInstanceResponses.Should().HaveCount(2);
        var statuses = _twoInstanceResponses.Select(r => r.Status).ToList();
        statuses.Should().Contain(HttpStatusCode.OK);
        statuses.Should().Contain(HttpStatusCode.Conflict);
    }

    [Then("該申請在資料庫中的最終狀態為 {string}")]
    public async Task ThenRequestFinalStatusIs(string expectedStatus)
    {
        var targetBaseUrl = _secondaryAuthServer?.BaseUrl ?? Runtime.AuthServer.BaseUrl;
        using var client = SignedHttp.CreateClient(Runtime, Runtime.AdministratorCertificate);
        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(targetBaseUrl, $"admin/client-certificate-requests/{_requestId}"));
        var response = await SignedHttp.SendAsync(client, req);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var doc = JsonDocument.Parse(response.Body);
        var status = doc.RootElement.GetProperty("status").GetString();
        status.Should().Be(expectedStatus);
    }

    [Then("該申請在資料庫中的最終狀態不為 {string}")]
    public async Task ThenRequestFinalStatusIsNot(string excludedStatus)
    {
        var targetBaseUrl = _secondaryAuthServer?.BaseUrl ?? Runtime.AuthServer.BaseUrl;
        using var client = SignedHttp.CreateClient(Runtime, Runtime.AdministratorCertificate);
        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(targetBaseUrl, $"admin/client-certificate-requests/{_requestId}"));
        var response = await SignedHttp.SendAsync(client, req);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var doc = JsonDocument.Parse(response.Body);
        var status = doc.RootElement.GetProperty("status").GetString();
        status.Should().NotBe(excludedStatus);
    }

    [When("呼叫端 {string} 併發送出 10 筆憑證登錄申請")]
    public async Task WhenClientConcurrentlySubmits10Requests(string clientId)
    {
        var tasks = Enumerable.Range(0, 10).Select(async index =>
        {
            var cert = SpikeCertificates.CreateSelfSignedClientCertificate($"{clientId}-concurrent-{index}-{Guid.NewGuid():N}");
            return await ApplicantProofs.SubmitCertificateAsync(
                Runtime,
                Runtime.ClientCertificate(clientId),
                clientId,
                cert.ExportCertificatePem(),
                ApplicantProofs.CertificateProof(clientId, cert));
        });

        _concurrentResponses = (await Task.WhenAll(tasks)).ToList();
    }

    [Then("剛好 5 筆申請獲得接受且其餘 5 筆回應 429")]
    public void ThenExactly5AcceptedAndOthers429()
    {
        _concurrentResponses.Should().HaveCount(10);
        var accepted = _concurrentResponses.Count(r => r.Status == HttpStatusCode.Accepted);
        var limitReached = _concurrentResponses.Count(r => r.Status == HttpStatusCode.TooManyRequests);

        accepted.Should().Be(5, $"期望 5 筆成功，實際狀態：{string.Join(", ", _concurrentResponses.Select(r => (int)r.Status))}");
        limitReached.Should().Be(5, $"期望 5 筆 429，實際狀態：{string.Join(", ", _concurrentResponses.Select(r => (int)r.Status))}");
    }

    [Then("呼叫端 {string} 在資料庫中的待核准申請數為 {int}")]
    public async Task ThenPendingCountInDatabaseIs(string clientId, int expectedCount)
    {
        var acceptedResponses = _concurrentResponses.Where(r => r.Status == HttpStatusCode.Accepted).ToList();
        acceptedResponses.Should().HaveCount(expectedCount, "被受理之申請總數應符合預期");

        foreach (var accepted in acceptedResponses)
        {
            using var doc = JsonDocument.Parse(accepted.Body);
            var reqId = doc.RootElement.GetProperty("requestId").GetString();
            var checkResponse = await AdministratorInterfaceSteps.SendAdminAsync(
                Runtime.AdministratorCertificate,
                HttpMethod.Get,
                $"admin/client-certificate-requests/{reqId}");
            checkResponse.Status.Should().Be(HttpStatusCode.OK, checkResponse.Body);
            using var checkDoc = JsonDocument.Parse(checkResponse.Body);
            checkDoc.RootElement.GetProperty("status").GetString().Should().Be("pending");
        }

        var probeCert = SpikeCertificates.CreateSelfSignedClientCertificate($"{clientId}-cap-probe-{Guid.NewGuid():N}");
        var probeResponse = await ApplicantProofs.SubmitCertificateAsync(
            Runtime,
            Runtime.ClientCertificate(clientId),
            clientId,
            probeCert.ExportCertificatePem(),
            ApplicantProofs.CertificateProof(clientId, probeCert));

        if (expectedCount >= PostgresAuthStore.PendingRequestLimitPerClient)
        {
            probeResponse.Status.Should().Be(HttpStatusCode.TooManyRequests, "達到上限時後續申請應被 429 拒絕");
        }
        else
        {
            probeResponse.Status.Should().Be(HttpStatusCode.Accepted, "未達到上限時後續申請應可被接受");
        }
    }

    [When("呼叫端 {string} 以相同持有證明 nonce 併發送出 2 次申請")]
    public async Task WhenClientConcurrentlySubmitsDuplicateProof(string clientId)
    {
        var cert = Candidate;
        var proof = ApplicantProofs.CertificateProof(clientId, cert);

        var task1 = Task.Run(() => ApplicantProofs.SubmitCertificateAsync(
            Runtime,
            Runtime.ClientCertificate(clientId),
            clientId,
            cert.ExportCertificatePem(),
            proof));

        var task2 = Task.Run(() => ApplicantProofs.SubmitCertificateAsync(
            Runtime,
            Runtime.ClientCertificate(clientId),
            clientId,
            cert.ExportCertificatePem(),
            proof));

        _concurrentResponses = [await task1, await task2];
    }

    [Then("剛好 1 筆申請獲得接受且另 1 筆被拒絕")]
    public void ThenOneAcceptedAndOneRejected()
    {
        _concurrentResponses.Should().HaveCount(2);
        var accepted = _concurrentResponses.Count(r => r.Status == HttpStatusCode.Accepted);
        var rejected = _concurrentResponses.Count(r => r.Status == HttpStatusCode.BadRequest || r.Status == HttpStatusCode.Conflict);

        accepted.Should().Be(1);
        rejected.Should().Be(1);
    }

    [Given("管理員核准該申請")]
    [When("管理員核准該申請")]
    public async Task WhenAdminApprovesRequest()
    {
        _lastAdminResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Post,
            $"admin/client-certificate-requests/{_requestId}/approve");
        _lastAdminResponse.Status.Should().Be(HttpStatusCode.OK, _lastAdminResponse.Body);
    }

    [Given("管理員核准該簽章金鑰申請")]
    [When("管理員核准該簽章金鑰申請")]
    public async Task WhenAdminApprovesSigningKeyRequest()
    {
        _lastAdminResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Post,
            $"admin/signing-key-requests/{_requestId}/approve");
        _lastAdminResponse.Status.Should().Be(HttpStatusCode.OK, _lastAdminResponse.Body);
    }

    [Given("呼叫端以核准的憑證取得存取 Token")]
    public async Task GivenClientObtainsTokenWithApprovedCert()
    {
        var (issued, token) = await AdministratorInterfaceSteps.RequestTokenAsync(Candidate, ClientId);
        issued.Should().BeTrue("核准後憑證應能取得 Token");
        token.Should().NotBeNullOrEmpty();
        _issuedTokenBeforeRestart = token;
    }

    [When("授權伺服器以原資料庫重新啟動")]
    public async Task WhenAuthServerRestarts()
    {
        _secondaryAuthServer = null;
        var initialCerts = Runtime.ClientCertificatesMap.ToDictionary(
            kv => kv.Key,
            kv => new List<X509Certificate2> { kv.Value[0] });
        var existingServerCert = Runtime.ServerCertificate;
        var existingTrust = Runtime.Trust;
        var existingAuthPort = Runtime.AuthServer.Port;
        var existingAdminCert = Runtime.AdministratorCertificate;

        await SpikeEnvironment.RestoreDefaultTokensAsync();
        await SpikeEnvironment.UsePostgresAuthAsync(
            _harness.AuthDbConnectionString,
            applyAuthMigrations: false,
            initialClientCertificates: initialCerts,
            initialServerCertificate: existingServerCert,
            initialTrust: existingTrust,
            authPort: existingAuthPort,
            initialAdministratorCertificate: existingAdminCert);
    }

    [When("由另一個授權伺服器執行個體接手連線至原資料庫")]
    public async Task WhenAnotherInstanceTakesOver()
    {
        var secondaryPort = SpikeEnvironment.FreeTcpPort();
        _secondaryAuthServer = await Runtime.StartSecondaryAuthServerAsync(
            secondaryPort,
            applyAuthMigrations: false,
            cancellationToken: DefaultCt);
    }

    [Then("重啟前核發的存取 Token 依然有效")]
    public async Task ThenPreExistingTokenStillActive()
    {
        _issuedTokenBeforeRestart.Should().NotBeNullOrEmpty();
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(SpikeRuntime.ResourceClientId));
        var targetBaseUrl = _secondaryAuthServer?.BaseUrl ?? Runtime.AuthServer.BaseUrl;
        using var response = await client.PostAsync(
            new Uri(targetBaseUrl, "connect/introspect"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = _issuedTokenBeforeRestart!,
                ["token_type_hint"] = "access_token",
                ["client_id"] = SpikeRuntime.ResourceClientId,
            }), DefaultCt);

        var body = await response.Content.ReadAsStringAsync(DefaultCt);
        response.IsSuccessStatusCode.Should().BeTrue(body);
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("active").GetBoolean().Should().BeTrue("Token 於接手或重啟後依然有效");
    }

    [Then("核准的憑證依然能成功取得 Token")]
    public async Task ThenApprovedCertCanStillGetToken()
    {
        var targetBaseUrl = _secondaryAuthServer?.BaseUrl ?? Runtime.AuthServer.BaseUrl;
        using var client = SignedHttp.CreateClient(Runtime, Candidate);
        using var response = await client.PostAsync(
            new Uri(targetBaseUrl, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = ClientId,
            }), DefaultCt);

        response.IsSuccessStatusCode.Should().BeTrue("接手或重啟後授權伺服器應自資料庫載入已核准憑證");
        var body = await response.Content.ReadAsStringAsync(DefaultCt);
        using var doc = JsonDocument.Parse(body);
        var token = doc.RootElement.GetProperty("access_token").GetString();
        token.Should().NotBeNullOrEmpty();
    }

    [Then("資料庫中該申請的狀態依然為 {string}")]
    public async Task ThenDbRequestStatusIs(string expectedStatus)
    {
        var targetBaseUrl = _secondaryAuthServer?.BaseUrl ?? Runtime.AuthServer.BaseUrl;
        using var client = SignedHttp.CreateClient(Runtime, Runtime.AdministratorCertificate);
        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(targetBaseUrl, $"admin/client-certificate-requests/{_requestId}"));
        var response = await SignedHttp.SendAsync(client, req);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var doc = JsonDocument.Parse(response.Body);
        doc.RootElement.GetProperty("status").GetString().Should().Be(expectedStatus);
    }

    [Then("管理信任名單中包含該核准的憑證指紋")]
    public async Task ThenTrustListContainsApprovedCert()
    {
        var targetBaseUrl = _secondaryAuthServer?.BaseUrl ?? Runtime.AuthServer.BaseUrl;
        using var client = SignedHttp.CreateClient(Runtime, Runtime.AdministratorCertificate);
        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(targetBaseUrl, "admin/trust-list"));
        var response = await SignedHttp.SendAsync(client, req);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var doc = JsonDocument.Parse(response.Body);
        var clients = doc.RootElement.GetProperty("clients");
        var found = false;
        foreach (var c in clients.EnumerateArray())
        {
            if (c.GetProperty("clientId").GetString() == ClientId)
            {
                if (c.GetProperty("certificateThumbprints").EnumerateArray().Any(t => string.Equals(t.GetString(), Candidate.Thumbprint, StringComparison.OrdinalIgnoreCase)))
                {
                    found = true;
                    break;
                }
            }
        }
        found.Should().BeTrue("管理信任名單必須包含核准的憑證指紋");
    }

    [Then("資料庫中該簽章金鑰申請的狀態依然為 {string}")]
    public async Task ThenDbSigningKeyRequestStatusIs(string expectedStatus)
    {
        var targetBaseUrl = _secondaryAuthServer?.BaseUrl ?? Runtime.AuthServer.BaseUrl;
        using var client = SignedHttp.CreateClient(Runtime, Runtime.AdministratorCertificate);
        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(targetBaseUrl, $"admin/signing-key-requests/{_requestId}"));
        var response = await SignedHttp.SendAsync(client, req);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var doc = JsonDocument.Parse(response.Body);
        doc.RootElement.GetProperty("status").GetString().Should().Be(expectedStatus);
    }

    [Then("管理信任名單中包含該核准的簽章金鑰")]
    public async Task ThenTrustListContainsApprovedSigningKey()
    {
        var targetBaseUrl = _secondaryAuthServer?.BaseUrl ?? Runtime.AuthServer.BaseUrl;
        using var client = SignedHttp.CreateClient(Runtime, Runtime.AdministratorCertificate);
        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(targetBaseUrl, "admin/trust-list"));
        var response = await SignedHttp.SendAsync(client, req);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var doc = JsonDocument.Parse(response.Body);
        var clients = doc.RootElement.GetProperty("clients");
        var found = false;
        foreach (var c in clients.EnumerateArray())
        {
            if (c.GetProperty("clientId").GetString() == ClientId)
            {
                if (c.GetProperty("signingKeyIds").EnumerateArray().Any(k => k.GetString() == CandidateKey.KeyId))
                {
                    found = true;
                    break;
                }
            }
        }
        found.Should().BeTrue("管理信任名單必須包含核准的簽章金鑰 ID");
    }

    [Then("重啟前留下的管理稽核紀錄依然存在且一致")]
    public async Task ThenAuditsPreservedAfterRestart()
    {
        var targetBaseUrl = _secondaryAuthServer?.BaseUrl ?? Runtime.AuthServer.BaseUrl;
        using var client = SignedHttp.CreateClient(Runtime, Runtime.AdministratorCertificate);
        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(targetBaseUrl, "admin/audit-records"));
        var response = await SignedHttp.SendAsync(client, req);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var doc = JsonDocument.Parse(response.Body);
        var audits = doc.RootElement.EnumerateArray()
            .Select(e => (
                Operation: e.GetProperty("operation").GetString(),
                Outcome: e.GetProperty("outcome").GetString(),
                ClientId: e.TryGetProperty("clientId", out var c) && c.ValueKind != JsonValueKind.Null ? c.GetString() : null,
                Fingerprint: e.TryGetProperty("fingerprint", out var f) && f.ValueKind != JsonValueKind.Null ? f.GetString() : null
            ))
            .ToList();

        audits.Should().HaveCountGreaterThanOrEqualTo(2, "重啟或接手後管理稽核紀錄必須完整保留");
        audits.Should().Contain(a => a.Outcome == "accepted" && (a.Operation == "certificate.submit" || a.Operation == "signing_key.submit"), "申請稽核欄位必須一致");
        audits.Should().Contain(a => a.Outcome == "accepted" && (a.Operation == "certificate.approve" || a.Operation == "signing_key.approve"), "核准稽核欄位必須一致");
    }

    [Then("資料庫中已登錄的管理員依然能成功讀取信任名單")]
    public async Task ThenRegisteredAdminCanStillAccessTrustList()
    {
        var targetBaseUrl = _secondaryAuthServer?.BaseUrl ?? Runtime.AuthServer.BaseUrl;
        using var client = SignedHttp.CreateClient(Runtime, Runtime.AdministratorCertificate);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(targetBaseUrl, "admin/trust-list"));
        var response = await SignedHttp.SendAsync(client, request);
        response.Status.Should().Be(HttpStatusCode.OK, "已登錄於 auth_administrators 之管理員憑證應成功讀取管理信任名單");
    }

    [Then("未登錄於資料庫的自簽憑證無法存取管理介面")]
    public async Task ThenUnregisteredCertCannotAccessAdmin()
    {
        var targetBaseUrl = _secondaryAuthServer?.BaseUrl ?? Runtime.AuthServer.BaseUrl;
        var fakeAdminCert = SpikeCertificates.CreateSelfSignedClientCertificate($"fake-admin-{Guid.NewGuid():N}");
        using var client = SignedHttp.CreateClient(Runtime, fakeAdminCert);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(targetBaseUrl, "admin/trust-list"));
        var response = await SignedHttp.SendAsync(client, request);
        response.Status.Should().Be(HttpStatusCode.Unauthorized, "未登錄於 auth_administrators 的憑證不得存取管理介面");
    }

    [AfterScenario]
    public async Task CleanupEnvironment()
    {
        var errors = new List<Exception>();
        if (_secondaryAuthServer is not null)
        {
            try
            {
                await _secondaryAuthServer.DisposeAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errors.Add(ex);
            }
            _secondaryAuthServer = null;
        }

        if (_failureConstraintInjected && _harness is not null)
        {
            try
            {
                var store = new PostgresAuthStore(_harness.AuthDbConnectionString);
                await store.ClearSimulatedFailureConstraintsAsync(DefaultCt);
                _failureConstraintInjected = false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errors.Add(ex);
            }
        }

        if (_harness is not null)
        {
            try
            {
                await _harness.ResetAuthDbAsync(DefaultCt);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errors.Add(ex);
            }
        }

        try
        {
            await SpikeEnvironment.RestoreDefaultTokensAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            errors.Add(ex);
        }

        if (errors.Count > 0)
        {
            throw new AggregateException("CleanupEnvironment encountered errors during test cleanup.", errors);
        }
    }
}
