using System;
using System.Windows.Forms;
using Lab.ExceptionStack.BLL;

namespace Lab.ExceptionStack.Winform
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            this.InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            var calculation = new Calculation();
            calculation.Sub(1, 1);
        }
    }
}