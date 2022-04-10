using Lab.DictionaryFluentValidation.Assistants;

namespace Lab.DictionaryFluentValidation.Fields;

public class BirthdayFieldNames
{
    public const string Year = "year";
    public const string Month = "month";
    public const string Day = "day";

    private static readonly Lazy<IEnumerable<string>> s_fieldNameLazy =
        new(ProfileAssistants.GetFieldNames<BirthdayFieldNames>);

    private static readonly Lazy<Dictionary<string, string>> s_fieldLazy =
        new(ProfileAssistants.GetFields<BirthdayFieldNames>);

    public static IEnumerable<string> GetFieldNames()
    {
        return s_fieldNameLazy.Value;
    }

    public static Dictionary<string, string> GetFields()
    {
        return s_fieldLazy.Value;
    }
}