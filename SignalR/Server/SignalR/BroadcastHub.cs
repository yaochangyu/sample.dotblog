using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace Server.SignalR
{
    public class BroadcastHub : Hub
    {
        private bool IsRunning { get; set; }

        private readonly CancellationTokenSource _cancellation;

        public BroadcastHub()
        {
            if (this._cancellation == null)
            {
                this._cancellation = new CancellationTokenSource();
            }

            this.Start();
        }

        public void Broadcast(string name, string country)
        {
            this.Clients.All.ShowMessage(name, country);
        }

        public void Start()
        {
            if (this.IsRunning)
            {
                return;
            }

            this.IsRunning = true;
            var interval = new TimeSpan(0, 0, 0, 10);
            var cancel   = this._cancellation.Token;
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
                                      this.Clients.All.ShowMessage(name, country);
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
        }

        public void Stop()
        {
            this.IsRunning = false;
        }
    }
}