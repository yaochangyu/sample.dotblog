namespace Server3
{
    public class ProductRepository : IProductRepository
    {
        public string GetName()
        {
            return "Product";
        }
    }
}