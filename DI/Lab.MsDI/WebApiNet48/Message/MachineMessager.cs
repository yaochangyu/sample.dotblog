using System;

namespace WebApiNet48
{
    internal class MachineMessager : IMessager
    {
        public string OperationId { get; } = $"機器-{Guid.NewGuid()}";
    }
}