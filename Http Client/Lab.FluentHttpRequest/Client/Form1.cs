using System;
using System.Net.Http;
using System.Windows.Forms;

namespace Client
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
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            MessageBox.Show(content);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}