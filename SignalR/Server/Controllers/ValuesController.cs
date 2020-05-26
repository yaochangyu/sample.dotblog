using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using Microsoft.AspNet.SignalR;
using Server.SignalR;

namespace Server.Controllers
{
    [RoutePrefix("api/values")]
    public class ValuesController : ApiController
    {
        private bool IsRunning { get; set; }

        private readonly CancellationTokenSource _cancellation;

        [HttpPost]
        [Route("AddSchedule")]
        public IHttpActionResult AddSchedule(CancellationToken cancel)
        {
            this.IsRunning = true;
            var context = GlobalHost.ConnectionManager.GetHubContext<BroadcastHub>();

            var interval = new TimeSpan(0, 0, 0, 10);
            Task.Factory
                .StartNew(() =>
                          {
                              while (this.IsRunning)
                              {
                                  try
                                  {
                                      if (cancel.IsCancellationRequested)
                                      {
                                          break;
                                      }

                                      var name    = FakeData.NameData.GetFullName();
                                      var country = FakeData.PlaceData.GetCountry();
                                      context.Clients.All.ShowMessage(name, country);
                                  }
                                  catch (Exception ex)
                                  {
                                      //Log
                                      Console.WriteLine(ex.ToString());
                                  }
                                  finally
                                  {
                                      var isExist = !this.IsRunning || cancel.IsCancellationRequested;
                                      SpinWait.SpinUntil(() => isExist, interval);
                                  }
                              }
                          }, cancel);

            return new ResponseMessageResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult Post(string name, string country, string connectionId)
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

            return new ResponseMessageResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        }
    }
}