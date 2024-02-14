using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace WinFormNet48
{
    public class BaseForm : Form
    {
        public BaseForm()
        {
            var serviceProvider = DependencyInjectionConfig.ServiceProvider;
            using (var serviceScope = serviceProvider.CreateScope())
            {
                var work = serviceScope.ServiceProvider.GetRequiredService<Worker>();
            }
        }
    }

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
            using (var serviceScope = serviceProvider.CreateScope())
            {
                var work = serviceScope.ServiceProvider.GetRequiredService<Worker>();
                Console.WriteLine($"{this._counter}=>\r\n" + work.Get());
            }

            this._counter++;
        }
    }
}