using System;

namespace WebApiNetCore31
{
    internal class MachineMessager : IMessager
    {
        public string OperationId { get; } = $"機器-{Guid.NewGuid()}";
        public void Dispose()
        {
            Console.WriteLine($"{nameof(MachineMessager)} GC");
        }
    }
}