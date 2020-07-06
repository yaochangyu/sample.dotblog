using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNetCore31.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DefaultController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<Member>> Get()
        {
            var key = typeof(Member).FullName;
            var member = this.HttpContext.Items[key] as Member; 
            return this.Ok(member);
        }
    }
}