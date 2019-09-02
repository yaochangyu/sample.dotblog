using System.Web.Http;

namespace Server.UnitTest.Controllers
{
    public class DefaultController : ApiController
    {
        public IProductRepository ProductRepository { get; set; }

        public DefaultController(IProductRepository productRepository)
        {
            this.ProductRepository = productRepository;
        }

        // GET api/values/
        public string Get()
        {
            return this.ProductRepository.GetName();
        }
    }
}