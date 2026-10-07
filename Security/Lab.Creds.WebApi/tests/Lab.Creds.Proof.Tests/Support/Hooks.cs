using Reqnroll;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Lab.Creds.Proof.Tests.Support;

[Binding]
public static class Hooks
{
    [BeforeTestRun]
    public static Task Start() => ProofEnvironment.StartAsync();

    [AfterTestRun]
    public static Task Stop() => ProofEnvironment.StopAsync();
}
