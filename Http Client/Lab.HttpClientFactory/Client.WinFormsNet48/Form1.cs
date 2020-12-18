using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Client.WinFormsNet48
{
    public partial class Form1 : Form
    {
        //public Form1()
        //{
        //    InitializeComponent();
        //}

        private IHttpClientFactory _clientFactory;
        private ILabService         _service;

        //public Form1(IHttpClientFactory clientFactory)
        //{
        //    this._clientFactory = clientFactory;
        //}

        public Form1(ILabService service)
        {
            InitializeComponent();

            _service = service;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            var data = this._service.Get();
            MessageBox.Show(JsonConvert.SerializeObject(data));
        }
    }
}
