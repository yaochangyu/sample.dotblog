using System;
using System.Windows.Forms;

namespace Client.WinFormsNet48
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            this.InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var form = Program.ServiceProvider.GetService(typeof(Form1)) as Form1;
            form.ShowDialog(this);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var form = Program.ServiceProvider.GetService(typeof(Form2)) as Form2;
            form.ShowDialog(this);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }
    }
}