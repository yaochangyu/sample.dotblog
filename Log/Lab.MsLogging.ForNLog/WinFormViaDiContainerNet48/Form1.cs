using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WinFormViaDiContainerNet48
{
    public partial class Form1 : Form
    {
        public ILogger<Form1> Logger { get; set; }

        public Runner Runner { get; set; }

        public Form1()
        {
            this.InitializeComponent();
        }

        public Form1(ILogger<Form1> logger, Runner runner)
        {
            this.InitializeComponent();
            this.Logger = logger;
            this.Runner = runner;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var button = (Button) sender;
            var name   = button.Name;

            this.Logger.LogInformation(LogEvent.GenerateItem, "{name} 按鈕被按了", name);
            this.Logger.LogInformation(LogEvent.UpdateItem,   "執行更新");

            Runner.DoAction(name);

            this.Logger.LogInformation(LogEvent.GenerateItem, "完成");
        }
    }
}