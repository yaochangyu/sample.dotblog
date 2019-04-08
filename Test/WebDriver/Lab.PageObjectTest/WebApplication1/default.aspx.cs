using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebApplication1
{
    public partial class _default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            //Process.Start(@"C:\Users\yao\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\東和鋼鐵\ClickOnceEx測試程式.開發者線上版.appref-ms");
            //Process.Start(@"http://nttp3ts1-iis85/app/clickonceex.debug/THS.ClickOnceEx.WinformApp.application");

            this.Response.Redirect("http://nttp3ts1-iis85/app/clickonceex.debug/THS.ClickOnceEx.WinformApp.application");
        }

        protected void Button2_Click(object sender, EventArgs e)
        {
            //Process.Start(@"http://nttp3ts1-iis85/app/clickonceex.debug/setup.exe");
            this.Response.Redirect("http://nttp3ts1-iis85/app/clickonceex.debug/setup.exe");

        }
    }
}