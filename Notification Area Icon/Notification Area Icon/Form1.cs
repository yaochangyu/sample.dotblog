using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Notification_Area_Icon
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            this.InitializeComponent();
        }

        protected void Displaynotify()
        {
            try
            {
                var filePath = Path.GetFullPath(@"THSLogo.ico");
                this.notifyIcon1.Icon            = new Icon(filePath);
                this.notifyIcon1.Icon            = new Icon("favicon.jpg");
                this.notifyIcon1.Text            = "Export Datatable Utlity";
                this.notifyIcon1.Visible         = true;
                this.notifyIcon1.BalloonTipTitle = "Welcome Devesh omar to Datatable Export Utlity";
                this.notifyIcon1.BalloonTipText  = "Click Here to see details";
                this.notifyIcon1.ShowBalloonTip(100);
            }
            catch (Exception ex)
            {
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Displaynotify();
        }
    }
}