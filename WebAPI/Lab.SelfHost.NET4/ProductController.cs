using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Lab.SelfHost.NET4
{
    public class ProductController : ApiController
    {
        private readonly Product[] products =
        {
            new Product {Id = 1, Name = "Tomato Soup", Category = "Groceries", Price = 1},
            new Product {Id = 2, Name = "Yo-yo", Category       = "Toys", Price      = 3.75M},
            new Product {Id = 3, Name = "Hammer", Category      = "Hardware", Price  = 16.99M}
        };

        [HttpGet]
        public IEnumerable<Product> GetAllProducts()
        {
            return this.products;
        }

        [HttpGet]
        public Product GetProductById(int id)
        {
            var product = this.products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return product;
        }

        [HttpGet]
        public IEnumerable<Product> GetProductsByCategory(string category)
        {
            return this.products.Where(p => string.Equals(p.Category, category,
                                                          StringComparison.OrdinalIgnoreCase));
        }
    }
}