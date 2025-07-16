namespace Lab.XmlSerializer.WebApi.Controllers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;
    using Microsoft.AspNetCore.Mvc;

    namespace XmlSerializerLeakTest
    {
        // 測試用的資料類別
        [Serializable]
        [XmlRoot("Person")]
        public class TestPerson
        {
            [XmlElement("Name")]
            public string Name { get; set; }

            [XmlElement("Age")]
            public int Age { get; set; }

            [XmlElement("Email")]
            public string Email { get; set; }

            [XmlElement("CreatedDate")]
            public DateTime CreatedDate { get; set; }
        }

        // 錯誤的 XmlSerializer 實作
        public class BadXmlSerializerService
        {
            public string Serialize(TestPerson person)
            {
                // ❌ 每次都建立新的 XmlSerializer，並且使用 XmlRootAttribute
                var xmlRoot = new XmlRootAttribute("Person");
                var serializer = new XmlSerializer(typeof(TestPerson), xmlRoot);
                using var writer = new StringWriter();
                serializer.Serialize(writer, person);
                return writer.ToString();
            }
        }

        // 正確的 XmlSerializer 實作
        public class GoodXmlSerializerService
        {
            // ✅ 使用 ConcurrentDictionary 快取 XmlSerializer 實例
            private static readonly ConcurrentDictionary<Type, XmlSerializer> Cache
                = new ConcurrentDictionary<Type, XmlSerializer>();

            private static XmlSerializer GetSerializer(Type type, XmlRootAttribute xmlRoot)
            {
                return Cache.GetOrAdd(type, _ => new XmlSerializer(type, xmlRoot));
            }

            public string Serialize(TestPerson person)
            {
                var xmlRoot = new XmlRootAttribute("Person");
                var serializer = GetSerializer(typeof(TestPerson), xmlRoot);
                using var writer = new StringWriter();
                serializer.Serialize(writer, person);
                return writer.ToString();
            }
        }

        [ApiController]
        [Route("[controller]")]
        public class XmlSerializerController : ControllerBase
        {
            private readonly BadXmlSerializerService _badService = new BadXmlSerializerService();
            private readonly GoodXmlSerializerService _goodService = new GoodXmlSerializerService();

            [HttpGet("bad")]
            public async Task<ActionResult> BadSerialize()
            {
                var person = new TestPerson
                {
                    Name = "yao-bad",
                    Age = 18,
                    Email = "yao-bad@aa.bb",
                    CreatedDate = DateTime.Now
                };
                var xml = _badService.Serialize(person);
                return this.Ok(xml);
            }

            [HttpGet("good")]
            public async Task<ActionResult> GoodSerialize()
            {
                var person = new TestPerson
                {
                    Name = "yao-good",
                    Age = 18,
                    Email = "yao-good@aa.bb",
                    CreatedDate = DateTime.Now
                };
                var xml = _goodService.Serialize(person);
                return this.Ok(xml);
            }
        }
    }
}
