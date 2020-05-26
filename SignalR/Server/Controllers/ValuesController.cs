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
        private static volatile bool IsRunning;

        [HttpPost]
        [Route("AddSchedule")]
        public IHttpActionResult AddSchedule(CancellationToken cancel)
        {
            if (IsRunning)
            {
                return new ResponseMessageResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            }

            IsRunning = true;

            var context = GlobalHost.ConnectionManager.GetHubContext<BroadcastHub>();
            var interval = new TimeSpan(0, 0, 0, 10);
            Task.Factory
                .StartNew(() =>
                          {
                              while (IsRunning)
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
                                      var isStop  = !IsRunning;
                                      var isExist = isStop || cancel.IsCancellationRequested;
                                      SpinWait.SpinUntil(() => isExist, interval);
                                  }
                              }
                          }, cancel);

            return new ResponseMessageResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult Post(string name, string country, string connectionIds)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<BroadcastHub>();
            if (string.IsNullOrWhiteSpace(connectionIds))
            {
                context.Clients
                       .All
                       .ShowMessage(name, country);
            }
            else
            {
                //不支援多筆
                context.Clients
                       .Clients(new List<string>
                       {
                           connectionIds
                       })
                       .ShowMessage(name, country);
            }

            return new ResponseMessageResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        }

        [HttpPost]
        [Route("RemoveSchedule")]
        public IHttpActionResult RemoveSchedule(CancellationToken cancel)
        {
            IsRunning = false;
            return new ResponseMessageResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        }
    }
}