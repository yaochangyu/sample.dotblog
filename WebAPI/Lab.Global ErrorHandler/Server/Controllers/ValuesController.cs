using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Server.Controllers
{
    public class ValuesController : ApiController
    {
        public void Post(Employee request)
        {
            throw new Exception("GG~");
        }
    }

    public class Employee
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
