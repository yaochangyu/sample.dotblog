using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DefaultController : ControllerBase
    {

        private readonly ILogger<DefaultController> _logger;

        public DefaultController(ILogger<DefaultController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("{type}")]
        public IActionResult Get(string type)
        {
            var fileProvider = this.HttpContext.RequestServices.GetService<IFileProvider>(type);
            var result        = fileProvider.Print();
            return this.Ok(result);
        }
    }
}