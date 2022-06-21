using System.Collections.Concurrent;
using System.Reflection;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite;

public class FieldTypeAssistant
{
    private static ConcurrentDictionary<Type, Dictionary<string, Type>> s_fieldTypeList = new();

    public static Dictionary<string, T> GetEnumValues<T>()
    {
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .ToDictionary(p => p.ToString(), p => p);
    }

    public static Dictionary<string, Type> GetStaticFieldName<T>()
    {
        var type = typeof(T);
        var fieldTypeList = s_fieldTypeList;
        if (fieldTypeList.TryGetValue(type, out var results))
        {
            return results;
        }

        var bindingFlags = BindingFlags.Public
                           | BindingFlags.Static
            ;
        results = new Dictionary<string, Type>();
        var fieldInfosInfos = type.GetFields(bindingFlags);
        foreach (var fieldInfo in fieldInfosInfos)
        {
            var value = fieldInfo.GetValue(null);

            results.Add(value.ToString(), fieldInfo.FieldType);
        }

        fieldTypeList.TryAdd(type, results);
        return results;
    }
}