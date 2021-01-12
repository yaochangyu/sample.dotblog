using System;
using System.Net.Http;
using System.Reflection;
using System.Windows.Forms;
using Flurl;
using Flurl.Http;

namespace Client.NET5
{
    public partial class Form1 : Form
    {
        private static readonly string     BasicUrl = "https://localhost:44333/";
        private static readonly HttpClient Client;


        static Form1()
        {
            Client = new HttpClient(s_handler)
            {
                BaseAddress = new Uri(BasicUrl)
            };

            s_handler = new SocketsHttpHandler();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            using (var client = new HttpClient(s_handler))
            {
                client.BaseAddress = new Uri(BasicUrl);
                var url = "WeatherForecast?lessTemperature=11";

                var result1 = await client.GetAsync(url);

                var result2 = client.GetAsync(url).Result;// 混用 await 不會造成死結
            }

        }
        public Form1()
        {
            this.InitializeComponent();
        }

        private static readonly SocketsHttpHandler s_handler;

        private async void button2_Click(object sender, EventArgs e)
        {
            var httpClient = new HttpClient();

            var bindingFlags = BindingFlags.NonPublic
                               | BindingFlags.Instance
                ;
            var type      = typeof(HttpClientHandler);
            var fieldInfo = type.GetField("_underlyingHandler", bindingFlags);

            var field = typeof(HttpClient).GetField("_under", bindingFlags);

            var url = "WeatherForecast";
            var requestUrl = BasicUrl.AppendPathSegment(url)
                                     .SetQueryParam("lessTemperature", 11);
            try
            {
                var content = await requestUrl.GetAsync().ReceiveString();
                MessageBox.Show(content);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private async void Get()
        {
            var url = "WeatherForecast";
            var content = await BasicUrl.AppendPathSegment(url)
                                        .SetQueryParam("lessTemperature", 11)
                                        .GetAsync()
                                        .ReceiveString()
                ;
            MessageBox.Show(content);
        }
    }
}