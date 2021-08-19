using System;

namespace WebApiNet48
{
    internal class LogMessager : IMessager
    {
        public string OperationId { get; } = $"日誌-{Guid.NewGuid()}";
    }
}