namespace Lab.XmlSerializer.ConsoleApp
{
    // 測試資料生成器
    public class TestDataGenerator
    {
        private static readonly Random Random = new Random();
        private static readonly string[] Names = { "余小章", "王小明", "李小華", "陳小美", "林小強" };
        private static readonly string[] Hobbies = { "登山", "健身", "游泳", "跑步", "騎車", "程式設計", "閱讀" };
        private static readonly string[] Products = { "登山背包", "健身器材", "運動鞋", "水壺", "帳篷" };

        public static TestPerson GenerateRandomPerson()
        {
            return new TestPerson
            {
                Name = Names[Random.Next(Names.Length)],
                Age = Random.Next(20, 60),
                Email = $"user{Random.Next(1000)}@example.com",
                Hobbies = GetRandomHobbies(),
                CreatedDate = DateTime.Now.AddDays(-Random.Next(365))
            };
        }

        public static TestOrder GenerateRandomOrder()
        {
            var itemCount = Random.Next(1, 5);
            var items = new List<TestOrderItem>();
          
            for (int i = 0; i < itemCount; i++)
            {
                items.Add(new TestOrderItem
                {
                    ProductName = Products[Random.Next(Products.Length)],
                    Quantity = Random.Next(1, 10),
                    Price = (decimal)(Random.NextDouble() * 1000 + 10)
                });
            }

            return new TestOrder
            {
                OrderId = Guid.NewGuid().ToString(),
                Amount = items.Sum(x => x.Price * x.Quantity),
                Items = items,
                OrderDate = DateTime.Now.AddDays(-Random.Next(30))
            };
        }

        private static List<string> GetRandomHobbies()
        {
            var count = Random.Next(1, 4);
            return Hobbies.OrderBy(x => Random.Next()).Take(count).ToList();
        }
    }
}
