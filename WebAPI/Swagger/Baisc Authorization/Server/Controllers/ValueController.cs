using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;

namespace Server.Controllers
{
    public class ValueController : ApiController
    {
        public IHttpActionResult Get()
        {
            return new ResponseMessageResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("value")
            });
        }
    }
}