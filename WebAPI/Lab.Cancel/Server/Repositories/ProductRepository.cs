using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Server.EntityModel;

namespace Server.Repositories
{
    internal interface IProductRepository
    {
        Task<int> InsertAsync(ICollection<Product> sources, CancellationToken cancelToken);
    }

    public class ProductRepository : IProductRepository
    {
        public async Task<int> InsertAsync(ICollection<Product> sources, CancellationToken cancelToken)
        {
            var result = 0;
            using (var dbContext = TestDbContext.Create())
            {
                dbContext.Products.AddRange(sources);
                result = await dbContext.SaveChangesAsync(cancelToken);
            }

            return result;
        }
    }
}