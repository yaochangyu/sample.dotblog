using System;

namespace WebApiNetCore31
{
    public interface IMessager:IDisposable
    {
        string OperationId { get; }
    }
}