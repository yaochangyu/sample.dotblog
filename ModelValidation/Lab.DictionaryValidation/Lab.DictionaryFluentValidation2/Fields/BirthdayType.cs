using Lab.DictionaryFluentValidation2.Assistants;

namespace Lab.DictionaryFluentValidation2.Fields;

public record BirthdayType
{
    public int Year { get; init; }

    public int Month { get; init; }

    public int Day { get; init; }

    private static readonly Lazy<Dictionary<string, string>> s_fieldNamesLazy =
        new(() => ProfileAssistants.GetFieldNames<BirthdayType>());

    private static Dictionary<string, string> FieldNames => s_fieldNamesLazy.Value;

    public static Dictionary<string, string> GetFieldNames()
    {
        return FieldNames;
    }
}