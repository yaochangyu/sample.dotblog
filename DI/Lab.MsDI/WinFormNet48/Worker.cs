using System;

namespace WinFormNet48
{
    public class Worker
    {
        public IMessager Operation { get; set; }

        public Worker(IMessager operation)
        {
            this.Operation = operation;
        }

        public string Get()
        {
            return this.Operation.OperationId;
        }
    }
}