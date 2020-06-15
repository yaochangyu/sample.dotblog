using System;
using System.Windows.Forms;
using Lab.NetRemoting.Core;
using Newtonsoft.Json;

namespace Lab.NetRemoting.Client
{
    public partial class Form1 : Form
    {
        private readonly string _url = "tcp://127.0.0.1:9527/RemotingTest";

        public Form1()
        {
            this.InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var message = (ITrMessage) Activator.GetObject(typeof(ITrMessage), this._url);
            try
            {
                var now  = message.GetNow();
                var name = message.GetName();
                var person = message.GetPerson();
                var msg = $"Hi, my name is {name}\r\n" +
                          $"Now：{now:yyyy-MM-dd hh:mm:ss}\r\n" +
                          $"Data：{JsonConvert.SerializeObject(person)}";
                MessageBox.Show(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}