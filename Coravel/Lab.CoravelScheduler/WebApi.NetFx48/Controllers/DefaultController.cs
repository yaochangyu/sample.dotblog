using System.Threading.Tasks;
using System.Web.Http;

namespace WebApi.NetFx48.Controllers
{
    public class DefaultController : ApiController
    {
        public async Task<IHttpActionResult> Get()
        {
            return this.Ok("OK");
        }
    }
}