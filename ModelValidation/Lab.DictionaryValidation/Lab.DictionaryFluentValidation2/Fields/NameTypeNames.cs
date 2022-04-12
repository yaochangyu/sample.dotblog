using Lab.DictionaryFluentValidation2.Assistants;

namespace Lab.DictionaryFluentValidation2.Fields;

public class NameTypeNames
{
    public const string FirstName = "firstName";
    public const string LastName = "lastName";
    public const string FullName = "fullName";
    
    private static readonly Lazy<Dictionary<string, string>> s_fieldNamesLazy =
        new(() => ProfileAssistants.GetFieldNames<NameTypeNames>());

    private static Dictionary<string, string> FieldNames => s_fieldNamesLazy.Value;

    public static Dictionary<string, string> GetFieldNames()
    {
        return FieldNames;
    }
}