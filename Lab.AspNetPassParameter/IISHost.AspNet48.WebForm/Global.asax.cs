using System;
using System.Web;
using Faker;

namespace IISHost.AspNet48.WebForm
{
    public class Global : HttpApplication
    {
        protected void Application_BeginRequest()
        {
            var member = new Member {Id = Guid.NewGuid(), Name = Name.FullName()};
            var key    = member.GetType().FullName;
            HttpContext.Current.Items[key] = member;
        }

        protected void Application_Start(object sender, EventArgs e)
        {
        }
    }
}