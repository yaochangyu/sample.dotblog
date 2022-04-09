using Lab.DictionaryFluentValidation.Assistants;

namespace Lab.DictionaryFluentValidation.Fields;

public class GenderFieldNames
{
    public static string Male = "male";
    public static string Female = "female";
    public static string NotAvailable = "notAvailable";

    private static readonly Lazy<IEnumerable<string>> s_fieldNameLazy =
        new(ProfileAssistants.GetFieldNames<GenderFieldNames>);

    private static readonly Lazy<Dictionary<string, string>> s_fieldLazy =
        new(ProfileAssistants.GetFields<GenderFieldNames>);

    public static IEnumerable<string> GetFieldNames()
    {
        return s_fieldNameLazy.Value;
    }

    public static Dictionary<string, string> GetFields()
    {
        return s_fieldLazy.Value;
    }
}