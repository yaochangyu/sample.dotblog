using System;
using Lab.NetRemoting.Implement;

namespace Lab.NetRemoting.Core
{
    public class TrMessageFactory : MarshalByRefObject, ITrMessageFactory
    {
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public string Url { get; set; }

        public ITrMessage CreateInstance()
        {
            return new TrMessage();
        }

        public ITrMessage CreateInstance(string name)
        {
            return new TrMessage(name);
        }
    }
}