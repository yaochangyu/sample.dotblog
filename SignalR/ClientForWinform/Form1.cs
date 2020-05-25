using System;
using System.Windows.Forms;
using Microsoft.AspNet.SignalR.Client;

namespace ClientForWinform
{
    public partial class Form1 : Form
    {
        private readonly string        _actionName = "Broadcast";
        private readonly HubConnection _hubConnection;
        private readonly string        _hubName    = "BroadcastHub";
        private readonly string        _signalrUrl = @"https://localhost:44342/";
        private          IHubProxy     _hubProxy;

        public Form1()
        {
            this.InitializeComponent();
            if (this._hubConnection == null)
            {
                this._hubConnection = new HubConnection(this._signalrUrl);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this._hubProxy = this._hubConnection.CreateHubProxy(this._hubName);

            this._hubProxy.On("ShowMessage",
                              (string name, string country) =>
                              {
                                  this.Messages_TextBox
                                      .InvokeIfNecessary(() =>
                                                         {
                                                             var msg = $"Hi, My name is {name}, I come from {country}\r\n";
                                                             this.Messages_TextBox
                                                                 .AppendText(msg);
                                                             Console.WriteLine(msg);
                                                         });
                              });

            //this._hubProxy.On("ShowMessage",
            //                  (string name) =>
            //                  {
            //                      this.Messages_TextBox.InvokeIfNecessary(() =>
            //                                                           {
            //                                                               this.Messages_TextBox
            //                                                                   .AppendText($"{name}\r\n");
            //                                                           });
            //                  });

            this._hubConnection.Start().Wait();
        }

        private async void SendMessageButton_Click(object sender, EventArgs e)
        {
            await this._hubProxy.Invoke(this._actionName,
                                        this.Name_TextBox.Text,
                                        this.Conutry_TextBox.Text);
        }
    }
}