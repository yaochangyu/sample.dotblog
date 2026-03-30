using System.Net.Http.Json;
using System.Text.Json;
using Lab.DeviceFingerprint.IntegrationTests.Support;
using Reqnroll;

namespace Lab.DeviceFingerprint.IntegrationTests.Steps;

[Binding]
public class AuthStepDefinitions(ScenarioContext scenarioContext)
{
    private readonly TestContext _ctx = new();
    private HttpResponseMessage _response = null!;
    private JsonElement _responseBody;

    [Given("已建立帳號 {string} 密碼為 {string}")]
    public async Task GivenARegisteredUser(string username, string password)
    {
        await _ctx.SeedUserAsync(username, password);
        scenarioContext["Username"] = username;
        scenarioContext["Password"] = password;
    }

    [Given("使用者已綁定指紋為 {string} 的裝置")]
    public async Task GivenVerifiedDevice(string fingerprint)
    {
        var username = scenarioContext["Username"].ToString()!;
        await _ctx.SeedVerifiedDeviceAsync(username, fingerprint);
        scenarioContext["KnownFingerprint"] = fingerprint;
    }

    [When("使用者以帳號 {string} 密碼 {string} 指紋 {string} 登入")]
    public async Task WhenLogin(string username, string password, string fingerprint)
    {
        _response = await _ctx.Client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password,
            fingerprint,
            deviceName = "Test Device",
        });

        if (_response.Content.Headers.ContentLength != 0)
            _responseBody = JsonSerializer.Deserialize<JsonElement>(await _response.Content.ReadAsStringAsync());
    }

    [Then("回應應包含 JWT token")]
    public void ThenResponseHasToken()
    {
        Assert.True(_responseBody.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrEmpty(token.GetString()));
    }

    [Then("requireDeviceVerification 應為 false")]
    public void ThenNoVerificationRequired()
    {
        Assert.True(_responseBody.TryGetProperty("requireDeviceVerification", out var val));
        Assert.False(val.GetBoolean());
    }

    [Then("requireDeviceVerification 應為 true")]
    public void ThenVerificationRequired()
    {
        Assert.True(_responseBody.TryGetProperty("requireDeviceVerification", out var val));
        Assert.True(val.GetBoolean());
    }

    [Then("回應應包含 userId 與 fingerprintHash")]
    public void ThenResponseHasUserIdAndHash()
    {
        Assert.True(_responseBody.TryGetProperty("userId", out var userId));
        Assert.True(_responseBody.TryGetProperty("fingerprintHash", out var hash));
        Assert.False(string.IsNullOrEmpty(userId.GetString()));
        Assert.False(string.IsNullOrEmpty(hash.GetString()));

        scenarioContext["PendingUserId"] = userId.GetString()!;
        scenarioContext["PendingFingerprintHash"] = hash.GetString()!;
    }

    [Given("使用者已從指紋 {string} 的新裝置登入")]
    public async Task GivenNewDeviceLogin(string fingerprint)
    {
        var username = scenarioContext["Username"].ToString()!;
        var password = scenarioContext["Password"].ToString()!;
        await WhenLogin(username, password, fingerprint);
        ThenVerificationRequired();
        ThenResponseHasUserIdAndHash();
    }

    [Given("系統已產生 OTP")]
    public void GivenOtpGenerated()
    {
        scenarioContext["OtpToUse"] = TestWebApplicationFactory.FixedOtp;
    }

    [When("使用者提交正確的 OTP 進行裝置驗證")]
    public async Task WhenVerifyWithCorrectOtp()
    {
        var otp = scenarioContext["OtpToUse"].ToString()!;
        await SubmitOtp(otp);
    }

    [When("使用者提交錯誤 OTP {string} 進行裝置驗證")]
    public async Task WhenVerifyWithWrongOtp(string otp)
    {
        var userId = scenarioContext["PendingUserId"].ToString()!;
        var hash = scenarioContext["PendingFingerprintHash"].ToString()!;

        _response = await _ctx.Client.PostAsJsonAsync("/api/auth/verify-device", new
        {
            userId,
            fingerprintHash = hash,
            otp,
            deviceName = "Test Device",
        });
    }

    private async Task SubmitOtp(string otp)
    {
        var userId = scenarioContext["PendingUserId"].ToString()!;
        var hash = scenarioContext["PendingFingerprintHash"].ToString()!;

        _response = await _ctx.Client.PostAsJsonAsync("/api/auth/verify-device", new
        {
            userId,
            fingerprintHash = hash,
            otp,
            deviceName = "Test Device",
        });

        if (_response.Content.Headers.ContentLength != 0)
            _responseBody = JsonSerializer.Deserialize<JsonElement>(await _response.Content.ReadAsStringAsync());
    }

    [Then("裝置應被標記為已驗證")]
    public void ThenDeviceVerified()
    {
        Assert.Equal(System.Net.HttpStatusCode.OK, _response.StatusCode);
    }

    [Then("回應狀態碼應為 {int}")]
    public void ThenResponseStatus(int statusCode)
    {
        Assert.Equal(statusCode, (int)_response.StatusCode);
    }

    [Given("使用者已以指紋 {string} 完成認證")]
    public async Task GivenAuthenticated(string fingerprint)
    {
        var username = scenarioContext["Username"].ToString()!;
        var password = scenarioContext["Password"].ToString()!;
        await WhenLogin(username, password, fingerprint);
        _responseBody.TryGetProperty("token", out var tokenEl);
        scenarioContext["Token"] = tokenEl.GetString()!;
        scenarioContext["AuthFingerprint"] = fingerprint;
    }

    [When(@"使用者以正確指紋標頭呼叫 GET \/api\/me")]
    public async Task WhenGetMeCorrectFingerprint()
    {
        var fingerprint = scenarioContext["AuthFingerprint"].ToString()!;
        await CallGetMe(fingerprint);
    }

    [When(@"使用者以指紋標頭 {string} 呼叫 GET \/api\/me")]
    public async Task WhenGetMeWrongFingerprint(string fingerprint)
    {
        await CallGetMe(fingerprint);
    }

    private async Task CallGetMe(string fingerprint)
    {
        var token = scenarioContext["Token"].ToString()!;
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        req.Headers.Add("X-Device-Fingerprint", fingerprint);
        _response = await _ctx.Client.SendAsync(req);
    }
}
