using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace Server.SignalR
{
    public class BroadcastHub : Hub
    {
        public void Broadcast(string name, string country)
        {
            this.Clients.All.ShowMessage(name, country);
        }
    }
}