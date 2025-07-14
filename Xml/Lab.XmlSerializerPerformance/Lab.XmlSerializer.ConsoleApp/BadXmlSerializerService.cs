namespace Lab.XmlSerializer.ConsoleApp
{
    // 有問題的實作 - 每次都建立新的 XmlSerializer
    public class BadXmlSerializerService
    {
        public string Serialize<T>(T obj)
        {
            // ❌ 每次都建立新的 XmlSerializer，會造成記憶體洩漏
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, obj);
            return writer.ToString();
        }

        public T Deserialize<T>(string xml)
        {
            // ❌ 每次都建立新的 XmlSerializer，會造成記憶體洩漏
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using var reader = new StringReader(xml);
            return (T)serializer.Deserialize(reader);
        }
        
        public async Task<T> Deserialize2<T>(string xml)
        {
           return await Task.Run(()=>
            {
                // ❌ 每次都建立新的 XmlSerializer，會造成記憶體洩漏
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                using var reader = new StringReader(xml);
                return (T)serializer.Deserialize(reader);
            });
        }

    }
}
