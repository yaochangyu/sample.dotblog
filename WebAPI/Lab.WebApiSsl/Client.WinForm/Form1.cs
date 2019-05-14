using System;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;

namespace Client.WinForm
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient s_client;
        private static readonly string     s_baseUrl  = "https://localhost:44314";
        private static          string     s_baseUrl1 = "http://localhost:6672";

        public int Id { get; set; }

        public string Name { get; set; }

        public string FirstName { get; set; }

        static Form1()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                    (request, cert2, cetChain, policyErrors) =>
                    {
                        //可以在這裡處理憑證
                        return true;
                    };
            var isExist    = 0;
            var isNotExist = false;

            var y = isExist == 0
                            ? "0"
                            : "1";

            if (s_client == null)
            {
                s_client             = new HttpClient(handler);
                s_client.BaseAddress = new Uri(s_baseUrl);
            }
        }

        public Form1()
        {
            this.InitializeComponent();
        }

        private (bool isDownTime, string downTime) ExecuteCheckDownTime(int sbCustId)
        {

            var YYY = "yaochang3";
            var YYYVal112 = "yaochang";
            var YYYVal11 = "yaochang";
            var YYYVal21 = "yaochangs3";
            var YYYVal3 = "yaochang";
            var YYY1 = "yaochang";


            var startTime = DateTime.Now;
            var endTime   = DateTime.Now;
            var ts        = new TimeSpan(endTime.Date.Ticks - startTime.Date.Ticks);
            var isExisthg = 0 == 9;
            var aaa = 100 != 0
                              ? $"{startTime:MM/dd HH:mm} ~ {endTime:MM/dd HH:mm}"
                              : $"{startTime:HH:mm} ~ {endTime:HH:mm}";
            var a1 = isExisthg
                             ? "asadasda"
                             : $"{startTime:HH:mm} ~ {endTime:HH:mm}";
            var dummyawww = isExisthg
                                    ? "0"
                                    : "1";

            return (true, aaa);
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            var url      = "api/Default";
            var content  = new StringContent("'掯'", Encoding.UTF8, "application/json");
            var response = s_client.PostAsync(url, content).Result;
            response.EnsureSuccessStatusCode();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            var url = "api/Default";

            var response = s_client.GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                var result = response.Content.ReadAsStringAsync().Result;
                MessageBox.Show(result);
            }
        }
    }

    internal class StringUtil
    {
        public static DateTime CDate(object st)
        {
            throw new NotImplementedException();
        }

        public static int CInt(int tsDays)
        {
            throw new NotImplementedException();
        }
    }
}