namespace AuthSpike.Tests.Support;

/// <summary>
/// 可手動推進的時鐘（20 單後續，待核准逾期）：只供 @isolated 情境注入授權伺服器，讓逾期判定不必真的等待。
/// 只影響注入的判定（待核准逾期）；持有證明的時間窗仍以系統時間判定，維持既有行為。
/// </summary>
public sealed class ManualTimeProvider : TimeProvider
{
    private readonly object _gate = new();
    private DateTimeOffset _now = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return _now;
        }
    }

    public void Advance(TimeSpan delta)
    {
        lock (_gate)
        {
            _now += delta;
        }
    }
}
