using System.Collections.Generic;
using System.Web.Http;

namespace Server.Controllers
{
    public class ValuesController : ApiController
    {
        // DELETE api/values/5
        public void Delete(int id)
        {
        }

        // GET api/values
        public IEnumerable<string> Get()
        {
            return new[] {"value1", "value2"};
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody] string value)
        {
        }
    }
}