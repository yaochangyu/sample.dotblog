using EsDailyLogs.Models;
using EsDailyLogs.Services;
using FluentAssertions;
using Xunit;

namespace EsDailyLogsApi.Tests;

public class LogQueueTests
{
    [Fact]
    public async Task EnqueueAsync_Should_Allow_Reading_Item()
    {
        // Arrange
        var queue = new LogQueue();
        var entry = new LogEntry
        {
            Service = "test-service",
            Message = "Queue unit test message",
            Level = "Information"
        };

        // Act
        await queue.EnqueueAsync(entry);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        LogEntry? received = null;
        await foreach (var item in queue.ReadAllAsync(cts.Token))
        {
            received = item;
            break;
        }

        // Assert
        received.Should().NotBeNull();
        received!.Service.Should().Be("test-service");
        received.Message.Should().Be("Queue unit test message");
    }
}
