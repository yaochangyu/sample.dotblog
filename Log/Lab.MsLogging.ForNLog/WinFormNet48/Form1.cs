using System;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace WinFormNet48
{
    public partial class Form1 : Form
    {
        private static readonly ILogger<Form1> Logger;

        static Form1()
        {
            var factory = LoggerFactory.Create(builder =>
                                               {
                                                   builder.AddFilter("Microsoft", LogLevel.Warning)
                                                          .AddFilter("System",           LogLevel.Warning)
                                                          .AddFilter("WindowsFormsApp1", LogLevel.Debug)
                                                          
                                                          .AddNLog();
                                                   ;
                                               });
            Logger = factory.CreateLogger<Form1>();
        }

        public Form1()
        {
            this.InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var button = (Button) sender;
            var name   = button.Name;

            Logger.LogInformation(LogEvent.GenerateItem, "{name} 按鈕被按了", name);
            Logger.LogInformation(LogEvent.UpdateItem,   "執行更新");
            Logger.LogInformation(LogEvent.GenerateItem, "完成");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var button = (Button) sender;
            var id     = Guid.NewGuid();
            var name   = button.Name;
            using (Logger.BeginScope("Scope {id}", id))
            {
                Logger.LogInformation("{name} 按鈕被按了", name);
                Logger.LogInformation("執行更新");
                Logger.LogInformation("完成");
            }
        }
    }
}