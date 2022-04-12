using Lab.DictionaryFluentValidation2.Assistants;

namespace Lab.DictionaryFluentValidation2.Fields;

public class ProfileTypeNames
{
    public const string Name = "name";
    public const string Gender = "gender";
    public const string Birthday = "birthday";
    public const string ContactEmail = "contactEmail";

    private static readonly Lazy<Dictionary<string, string>> s_fieldNamesLazy =
        new(() => ProfileAssistants.GetFieldNames<ProfileTypeNames>());

    private static Dictionary<string, string> FieldNames => s_fieldNamesLazy.Value;

    public static Dictionary<string, string> GetFieldNames()
    {
        return FieldNames;
    }
}