using System;

namespace Mvc5Net48.Message
{
    public interface IMessager:IDisposable
    {
        string OperationId { get; }
    }
}