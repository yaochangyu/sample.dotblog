using System;
using System.Security.Principal;
using Nancy;
using Nancy.Security;

namespace Lab.Security.BasicAuthentication
{
    public class GeneratorNancyModule  : NancyModule
    {
        public GeneratorNancyModule()
        {
            this.RequiresAuthentication();
            this.Get["guid"] = p => Guid.NewGuid().ToString();
            this.Get["id"] = p => Guid.NewGuid().ToString();
        }
    }
}