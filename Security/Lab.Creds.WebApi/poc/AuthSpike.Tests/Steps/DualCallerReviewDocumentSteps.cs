using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>雙呼叫者審查意見的文件修正：規格、票據與 ADR 的文字檢查（記錄型，不驗證行為）。</summary>
[Binding]
public sealed class DualCallerReviewDocumentSteps
{
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

    [Then("雙呼叫者文件 {string} 不包含 {string}")]
    public void ThenDualCallerDocumentDoesNotContain(string relativePath, string unexpected)
        => File.ReadAllText(Path.Combine(RepoRoot, relativePath)).Should().NotContain(unexpected);
}
