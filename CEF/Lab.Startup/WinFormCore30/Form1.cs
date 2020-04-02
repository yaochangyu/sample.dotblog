using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace WinFormCore30
{
    public partial class Form1 : Form
    {
        public ChromiumWebBrowser _browser;

        public Form1()
        {
            this.InitializeComponent();
            this.FormClosed += this.Form1_FormClosed;

            this.InitBrowser();
        }

        public void InitBrowser()
        {
            Cef.Initialize(new CefSettings());
            this._browser = new ChromiumWebBrowser("www.google.com")
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(this._browser);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Cef.Shutdown();
            this._browser?.Dispose();
        }
    }
}