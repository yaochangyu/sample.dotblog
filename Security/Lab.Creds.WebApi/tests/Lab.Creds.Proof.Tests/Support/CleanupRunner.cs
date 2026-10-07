using System.Runtime.ExceptionServices;

namespace Lab.Creds.Proof.Tests.Support;

internal static class CleanupRunner
{
    // Attempts every step, then rethrows: one error keeps its original instance/stack, several are aggregated in order.
    public static async Task RunAsync(IReadOnlyList<Func<Task>> steps)
    {
        var errors = new List<Exception>();
        foreach (var step in steps)
        {
            try
            {
                await step();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        if (errors.Count == 1) ExceptionDispatchInfo.Throw(errors[0]);
        if (errors.Count > 1) throw new AggregateException("ProofEnvironment cleanup failed", errors);
    }
}
