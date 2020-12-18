using System;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Client.WinFormsNet48
{
    public partial class Form2 : Form
    {
        private readonly ILabService _service;

        public Form2(LabService2 service)
        {
            this.InitializeComponent();

            this._service = service;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var data = this._service.Get();
            MessageBox.Show(JsonConvert.SerializeObject(data));
        }
    }
}