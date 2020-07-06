using System;
using System.Web;
using System.Web.UI;

namespace AspNet48.WebForm
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (this.IsPostBack)
            {
                return;
            }

            var httpContext = new HttpContextWrapper(HttpContext.Current);
            var key            = typeof(Member).FullName;
            var member         = httpContext.Items[key] as Member;
            this.Id_Label.Text   = member.Id.ToString();
            this.Name_Label.Text = member.Name;
        }
    }
}