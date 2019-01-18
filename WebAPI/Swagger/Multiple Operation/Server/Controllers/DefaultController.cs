using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Server.Controllers
{
    public class DefaultController : ApiController
    {
        public IHttpActionResult Get(string userName)
        {
            return this.Ok();
        }

        public IHttpActionResult Get(string userName, string department)
        {
            return this.Ok();
        }
        public IHttpActionResult Get(string userName, string department, string leader)
        {
            return this.Ok();
        }
        public IHttpActionResult Post()
        {
            return this.Ok();
        }
        public IHttpActionResult Post(string userName)
        {
            return this.Ok();
        }
    }
}
