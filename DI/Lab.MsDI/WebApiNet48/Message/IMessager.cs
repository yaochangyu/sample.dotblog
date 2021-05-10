using System;

namespace WebApiNet48
{
    public interface IMessager:IDisposable
    {
        string OperationId { get; }
    }
}