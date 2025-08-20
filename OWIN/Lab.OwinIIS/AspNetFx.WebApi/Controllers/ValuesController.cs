using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.Owin;

namespace AspNetFx.WebApi.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public async Task<IHttpActionResult> Get()
        {
            var _provider = Request.GetHttpContextProvider();
            var serverVariables = _provider.GetServerVariables();
            return Ok(serverVariables.ToDictionary(x => x.Key, x => x.Value.ToString()));
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }
    }
}
