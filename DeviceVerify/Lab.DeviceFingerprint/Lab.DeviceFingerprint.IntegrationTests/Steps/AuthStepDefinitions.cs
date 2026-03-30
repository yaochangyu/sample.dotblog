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

    [Given("a registered user {string} with password {string}")]
    public async Task GivenARegisteredUser(string username, string password)
    {
        await _ctx.SeedUserAsync(username, password);
        scenarioContext["Username"] = username;
        scenarioContext["Password"] = password;
    }

    [Given("the user has a verified device with fingerprint {string}")]
    public async Task GivenVerifiedDevice(string fingerprint)
    {
        var username = scenarioContext["Username"].ToString()!;
        await _ctx.SeedVerifiedDeviceAsync(username, fingerprint);
        scenarioContext["KnownFingerprint"] = fingerprint;
    }

    [When("the user logs in with username {string} password {string} and fingerprint {string}")]
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

    [Then("the response should contain a JWT token")]
    public void ThenResponseHasToken()
    {
        Assert.True(_responseBody.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrEmpty(token.GetString()));
    }

    [Then("requireDeviceVerification should be false")]
    public void ThenNoVerificationRequired()
    {
        Assert.True(_responseBody.TryGetProperty("requireDeviceVerification", out var val));
        Assert.False(val.GetBoolean());
    }

    [Then("requireDeviceVerification should be true")]
    public void ThenVerificationRequired()
    {
        Assert.True(_responseBody.TryGetProperty("requireDeviceVerification", out var val));
        Assert.True(val.GetBoolean());
    }

    [Then("the response should contain userId and fingerprintHash")]
    public void ThenResponseHasUserIdAndHash()
    {
        Assert.True(_responseBody.TryGetProperty("userId", out var userId));
        Assert.True(_responseBody.TryGetProperty("fingerprintHash", out var hash));
        Assert.False(string.IsNullOrEmpty(userId.GetString()));
        Assert.False(string.IsNullOrEmpty(hash.GetString()));

        scenarioContext["PendingUserId"] = userId.GetString()!;
        scenarioContext["PendingFingerprintHash"] = hash.GetString()!;
    }

    [Given("the user logged in from new device with fingerprint {string}")]
    public async Task GivenNewDeviceLogin(string fingerprint)
    {
        var username = scenarioContext["Username"].ToString()!;
        var password = scenarioContext["Password"].ToString()!;
        await WhenLogin(username, password, fingerprint);
        ThenVerificationRequired();
        ThenResponseHasUserIdAndHash();
    }

    [Given("an OTP was generated for the device")]
    public void GivenOtpGenerated()
    {
        scenarioContext["OtpToUse"] = TestWebApplicationFactory.FixedOtp;
    }

    [When("the user submits the correct OTP for device verification")]
    public async Task WhenVerifyWithCorrectOtp()
    {
        var otp = scenarioContext["OtpToUse"].ToString()!;
        await SubmitOtp(otp);
    }

    [When("the user submits the wrong OTP {string} for device verification")]
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

    [Then("the device should be marked as verified")]
    public void ThenDeviceVerified()
    {
        Assert.Equal(System.Net.HttpStatusCode.OK, _response.StatusCode);
    }

    [Then("the response status should be {int}")]
    public void ThenResponseStatus(int statusCode)
    {
        Assert.Equal(statusCode, (int)_response.StatusCode);
    }

    [Given("the user is authenticated with fingerprint {string}")]
    public async Task GivenAuthenticated(string fingerprint)
    {
        var username = scenarioContext["Username"].ToString()!;
        var password = scenarioContext["Password"].ToString()!;
        await WhenLogin(username, password, fingerprint);
        _responseBody.TryGetProperty("token", out var tokenEl);
        scenarioContext["Token"] = tokenEl.GetString()!;
        scenarioContext["AuthFingerprint"] = fingerprint;
    }

    [When(@"the user calls GET \/api\/me with the correct fingerprint header")]
    public async Task WhenGetMeCorrectFingerprint()
    {
        var fingerprint = scenarioContext["AuthFingerprint"].ToString()!;
        await CallGetMe(fingerprint);
    }

    [When(@"the user calls GET \/api\/me with fingerprint header {string}")]
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
