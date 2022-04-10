using Lab.DictionaryFluentValidation.Assistants;

namespace Lab.DictionaryFluentValidation.Fields;

public class NameFieldNames
{
    public const string FirstName = "firstName";
    public const string LastName = "lastName";
    public const string FullName = "fullName";

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