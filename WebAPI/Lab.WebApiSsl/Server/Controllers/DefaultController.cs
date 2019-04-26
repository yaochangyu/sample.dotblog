using System.Collections.Generic;
using System.Web.Http;

namespace Server.Controllers
{
    public class DefaultController : ApiController
    {
        // GET: api/Default
        public IEnumerable<string> Get()
        {
            return new[] {"value1", "value2"};
        }

        // GET: api/Default/5
        public string Get(int id)
        {
            return "value";
        }

        [HttpPost]
        // POST: api/Default
        public void Post([FromBody] string value)
        {
        }

        // PUT: api/Default/5
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/Default/5
        public void Delete(int id)
        {
        }
    }
}