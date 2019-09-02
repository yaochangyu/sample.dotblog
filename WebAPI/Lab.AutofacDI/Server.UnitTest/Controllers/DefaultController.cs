using System.Web.Http;
using Server.UnitTest.Repositories;

namespace Server.UnitTest.Controllers
{
    public class DefaultController : ApiController
    {
        public IProductRepository ProductRepository { get; set; }

        public DefaultController(IProductRepository productRepository)
        {
            this.ProductRepository = productRepository;
        }

        // GET api/default/
        public string Get()
        {
            return this.ProductRepository.GetName();
        }
    }
}