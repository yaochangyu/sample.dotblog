using System;

namespace Mvc5Net48.Message
{
    internal class MachineMessager : IMessager
    {
        public string OperationId { get; } = $"機器-{Guid.NewGuid()}";
    }
}