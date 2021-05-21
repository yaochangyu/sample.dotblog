using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.AttributeFilters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NET5.TestProject.File;

namespace NET5.TestProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AutofacController : ControllerBase
    {
        private readonly IFileProvider _fileProvider;

        private readonly ILogger<AutofacController> _logger;

        public AutofacController(ILogger<AutofacController>       logger,
                                 [KeyFilter("zip")] IFileProvider fileProvider)
        {
            this._logger       = logger;
            this._fileProvider = fileProvider;
            var msg = $"{this._fileProvider.Print()} in {this.GetType().Name} constructor";
            Console.WriteLine(msg);
        }

        [HttpGet]
        [Route("{key}")]
        public IActionResult Get(string key)
        {
            var serviceProvider        = this.HttpContext.RequestServices;
            var autofacServiceProvider = (AutofacServiceProvider) serviceProvider;
            var fileProvider           = autofacServiceProvider.LifetimeScope.ResolveKeyed<IFileProvider>(key);
            return this.Ok(fileProvider.Print());
        }
    }
}