using StackExchange.Redis;

namespace TestProject1;

[TestClass]
public class RedisConnectionUnitTest
{
    [TestMethod]
    public void SetDTO()
    {
        var connection = new RedisConnection();
        var database = connection.Connect();

        var model = new Model
        {
            Name = "小章",
            Age = 29
        };

        database.Set("dto", model);
        var actual = database.Get<Model>("dto");
        Assert.AreEqual(model, actual);
    }

    [Serializable]
    record Model
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }
}