using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using Lab.Compress.ViaDecompressHandler.Models;

namespace Lab.Compress.ViaDecompressHandler.Controllers
{
    public class JsonDataController : ApiController
    {
        public IHttpActionResult Post(ICollection<Member> sources)
        {
            if (sources.Count >= 1)
            {
                return new ResponseMessageResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            return new ResponseMessageResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }
}