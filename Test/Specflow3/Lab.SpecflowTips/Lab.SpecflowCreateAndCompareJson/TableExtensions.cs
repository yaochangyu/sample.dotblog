using System.Text.Json;
using TechTalk.SpecFlow;

namespace Lab.SpecflowCreateAndCompareJson;

public static class TableExtensions
{
    public static IEnumerable<T>? CreateJsonSet<T>(this Table table)
    {
        var results = new List<T>();
        var type = typeof(T);
        foreach (var row in table.Rows)
        {
            var instance = Activator.CreateInstance<T>();
            foreach (var header in table.Header)
            {
                var property = type.GetProperty(header);
                if (property == null)
                {
                    continue;
                }

                var value = row[header];
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var propertyType = property.PropertyType;

                //若是泛型且是集合
                if (propertyType.IsGenericType
                    && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listType = propertyType.GetGenericArguments()[0];
                    var list = JsonSerializer.Deserialize(value, typeof(List<>).MakeGenericType(listType));
                    property.SetValue(instance, list);
                }

                //若是物件且不是字串
                else if (propertyType.IsClass
                         && propertyType != typeof(string))
                {
                    var obj = JsonSerializer.Deserialize(value, propertyType);
                    property.SetValue(instance, obj);
                }

                //若是列舉
                else if (propertyType.IsEnum)
                {
                    property.SetValue(instance, Enum.Parse(propertyType, value));
                }
                else
                {
                    property.SetValue(instance, Convert.ChangeType(value, propertyType));
                }
            }

            results.Add(instance);
        }

        return results;
    }
}