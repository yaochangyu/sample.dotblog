using Lab.Redis.Client;
using StackExchange.Redis;

namespace TestProject1;

[TestClass]
public class RedisConnectionUnitTest
{
    [TestMethod]
    public void SetDTO()
    {
        var connection = new RedisConnection();

        // var database = connection.Connect("localhost:6379");
        var config = ConfigurationOptions.Parse("127.0.0.1:6379");
        var database = connection.Connect(config.ToString());
        var model = new Model
        {
            Name = "小章",
            Age = 29
        };

        database.Set("dto", model, options: JsonSerializeFactory.CreateDefault());
        var actual = database.Get<Model>("dto", options: JsonSerializeFactory.CreateDefault());
        Assert.AreEqual(model, actual);
    }

    [Serializable]
    record Model
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }
}