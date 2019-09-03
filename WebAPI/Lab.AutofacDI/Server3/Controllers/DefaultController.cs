using System.Web.Http;

namespace Server3.Controllers
{
    public class DefaultController : ApiController
    {
        public IProductRepository ProductRepository { get; set; }

        public DefaultController()
        {
            //if (this.ProductRepository == null)
            //{
            //    this.ProductRepository = new ProductRepository();
            //}
        }

        //public DefaultController(IProductRepository repository)
        //{
        //    this.ProductRepository = repository;
        //}
        // GET api/default/5
        public string Get()
        {
            return this.ProductRepository.GetName();
        }
    }
}