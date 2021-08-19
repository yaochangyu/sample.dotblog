using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace WebApiOwinNet48.Controllers
{
    public class DefaultController : ApiController
    {
        private Commander _cmder;

        public DefaultController(Commander cmder)
        {
            this._cmder = cmder;
        }

        // GET
        public async Task<IHttpActionResult> Get()
        {
            // this._cmder.Get();
            Console.WriteLine($"我在 DefaultController.Get ，Command.Id = {this._cmder.Id}");
            return this.Ok(this._cmder);
        }

        private class Member
        {
            public Guid Id { get; set; }

            public int Age { get; set; }
        }
    }
}