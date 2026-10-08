namespace Lab.Creds.Proof.Signatures;

// Counts executions of business endpoint handlers, so a rejected request can be shown never to have reached business logic.
public sealed class BusinessCallCounter
{
    private int _count;
    public int Count => Volatile.Read(ref _count);
    public void Increment() => Interlocked.Increment(ref _count);
}
