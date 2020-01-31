using System;
using Nancy;

namespace Lab.ServiceNet4
{
    public class GeneratorNancyModule : NancyModule
    {
        public GeneratorNancyModule()
        {
            this.Get["guid"] = p => Guid.NewGuid().ToString();
        }
    }
}