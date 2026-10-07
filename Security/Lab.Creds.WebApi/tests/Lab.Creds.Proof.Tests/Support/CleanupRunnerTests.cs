using Xunit;

namespace Lab.Creds.Proof.Tests.Support;

public class CleanupRunnerTests
{
    [Fact]
    public async Task Given_第一個清理步驟失敗_When_執行清理_Then_後續步驟仍執行且重拋同一個例外()
    {
        var first = new InvalidOperationException("A");
        var executed = new List<string>();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => CleanupRunner.RunAsync(
        [
            () => { executed.Add("1"); throw first; },
            () => { executed.Add("2"); return Task.CompletedTask; },
            () => { executed.Add("3"); return Task.CompletedTask; }
        ]));

        Assert.Same(first, actual);
        Assert.Equal(["1", "2", "3"], executed);
    }

    [Fact]
    public async Task Given_第一與第三個清理步驟失敗_When_執行清理_Then_中間步驟執行且依序聚合例外()
    {
        var a = new InvalidOperationException("A");
        var c = new TimeoutException("C");
        var executed = new List<string>();

        var actual = await Assert.ThrowsAsync<AggregateException>(() => CleanupRunner.RunAsync(
        [
            () => { executed.Add("1"); throw a; },
            () => { executed.Add("2"); return Task.CompletedTask; },
            () => { executed.Add("3"); throw c; }
        ]));

        Assert.Equal(["1", "2", "3"], executed);
        Assert.Equal(new Exception[] { a, c }, actual.InnerExceptions);
    }
}
