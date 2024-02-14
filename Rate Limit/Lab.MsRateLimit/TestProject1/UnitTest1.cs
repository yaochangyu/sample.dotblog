using System.Threading.RateLimiting;

namespace TestProject1;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public async Task TestMethod1()
    {
        RateLimiter limiter = new ConcurrencyLimiter(
            new ConcurrencyLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
            });
        limiter.ConfigureAwait(false);
         var acquireResult1 =  limiter.AcquireAsync(permitCount: 2).Result;
         var acquireResult2 =  limiter.AcquireAsync(permitCount: 2).Result;
        // thread 1
        //  var acquireResult1 = await limiter.AcquireAsync(permitCount: 2);
        // if (acquireResult1.IsAcquired)
        // {
        // }
        //
        // // thread 2
        // var acquireResult2 = await limiter.AcquireAsync(permitCount: 2);
        // if (acquireResult2.IsAcquired)
        // {
        // }
    }
}