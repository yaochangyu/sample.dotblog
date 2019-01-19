using System;
using System.Web.Http.Description;

namespace Server
{
    public class SwaggerVersionHelper
    {
        public static bool ResolveVersionSupportByRouteConstraint(ApiDescription apiDesc, string targetApiVersion)
        {
            var urls = apiDesc.ActionDescriptor.ControllerDescriptor.ControllerType.FullName.Split('.');
            var nameSpace = urls[urls.Length - 2];
            return targetApiVersion.ToLower() == nameSpace.ToLower();
        }
    }
}