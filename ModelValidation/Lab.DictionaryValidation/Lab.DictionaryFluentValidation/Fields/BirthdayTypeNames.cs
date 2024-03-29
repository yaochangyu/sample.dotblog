using Lab.DictionaryFluentValidation.Assistants;

namespace Lab.DictionaryFluentValidation.Fields;

public class BirthdayTypeNames
{
    public const string Year = "year";
    public const string Month = "month";
    public const string Day = "day";

    private static readonly Lazy<Dictionary<string, string>> s_fieldNamesLazy =
        new(() => ProfileAssistants.GetFieldNames<BirthdayTypeNames>());

    private static Dictionary<string, string> FieldNames => s_fieldNamesLazy.Value;

    public static Dictionary<string, string> GetFieldNames()
    {
        return FieldNames;
    }
}