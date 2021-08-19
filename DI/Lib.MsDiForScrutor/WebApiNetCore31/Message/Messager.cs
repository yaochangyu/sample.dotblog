using System;

namespace WebApiNetCore31
{
    public class Messager : IMessager
    {
        public string OperationId { get; } = $"訊息-{Guid.NewGuid()}";
    }
}