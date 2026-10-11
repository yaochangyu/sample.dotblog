using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>審查意見的文件修正：契約、規格檔頭、issue 紀錄與 ADR 的文字檢查（記錄型，不驗證行為）。</summary>
[Binding]
public sealed class ReviewDocumentConsistencySteps
{
    private const string OpenApiRelativePath = "poc/AuthSpike/doc/openapi.yml";
    private const string SpecRelativePath = ".scratch/server-to-server-api-protection/spec.md";
    private const string IssueTwentyRelativePath = ".scratch/server-to-server-api-protection/issues/20-rejected-request-audit.md";
    private const string AdrRelativePath = "docs/adr/0001-lab-poc-exceptions.md";

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

    private static string Read(string relativePath) => File.ReadAllText(Path.Combine(RepoRoot, relativePath));

    [Then("審查修正的 OpenAPI 不包含 {string}")]
    public void ThenOpenApiDoesNotContain(string unexpected) => Read(OpenApiRelativePath).Should().NotContain(unexpected);

    [Then("審查修正的 OpenAPI 包含 {string}")]
    public void ThenOpenApiContains(string expected) => Read(OpenApiRelativePath).Should().Contain(expected);

    [Then("審查修正的規格檔頭不包含 {string}")]
    public void ThenSpecDoesNotContain(string unexpected) => Read(SpecRelativePath).Should().NotContain(unexpected);

    [Then("審查修正的 20 單不包含 {string}")]
    public void ThenIssueTwentyDoesNotContain(string unexpected) => Read(IssueTwentyRelativePath).Should().NotContain(unexpected);

    [Then("審查修正的 20 單包含 {string}")]
    public void ThenIssueTwentyContains(string expected) => Read(IssueTwentyRelativePath).Should().Contain(expected);

    [Then("審查修正的 ADR 0001 包含 {string}")]
    public void ThenAdrContains(string expected) => Read(AdrRelativePath).Should().Contain(expected);
}
