using System.Reflection;

namespace Lab.DictionaryFluentValidation2.Assistants;

public class ProfileAssistants
{
    public static Dictionary<string, string> GetFieldNames<T>()
    {
        var type = typeof(T);

        var bindingFlags = BindingFlags.Public
                           | BindingFlags.Static
            ;
        var results = new Dictionary<string, string>();
        var fieldInfosInfos = type.GetFields(bindingFlags);
        foreach (var fieldInfo in fieldInfosInfos)
        {
            results.Add(fieldInfo.GetValue(null).ToString(), null);
        }

        return results;
    }
}