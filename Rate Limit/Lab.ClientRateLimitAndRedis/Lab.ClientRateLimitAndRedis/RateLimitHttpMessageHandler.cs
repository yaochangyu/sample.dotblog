﻿namespace Lab.ClientRateLimitAndRedis;

public class RateLimitHttpMessageHandler : DelegatingHandler
{
    private readonly List<DateTimeOffset> _callLog =
        new List<DateTimeOffset>();
    private readonly TimeSpan _limitTime;
    private readonly int _limitCount;

    public RateLimitHttpMessageHandler(int limitCount, TimeSpan limitTime)
    {
        this._limitCount = limitCount;
        this._limitTime = limitTime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        lock (this._callLog)
        {
            this._callLog.Add(now);

            while (this._callLog.Count > this._limitCount)
                this._callLog.RemoveAt(0);
        }

        await this.LimitDelay(now);

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task LimitDelay(DateTimeOffset now)
    {
        if (this._callLog.Count < this._limitCount)
            return;

        var limit = now.Add(-this._limitTime);

        var lastCall = DateTimeOffset.MinValue;
        var shouldLock = false;

        lock (this._callLog)
        {
            lastCall = this._callLog.FirstOrDefault();
            shouldLock = this._callLog.Count(x => x >= limit) >= this._limitCount;
        }

        var delayTime = shouldLock && (lastCall > DateTimeOffset.MinValue)
            ? (limit - lastCall)
            : TimeSpan.Zero;

        if (delayTime > TimeSpan.Zero)
            await Task.Delay(delayTime);
    }
}