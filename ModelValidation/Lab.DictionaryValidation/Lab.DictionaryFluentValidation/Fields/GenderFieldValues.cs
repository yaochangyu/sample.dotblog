using Lab.DictionaryFluentValidation.Assistants;

namespace Lab.DictionaryFluentValidation.Fields;

public class GenderFieldValues
{
    public const string Male = "male";
    public const string Female = "female";
    public const string NotAvailable = "notAvailable";

    private static readonly Lazy<IEnumerable<string>> s_fieldNameLazy =
        new(ProfileAssistants.GetFieldNames<GenderFieldValues>);

    private static readonly Lazy<Dictionary<string, string>> s_fieldLazy =
        new(ProfileAssistants.GetFields<GenderFieldValues>);

    public static IEnumerable<string> GetFieldValues()
    {
        return s_fieldNameLazy.Value;
    }

    public static Dictionary<string, string> GetValues()
    {
        return s_fieldLazy.Value;
    }
}