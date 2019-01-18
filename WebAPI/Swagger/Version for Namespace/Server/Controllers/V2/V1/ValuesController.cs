using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;

namespace Server.Controllers.V1
{
    public class ValuesController : ApiController
    {
        public HttpResponseMessage Get()
        {
            return new HttpResponseMessage()
            {
                Content = new StringContent("This is a V1 response.")
            };
        }

        public IHttpActionResult Get(int id)
        {
            return new ResponseMessageResult(this.Request.CreateResponse(HttpStatusCode.OK,id));
        }
        
        // POST: api/Default
        public void Post([FromBody]string value)
        {
        }

        public IHttpActionResult Post(string name,[FromBody] int age)
        {
            return this.Ok();
        }

    }
}
