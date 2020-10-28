using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace WinFormViaDiContainerNet48
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private ILogger<Form1> _logger;
        public Form1(ILogger<Form1>logger)
        {
            InitializeComponent();
            this._logger = logger;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var button = (Button) sender;
            var name   = button.Name;

            _logger.LogInformation(LogEvent.GenerateItem, "{name} 按鈕被按了", name);
            _logger.LogInformation(LogEvent.UpdateItem,   "執行更新");
            _logger.LogInformation(LogEvent.GenerateItem, "完成");
        }
    }
}
