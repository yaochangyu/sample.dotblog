using System;

namespace Mvc5Net48_1.Message
{
    public class MultiMessager : IScopeMessager, ISingleMessager, ITransientMessager
    {
        public string OperationId { get; } = $"多個接口-{Guid.NewGuid()}";
    }
}