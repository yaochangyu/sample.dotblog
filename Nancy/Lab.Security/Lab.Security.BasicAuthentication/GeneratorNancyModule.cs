using System;
using Nancy;

namespace Lab.Security.BasicAuthentication
{
    public class GeneratorNancyModule : NancyModule
    {
        public GeneratorNancyModule()
        {
            //this.RequiresAuthentication();
            this.RequiresAuthentication(new[] {"name:id"});
            this.Get["name:guid", "guid"] = p => Guid.NewGuid().ToString();
            this.Get["name:id", "id"]     = p => Guid.NewGuid().ToString();
        }
    }
}