using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace AspNetFx.WebApi.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public async Task<IHttpActionResult> Get(CancellationToken cancel)
        {
            var factory = new HttpContextProviderFactory();
            var provider = factory.CreateProvider();
            var serverVariables = provider.GetServerVariables();
            return Ok(serverVariables);
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // GET api/values/test?name=test
        [HttpGet]
        [Route("api/values/test")]
        public IHttpActionResult GetTest(string name)
        {
            var factory = new HttpContextProviderFactory();
            var provider = factory.CreateProvider();
            
            var result = new
            {
                QueryStringValue = provider.GetQueryString("name"),
                UserAgentHeader = provider.GetHeader("User-Agent"),
                AllQueryString = provider.GetQueryString(name) // 取得指定查詢參數
            };
            
            return Ok(result);
        }

        // POST api/values
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}