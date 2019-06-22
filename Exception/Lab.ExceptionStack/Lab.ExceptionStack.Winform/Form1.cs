using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Lab.ExceptionStack.BLL;

namespace Lab.ExceptionStack.Winform
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            var calculation = new Calculation();
            var add = calculation.Add(1, 1);
            calculation.Sub(add, 1);
        }
    }
}
