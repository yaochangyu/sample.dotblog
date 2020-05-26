using System.Collections.Generic;
using System.Web.Http;
using Microsoft.AspNet.SignalR;
using Server.SignalR;

namespace Server.Controllers
{
    public class ValuesController : ApiController
    {
        [HttpPost]
        public void Send(string name, string country, string connectionId)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<BroadcastHub>();
            if (connectionId == "*")
            {
                context.Clients
                       .All
                       .ShowMessage(name, country);
            }
            else
            {
                context.Clients
                       .Clients(new List<string>
                       {
                           connectionId
                       })
                       .ShowMessage(name, country);
            }
        }
    }
}