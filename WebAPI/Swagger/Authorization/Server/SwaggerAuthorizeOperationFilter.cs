using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Filters;
using Swashbuckle.Swagger;

namespace Server
{
    public class SwaggerAuthorizeOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            var filterPipeline = apiDescription.ActionDescriptor.GetFilterPipeline();
            // check if authorization is required
            var isAuthorized = filterPipeline
                               .Select(filterInfo => filterInfo.Instance)
                               .Any(filter => filter is IAuthorizationFilter);
            // check if anonymous access is allowed
            var allowAnonymous = apiDescription.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Any();
            if (isAuthorized && !allowAnonymous)
            {
                if (operation.security == null)
                {
                    operation.security = new List<IDictionary<string, IEnumerable<string>>>();
                }

                var auth = new Dictionary<string, IEnumerable<string>>
                {
                    {"basic", Enumerable.Empty<string>()}
                };
               
                operation.security.Add(auth);
                //operation.parameters.Add(new Parameter
                //{
                //    name = "Authorization",
                //    @in = "header", // specify that the value will be sent in the header
                //    description = "Basic U3dhZ2dlcjpUZXN0", // Basic Swagger:Test
                //    required = true, // parameter required
                //    type = "string"
                //});
            }
        }
    }
}