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

namespace WinFromNet48
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            var factory = LoggerFactory.Create(builder =>
                                               {
                                                   builder.AddFilter("Microsoft", LogLevel.Warning)
                                                          .AddFilter("System", LogLevel.Warning)
                                                          .AddFilter("WindowsFormsApp1", LogLevel.Debug)
                                                          .AddConsole()
                                                       ;
                                               });
            var logger = factory.CreateLogger<Form1>();
            logger.LogInformation("Example log message");
        }
    }
}
