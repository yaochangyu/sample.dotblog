using System;

namespace WebApiNet48
{
    public class MultiMessager : IScopeMessager, ISingleMessager, ITransientMessager
    {
        public string OperationId { get; } = $"多個接口-{Guid.NewGuid()}";
    }
}