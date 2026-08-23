using System.Threading.Channels;
using EsDailyLogs.Models;

namespace EsDailyLogs.Services;

public interface ILogQueue
{
    ValueTask EnqueueAsync(LogEntry entry, CancellationToken ct = default);
    IAsyncEnumerable<LogEntry> ReadAllAsync(CancellationToken ct = default);
}

public class LogQueue : ILogQueue
{
    private readonly Channel<LogEntry> _channel;

    public LogQueue()
    {
        var options = new BoundedChannelOptions(500_000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<LogEntry>(options);
    }

    public ValueTask EnqueueAsync(LogEntry entry, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(entry, ct);

    public IAsyncEnumerable<LogEntry> ReadAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);
}
