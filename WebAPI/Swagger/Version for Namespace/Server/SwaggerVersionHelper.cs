using System;
using System.Linq;
using System.Web.Http.Description;

namespace Server
{
    public class SwaggerVersionHelper
    {
        public static bool ResolveVersionSupportByRouteConstraint(ApiDescription apiDesc, string targetApiVersion)
        {
            var attr = apiDesc.ActionDescriptor
                              .ControllerDescriptor
                              .GetCustomAttributes<VersionRoute>()
                              .FirstOrDefault();
            return attr.Version == Convert.ToInt32(targetApiVersion.TrimStart('v'));
        }
    }
}