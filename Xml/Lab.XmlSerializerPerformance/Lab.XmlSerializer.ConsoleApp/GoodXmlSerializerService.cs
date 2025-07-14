using System.Collections.Concurrent;

namespace Lab.XmlSerializer.ConsoleApp
{
    // 改善的實作 - 使用快取
    public class GoodXmlSerializerService
    {
        // ✅ 使用 ConcurrentDictionary 快取 XmlSerializer 實例
        private static readonly ConcurrentDictionary<Type, System.Xml.Serialization.XmlSerializer> Cache
            = new ConcurrentDictionary<Type, System.Xml.Serialization.XmlSerializer>();

        private static System.Xml.Serialization.XmlSerializer GetSerializer<T>()
        {
            return Cache.GetOrAdd(typeof(T), type => new System.Xml.Serialization.XmlSerializer(type));
        }

        public string Serialize<T>(T obj)
        {
            var serializer = GetSerializer<T>();
            using var writer = new StringWriter();
            serializer.Serialize(writer, obj);
            return writer.ToString();
        }

        public T Deserialize<T>(string xml)
        {
            var serializer = GetSerializer<T>();
            using var reader = new StringReader(xml);
            return (T)serializer.Deserialize(reader);
        }

        // 提供查看快取狀態的方法
        public static int CacheCount => Cache.Count;
        public static void ClearCache() => Cache.Clear();
    }
}
