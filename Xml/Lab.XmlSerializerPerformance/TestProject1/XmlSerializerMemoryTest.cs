using System.Diagnostics;
using Lab.XmlSerializer.ConsoleApp;

namespace TestProject1
{
    [TestClass]

    // 主要測試類別
    public class XmlSerializerMemoryTest
    {
        private const int TestIterations = 10000000;
        private const int TypeVariations = 10;

        [TestMethod]
        public async Task TestMethod1()
        {
            Console.WriteLine("XmlSerializer 記憶體洩漏測試");
            Console.WriteLine("=====================================");

            // 測試 1: 證明記憶體洩漏問題
            await TestMemoryLeak();

            // 測試 2: 證明快取解決方案
            // await TestCachedSolution();

            // 測試 3: 效能比較
            // await TestPerformanceComparison();

            // 測試 4: 並行測試
            // await TestConcurrentAccess();
        }

        // 測試 1: 記憶體洩漏測試
        public static async Task TestMemoryLeak()
        {
            var monitor = new MemoryMonitor("記憶體洩漏測試 (Bad Implementation)");
            var badService = new BadXmlSerializerService();

            // 使用多種不同的類型來加劇記憶體洩漏
            for (int i = 0; i < TestIterations; i++)
            {
                // 序列化不同類型的物件
                var person = TestDataGenerator.GenerateRandomPerson();
                var order = TestDataGenerator.GenerateRandomOrder();

                var personXml = badService.Serialize(person);
                var orderXml = badService.Serialize(order);

                var deserializedPerson = badService.Deserialize2<TestPerson>(personXml);
                var deserializedOrder = badService.Deserialize2<TestOrder>(orderXml);
                Task.WhenAll(new List<Task>() { deserializedOrder, deserializedPerson });
                if (i % 100 == 0)
                {
                    monitor.ShowCurrentMemory($"迭代 {i + 1}");
                }
            }

            monitor.Finish();
        }

        // 測試 2: 快取解決方案測試
        public static async Task TestCachedSolution()
        {
            var monitor = new MemoryMonitor("快取解決方案測試 (Good Implementation)");
            var goodService = new GoodXmlSerializerService();

            Console.WriteLine($"快取初始數量: {GoodXmlSerializerService.CacheCount}");

            for (int i = 0; i < TestIterations; i++)
            {
                var person = TestDataGenerator.GenerateRandomPerson();
                var order = TestDataGenerator.GenerateRandomOrder();

                var personXml = goodService.Serialize(person);
                var orderXml = goodService.Serialize(order);

                var deserializedPerson = goodService.Deserialize<TestPerson>(personXml);
                var deserializedOrder = goodService.Deserialize<TestOrder>(orderXml);

                if (i % 100 == 0)
                {
                    monitor.ShowCurrentMemory($"迭代 {i + 1}");
                    Console.WriteLine($"快取數量: {GoodXmlSerializerService.CacheCount}");
                }
            }

            Console.WriteLine($"最終快取數量: {GoodXmlSerializerService.CacheCount}");
            monitor.Finish();
        }

        // 測試 3: 效能比較
        public static async Task TestPerformanceComparison()
        {
            Console.WriteLine("=== 效能比較測試 ===");

            var badService = new BadXmlSerializerService();
            var goodService = new GoodXmlSerializerService();
            var testData = Enumerable.Range(0, TestIterations)
                .Select(_ => TestDataGenerator.GenerateRandomPerson())
                .ToList();

            // 測試 Bad Implementation
            var sw1 = Stopwatch.StartNew();
            foreach (var person in testData)
            {
                var xml = badService.Serialize(person);
                var deserialized = badService.Deserialize<TestPerson>(xml);
            }
            sw1.Stop();

            // 測試 Good Implementation
            var sw2 = Stopwatch.StartNew();
            foreach (var person in testData)
            {
                var xml = goodService.Serialize(person);
                var deserialized = goodService.Deserialize<TestPerson>(xml);
            }
            sw2.Stop();

            Console.WriteLine($"Bad Implementation: {sw1.ElapsedMilliseconds} ms");
            Console.WriteLine($"Good Implementation: {sw2.ElapsedMilliseconds} ms");
            Console.WriteLine($"效能提升: {(double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds:F2}x");
            Console.WriteLine();
        }

        // 測試 4: 並行存取測試
        public static async Task TestConcurrentAccess()
        {
            Console.WriteLine("=== 並行存取測試 ===");

            var goodService = new GoodXmlSerializerService();
            GoodXmlSerializerService.ClearCache();

            var tasks = new List<Task>();
            var concurrentUsers = 10;
            var operationsPerUser = 50;

            var monitor = new MemoryMonitor("並行存取測試");

            for (int user = 0; user < concurrentUsers; user++)
            {
                var userId = user;
                tasks.Add(Task.Run(() =>
                {
                    for (int op = 0; op < operationsPerUser; op++)
                    {
                        var person = TestDataGenerator.GenerateRandomPerson();
                        var order = TestDataGenerator.GenerateRandomOrder();

                        var personXml = goodService.Serialize(person);
                        var orderXml = goodService.Serialize(order);

                        var deserializedPerson = goodService.Deserialize<TestPerson>(personXml);
                        var deserializedOrder = goodService.Deserialize<TestOrder>(orderXml);
                    }
                    Console.WriteLine($"使用者 {userId} 完成");
                }));
            }

            await Task.WhenAll(tasks);

            Console.WriteLine($"並行測試完成，快取數量: {GoodXmlSerializerService.CacheCount}");
            // monitor.Finish();
        }
    }


}
