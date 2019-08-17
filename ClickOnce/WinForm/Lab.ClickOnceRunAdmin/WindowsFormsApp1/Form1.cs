using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1(CommandArgs arg)
        {
            this.InitializeComponent();
            this.Text        = arg.Title;
            this.label1.Text = arg.Title;
        }
    }
}