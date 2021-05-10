using System;

namespace WebApiNetCore31
{
    public class MultiMessager : IScopeMessager, ISingleMessager, ITransientMessager
    {
        public string OperationId { get; } = $"多個接口-{Guid.NewGuid()}";

        public void Dispose()
        {
            Console.WriteLine($"{nameof(MultiMessager)} GC");
        }
    }
}