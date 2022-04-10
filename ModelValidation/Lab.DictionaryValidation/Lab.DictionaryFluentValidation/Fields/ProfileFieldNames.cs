using Lab.DictionaryFluentValidation.Assistants;

namespace Lab.DictionaryFluentValidation.Fields;

public class ProfileFieldNames
{
    public const string Name = "name";
    public const string Gender = "gender";
    public const string Birthday = "birthday";
    public const string ContactEmail = "contactEmail";

    private static readonly Lazy<IEnumerable<string>> s_fieldNames =
        new(ProfileAssistants.GetFieldNames<ProfileFieldNames>);

    private static Lazy<Dictionary<string, string>> s_fields =
        new(ProfileAssistants.GetFields<ProfileFieldNames>);

    public static IEnumerable<string> GetFieldNames()
    {
        return s_fieldNames.Value;
    }

    public static Dictionary<string, string> GetFields()
    {
        return s_fields.Value;
    }
}