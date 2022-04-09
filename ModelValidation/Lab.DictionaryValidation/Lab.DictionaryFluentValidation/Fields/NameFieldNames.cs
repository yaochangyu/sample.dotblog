using Lab.DictionaryFluentValidation.Assistants;

namespace Lab.DictionaryFluentValidation.Fields;

public class NameFieldNames
{
    public static readonly string FirstName = "firstName";
    public static readonly string LastName = "lastName";
    public static readonly string FullName = "fullName";

    private static readonly Lazy<IEnumerable<string>> s_fieldNameLazy =
        new(ProfileAssistants.GetFieldNames<NameFieldNames>);

    private static readonly Lazy<Dictionary<string, string>> s_fieldLazy =
        new(ProfileAssistants.GetFields<NameFieldNames>);

    public static IEnumerable<string> GetFieldNames()
    {
        return s_fieldNameLazy.Value;
    }

    public static Dictionary<string, string> GetFields()
    {
        return s_fieldLazy.Value;
    }
}