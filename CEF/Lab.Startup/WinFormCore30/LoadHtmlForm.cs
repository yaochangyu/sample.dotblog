using System;
using System.IO;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace WinFormCore30
{
    public partial class LoadHtmlForm : Form
    {
        private ChromiumWebBrowser _browser;

        public LoadHtmlForm()
        {
            this.InitializeComponent();
            this.InitBrowser();
            this.FormClosed += this.HtmlForm_FormClosed;
        }

        public void InitBrowser()
        {
            var fileName = Path.Combine(Directory.GetCurrentDirectory(), "Views/Index.html");

            this._browser = new ChromiumWebBrowser(fileName)

                {
                    Dock = DockStyle.Fill
                };
            this.Controls.Add(this._browser);
        }

        private void HtmlForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this._browser?.Dispose();
        }
    }
}