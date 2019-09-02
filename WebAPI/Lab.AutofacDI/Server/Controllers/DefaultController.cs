using System.Collections.Generic;
using System.Web.Http;

namespace Server.Controllers
{
    public class DefaultController : ApiController
    {
        private IService Service;

        public DefaultController(IService service)
        {
            this.Service = service;
        }

        // DELETE: api/Default/5
        public void Delete(int id)
        {
        }

        // GET: api/Default
        public IEnumerable<string> Get()
        {
            return new[] {this.Service.GetName()};
        }

        // GET: api/Default/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Default
        public void Post([FromBody] string value)
        {
        }

        // PUT: api/Default/5
        public void Put(int id, [FromBody] string value)
        {
        }
    }
}