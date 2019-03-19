using System;
using System.Windows.Forms;

namespace App
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            this.InitializeComponent();
        }

        private void Login_Button_Click(object sender, EventArgs e)
        {
            if (this.Id_TextBox.Text == "yao" & this.Password_TextBox.Text == "123456")
            {
                MessageBox.Show($"Hi~{this.Id_TextBox.Text}","Title",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
        }
    }
}