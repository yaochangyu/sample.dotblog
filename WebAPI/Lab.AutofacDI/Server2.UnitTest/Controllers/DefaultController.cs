using System.Web.Http;
using Server2.UnitTest.Repositories;

namespace Server2.UnitTest.Controllers
{
    public class DefaultController : ApiController
    {
        // GET api/default/
        public string Get()
        {
            return InstanceUtility.ProductRepository.GetName();
        }
    }

    internal class InstanceUtility
    {
        private static IProductRepository s_productRepository;

        public static IProductRepository ProductRepository
        {
            get
            {
                if (s_productRepository == null)
                {
                    s_productRepository = new ProductRepository();
                }

                return s_productRepository;
            }
            set => s_productRepository = value;
        }
    }
}