using System;

namespace WebApiNetCore31
{
    internal class LogMessager : IMessager
    {
        public string OperationId { get; } = $"日誌-{Guid.NewGuid()}";

        public void Dispose()
        {
            Console.WriteLine($"{nameof(LogMessager)} GC");
        }
    }
}