using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Lab.HangfireServer.DAL.EntityModel;

namespace Lab.HangfireServer.Controllers
{
    public class DefaultController : ApiController
    {
        public void Get()
        {
            //var dbContext = new DemoDbContext();
            var demoDbContext = DemoDbContext.Create();


            demoDbContext.Members.ToList();
            //dbContext.Database.Initialize(true);
            //demoDbContext.Database.Initialize(true);
        }
    }
}
