using System;

namespace WinFormNet48
{
    internal class LogMessager : IMessager
    {
        public string OperationId { get; } = $"日誌-{Guid.NewGuid()}";
    }
}