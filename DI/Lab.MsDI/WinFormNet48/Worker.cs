namespace WinFormNet48
{
    public class Worker
    {
        private IMessager Transient { get; }

        private IMessager Scope { get; }

        private IMessager Single { get; }

        public Worker(ITransientMessager transient, IScopeMessager scope, ISingleMessager single)
        {
            this.Transient = transient;
            this.Scope     = scope;
            this.Single    = single;
        }

        public string Get()
        {
            return $"transient:{this.Transient.OperationId}\r\n" +
                   $"scope:{this.Scope.OperationId}\r\n"         +
                   $"single:{this.Single.OperationId}";
        }
    }
}