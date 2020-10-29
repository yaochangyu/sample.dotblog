using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormNet48
{
    public partial class Form1 : Form
    {
        public Worker Workflow1 { get; set; }
        public Form1(Worker workflow1)
        {
            InitializeComponent();
            Workflow1 = workflow1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine(this.Workflow1.Operation.OperationId);
        }
    }
}
