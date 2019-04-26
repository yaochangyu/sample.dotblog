using System;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;

namespace Client.WinForm
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient s_client;

        //private static string s_baseUrl = "http://localhost:6672";
        private static readonly string s_baseUrl = "https://localhost:44344";

        static Form1()
        {
            var handler = new HttpClientHandler();
            //handler.ServerCertificateCustomValidationCallback =
            //    (request, cert2, cetChain, policyErrors) =>
            //    {
            //        //可以在這裡處理憑證
            //        return true;
            //    };

            if (s_client == null)
            {
                s_client = new HttpClient(handler);
                s_client.BaseAddress = new Uri(s_baseUrl);
            }
        }

        public Form1()
        {
            this.InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            var url = "api/Default";
            var content = new StringContent("'掯'", Encoding.UTF8, "application/json");

            var response = s_client.PostAsync(url, content).Result;
            response.EnsureSuccessStatusCode();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            var url = "api/Default";

            var response = s_client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
        }
    }
}