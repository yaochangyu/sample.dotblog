using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Testcontainers.MongoDb;

namespace Lab.MongoDB.CRUD.TestProject;

[TestClass]
public class UnitTest1
{
    private static MongoDbContainer MongoDbContainer;
    private static MongoClient MongoClient;
    private string TestData = "出發吧，讓我們航向偉大的航道";

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        // MongoDbContainer = new MongoDbBuilder()
        //     .WithPortBinding(27017, true)
        //     .Build();
        // await MongoDbContainer.StartAsync().ConfigureAwait(false);
        // var mongoClientSettings = MongoClientSettings.FromConnectionString(MongoDbContainer.GetConnectionString());
        var mongoClientSettings = new MongoClientSettings()
        {
            Server = new MongoServerAddress("localhost", 27017),
        };

        MongoClient = new MongoClient(mongoClientSettings);
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        // await MongoDbContainer.DisposeAsync().ConfigureAwait(false);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        //復原資料
        var mongoCollection = MongoClient.GetDatabase("example").GetCollection<Product>("product");
        var filter = Builders<Product>.Filter
            .Eq(r => r.Remark, this.TestData);
        var data = mongoCollection.DeleteMany(filter);
    }

    [TestMethod]
    public async Task 新增一筆資料()
    {
        var mongoCollection = MongoClient.GetDatabase("example").GetCollection<Product>("product");
        var expected = new Product
        {
            Id = "1",
            Name = "TV",
            Price = 33.11m,
            Remark = this.TestData
        };

        //新增一筆資料
        await mongoCollection.InsertOneAsync(expected);

        //驗證
        var actual = await mongoCollection.AsQueryable().FirstOrDefaultAsync(p => p.Id == "1");
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public async Task 更新一筆資料()
    {
        var mongoCollection = MongoClient.GetDatabase("example").GetCollection<Product>("product");
        var expected = new Product
        {
            Id = "1",
            Name = "TV",
            Price = 33.11m,
            Remark = this.TestData
        };

        //產生資料
        var products = this.GenerateProducts();
        await mongoCollection.InsertManyAsync(products);

        var filter = Builders<Product>.Filter
            .Eq(restaurant => restaurant.Id, "1");

        var update = Builders<Product>.Update
            .Set(restaurant => restaurant.Name, "TV");
        
        //更新資料
        await mongoCollection.UpdateOneAsync(filter, update);

        //驗證
        var actual = await mongoCollection.AsQueryable().FirstOrDefaultAsync(p => p.Id == "1");

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public async Task 刪除資料()
    {
        var mongoCollection = MongoClient.GetDatabase("example").GetCollection<Product>("product");

        //產生資料
        var products = this.GenerateProducts();
        await mongoCollection.InsertManyAsync(products);

        var filter = Builders<Product>.Filter
            .Eq(restaurant => restaurant.Id, "1");
        
        //更新資料
        await mongoCollection.DeleteOneAsync(filter);

        //驗證
        var actual = await mongoCollection.AsQueryable().FirstOrDefaultAsync(p => p.Id == "1");

        Assert.AreEqual(null, actual);
    }

    
    [TestMethod]
    public async Task 查詢()
    {
        var mongoCollection = MongoClient.GetDatabase("example").GetCollection<Product>("product");
        var expected = new Product
        {
            Id = "1",
            Name = "Air jordan 11",
            Price = 33.11m,
            Remark = this.TestData
        };

        //產生資料
        var products = this.GenerateProducts();
        await mongoCollection.InsertManyAsync(products);

        //查詢1
        var filter = Builders<Product>.Filter.Eq(x => x.Id, "1");
        var find = await mongoCollection.FindAsync(filter);
        var data1 = await find.FirstOrDefaultAsync();

        //驗證
        Assert.AreEqual(expected, data1);

        //查詢2
        var data2 = await mongoCollection.AsQueryable().FirstOrDefaultAsync(p => p.Id == "1");

        //驗證
        Assert.AreEqual(expected, data2);
    }

    private List<Product> GenerateProducts()
    {
        var products = new List<Product>()
        {
            new()
            {
                Id = "1",
                Name = "Air jordan 11",
                Price = 33.11m,
                Remark = this.TestData
            },
            new()
            {
                Id = "2",
                Name = "Air jordan 12",
                Price = 33.12m,
                Remark = this.TestData
            },
            new()
            {
                Id = "3",
                Name = "Air jordan 13",
                Price = 33.13m,
                Remark = this.TestData
            }
        };
        return products;
    }

    
    public record Product
    {
        public string Id { get; init; }

        public string Name { get; init; }

        public decimal Price { get; init; }

        public string Remark { get; init; }
    }
}