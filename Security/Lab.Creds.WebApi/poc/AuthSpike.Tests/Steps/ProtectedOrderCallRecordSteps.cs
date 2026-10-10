using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>檢查 02 單的實作紀錄與驗收項目勾選狀態。</summary>
[Binding]
public sealed class ProtectedOrderCallRecordSteps
{
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/02-certificate-bound-api-call.md";

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

    [Given("02 單的實作紀錄可讀取")]
    public void GivenIssueRecordIsReadable()
    {
        File.Exists(Path.Combine(RepoRoot, IssueRelativePath)).Should().BeTrue();
    }

    [Then("02 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("02 單 mTLS 短效 Token 項目已勾選")]
    public void ThenMtlsShortLivedTokenCheckboxIsChecked()
    {
        IssueText.Should().Contain("- [x] mTLS 用戶端認證成功才核發綁定該憑證的短效 Opaque Token；Token 效期已由使用者核准為 300 秒。");
    }

    [Then("02 單 Gateway 項目標為略過而非完成")]
    public void ThenGatewayCheckboxIsSkipped()
    {
        var line = IssueText.Split('\n').Single(l => l.Contains("Gateway 下游通道經過認證"));
        line.Should().StartWith("- [ ] ~~Gateway 下游通道經過認證");
        line.Should().Contain("本 lab 不實作，略過（非完成）");
    }
}
