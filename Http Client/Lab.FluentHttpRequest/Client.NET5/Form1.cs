using System;
using System.Net.Http;
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
            Client = new HttpClient
            {
                BaseAddress = new Uri(BasicUrl)
            };
        }

        public Form1()
        {
            this.InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var url      = "WeatherForecast?lessTemperature=11";
            var response = await Client.GetAsync(url);

            // response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            MessageBox.Show(content);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            var url = "WeatherForecast";
            var requestUrl = BasicUrl.AppendPathSegment(url)
                                     .SetQueryParam("lessTemperature", 11);
            // var response       = requestUrl.GetAsync();
            // var responseResult = response.Result.ResponseMessage;
            // var content        = responseResult.Content.ReadAsStringAsync().Result;

            var content           = await requestUrl.GetAsync().ReceiveString();
            MessageBox.Show(content);
        }
    }
}