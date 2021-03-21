using System.Net.Http;
using System.Web.Http;

namespace WebApiNet48.Controllers
{
    public class DefaultController : ApiController
    {
        private IMessager Messager { get; set; }

        public DefaultController(IMessager messager)
        {
            this.Messager = messager;
        }

        [HttpGet]
        public IHttpActionResult Get()
        {
            var content = $"Messager:{this.Messager.OperationId}";
            return this.Ok(content);
        }

        [HttpGet]
        public IHttpActionResult Get1()
        {
            var messager = InstanceManager.Messager;

            var content = $"Messager:{messager.OperationId}";
            return this.Ok(content);
        }
    }

    public class InstanceManager
    {
        public static IMessager Messager { get; set; }
    }
}