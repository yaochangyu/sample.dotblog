﻿using System;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Client.WinFormsNet48
{
    public partial class Form1 : Form
    {
        private readonly ILabService _service;

        public Form1(LabService service)
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