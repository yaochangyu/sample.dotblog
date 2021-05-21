using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NET5.TestProject.File;

namespace NET5.TestProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MultiController : ControllerBase
    {
        private readonly IFileProvider _fileProvider;

        private readonly ILogger<AutofacController> _logger;

        // public MultiController(ILogger<AutofacController> logger,
        //                        IEnumerable<IFileProvider> pool)
        // {
        //     this._logger       = logger;
        //     this._fileProvider = pool.FirstOrDefault(p => p.GetType().Name == "ZipFileProvider");
        //     var msg = $"{this._fileProvider.Print()} in {this.GetType().Name} constructor";
        //     Console.WriteLine(msg);
        // }
        //
        // [HttpGet]
        // public IActionResult Get()
        // {
        //     var serviceProvider =  this.HttpContext.RequestServices;
        //     var pool         = serviceProvider.GetServices<IFileProvider>();
        //     var fileProvider = pool.FirstOrDefault(p => p.GetType().Name == "ZipFileProvider");
        //     return this.Ok(fileProvider.Print());
        // }

        public MultiController(ILogger<AutofacController>        logger,
                               Dictionary<string, IFileProvider> pool)
        {
            this._logger       = logger;
            this._fileProvider = pool["zip"];
            var msg = $"{this._fileProvider.Print()} in {this.GetType().Name} constructor";
            Console.WriteLine(msg);
        }
        
        [HttpGet]
        [Route("{key}")]
        public IActionResult Get(string key)
        {
            var serviceProvider = this.HttpContext.RequestServices;
            var pool = serviceProvider.GetService<Dictionary<string, IFileProvider>>();
            var fileProvider = pool[key];
            return this.Ok(fileProvider.Print());
        }
    }
}