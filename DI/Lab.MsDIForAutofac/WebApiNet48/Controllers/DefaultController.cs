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
            if (this.Messager == null)
            {
                var dependencyScope = this.Request.GetDependencyScope();
                this.Messager = dependencyScope.GetService(typeof(IMessager)) as IMessager;
            }

            var content = $"Messager:{this.Messager.OperationId}";
            return this.Ok(content);
        }
    }
}