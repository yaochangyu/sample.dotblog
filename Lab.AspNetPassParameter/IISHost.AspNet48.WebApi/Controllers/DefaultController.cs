using System.Web;
using System.Web.Http;

namespace AspNet48.WebApi.Controllers
{
    public class DefaultController : ApiController
    {
        // GET: api/Default
        public IHttpActionResult Get()
        {
            var key    = typeof(Member).FullName;
            var member = this.Request.Properties[key] as Member;
            return this.Ok(member);
        }
    }
}