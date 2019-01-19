using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using Server.Filters;

namespace Server.Controllers
{
    public class ValueController : ApiController
    {
        //[IdentityBasicAuthentication]
        public IHttpActionResult Get()
        {
            return new ResponseMessageResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("value")
            });
        }
    }
}