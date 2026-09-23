namespace Lab.Signature.WebApi.Tests.Support;

/// <summary>
/// 每個 Admin Key Issuance Scenario 各自獨立的一份實例，用來在 Given/When/Then step 之間傳遞狀態。
/// </summary>
public class AdminScenarioContext
{
    public string? AdminKeyHeaderValue { get; set; }
    public bool UseProductionEnvironment { get; set; }

    public HttpResponseMessage? LastResponse { get; set; }
    public string LastResponseBody { get; set; } = string.Empty;
}
