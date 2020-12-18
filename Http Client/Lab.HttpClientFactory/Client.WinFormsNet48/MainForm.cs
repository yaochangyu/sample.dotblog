using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace Client.WinFormsNet48
{
    public partial class MainForm : Form
    {
        internal static ServiceProvider ServiceProvider { get; set; }

        public MainForm()
        {
            this.InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var form = ServiceProvider.GetService(typeof(Form1)) as Form1;
            form.ShowDialog(this);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var form = ServiceProvider.GetService(typeof(Form2)) as Form2;
            form.ShowDialog(this);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }
    }
}