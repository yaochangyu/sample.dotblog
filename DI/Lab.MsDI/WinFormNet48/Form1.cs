using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace WinFormNet48
{
    public partial class Form1 : Form
    {
        int _counter = 1;
        public Form1()
        {
            this.InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var serviceProvider = DependencyInjectionConfig.ServiceProvider;
            var work            = serviceProvider.GetRequiredService<Worker>();
            Console.WriteLine($"{this._counter}=>\r\n"+work.Get());
            
            this._counter++;
        }
    }
}