using System.IO;
using System.Reflection;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;

namespace WebApplication1
{
    public class LogActionParameterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var actionParameters = filterContext.ActionParameters;
            var controllerName   = filterContext.Controller.GetType().Name;
            var actionName       = filterContext.ActionDescriptor.ActionName;
            var parametersInfo = JsonConvert.SerializeObject(actionParameters, new JsonSerializerSettings
            {
                ContractResolver = new RemoveStreamResolver()
            });

            var message = string.Format(
                                        "{0}.{1}() => {2}",
                                        controllerName,
                                        actionName,
                                        string.IsNullOrEmpty(parametersInfo) ? "(void)" : parametersInfo
                                       );

            var logger = LogManager.GetCurrentClassLogger();
            logger.Info(message);
        }

        private class RemoveStreamResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);
                if (typeof(Stream).IsAssignableFrom(property.PropertyType))
                {
                    property.Ignored = true;
                }

                return property;
            }
        }
    }
}