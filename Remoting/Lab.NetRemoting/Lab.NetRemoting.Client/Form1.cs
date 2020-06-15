using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Activation;
using System.Windows.Forms;
using Lab.NetRemoting.Core;
using Newtonsoft.Json;

namespace Lab.NetRemoting.Client
{
    public partial class Form1 : Form
    {
        private readonly ITrMessage _trMessage;
        private readonly string     _url = "tcp://127.0.0.1:9527/RemotingTest";

        public Form1()
        {
            this.InitializeComponent();

            //WellKnown 啟用模式，在客戶端建立物件時，只能呼叫預設的建構函式
            this._trMessage = (ITrMessage) Activator.GetObject(typeof(ITrMessage), this._url);
            Console.WriteLine($"{DateTime.Now}, 已連接伺服器：{this._url}");

            ////客戶端啟用模式，主要是要傳參數給建構函數，需參考 Lab.NetRemoting.Implement，若 TrMessage 有異動，客戶端的部署難度會增加
            //RemotingConfiguration.RegisterActivatedClientType(typeof(ITrMessage), this._url);

            ////直接實體化遠端物件
            //this._trMessage = new TrMessage("余小章");

            ////動態實體化遠端物件
            //object[] attrs = {new UrlAttribute(this._url)};
            //var      objs  = new object[1];
            //objs[0]         = "余小章";
            //this._trMessage = (ITrMessage) Activator.CreateInstance(typeof(TrMessage), objs, attrs);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var now    = this._trMessage.GetNow();
                var name   = this._trMessage.GetName();
                var person = this._trMessage.GetPerson();
                var msg = $"Hi, my name is {name}\r\n"         +
                          $"Now：{now:yyyy-MM-dd hh:mm:ss}\r\n" +
                          $"Data：{JsonConvert.SerializeObject(person)}";

                //MessageBox.Show(msg);
                Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
    }
}