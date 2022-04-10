using Lab.DictionaryFluentValidation.Assistants;

namespace Lab.DictionaryFluentValidation.Fields;

public class GenderFieldValues
{
    public const string Male = "male";
    public const string Female = "female";
    public const string NotAvailable = "notAvailable";

    private static readonly Lazy<Dictionary<string, string>> s_fieldValuesLazy =
        new(() => ProfileAssistants.GetFieldNames<GenderFieldValues>());

    private static Dictionary<string, string> FieldValues => s_fieldValuesLazy.Value;

    public static Dictionary<string, string> GetFieldValues()
    {
        return FieldValues;
    }
}