using Lab.XmlSerializer.WebApi.Controllers;

namespace TestProject1
{
    [TestClass]
    public class GoodXmlSerializerServiceTest
    {
        int Count = 1000000;

        [TestMethod]
        public async Task BadTest()
        {
            var service = new BadXmlSerializerService();
            var person = new TestPerson
            {
                Name = null,
                Age = 0,
                Email = null,
                CreatedDate = default
            };
            for (int i = 0; i < Count; i++)
            {
                service.Serialize(person);
            }
        }

        [TestMethod]
        public async Task GoodTest()
        {
            var service = new GoodXmlSerializerService();
            var person = new TestPerson
            {
                Name = null,
                Age = 0,
                Email = null,
                CreatedDate = default
            };
            for (int i = 0; i < Count; i++)
            {
                service.Serialize(person);
            }
        }
    }
}
