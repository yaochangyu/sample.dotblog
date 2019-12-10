using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            A.A_Form1 a = new A.A_Form1();
            a.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            B.B_Form1 b = new B.B_Form1();
            b.Show();
        }
    }
}