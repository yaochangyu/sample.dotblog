using System;
using System.Net.Http;
using Flurl.Http.Configuration;

namespace Client.NET5
{
    public class ConnectionLifetimeHttpClientFactory : DefaultHttpClientFactory
    {
        public override HttpMessageHandler CreateMessageHandler()
        {
            var socketsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime    = TimeSpan.FromMinutes(10),
                // PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                // MaxConnectionsPerServer     = 10
            };
            return socketsHandler;
        }
    }
}