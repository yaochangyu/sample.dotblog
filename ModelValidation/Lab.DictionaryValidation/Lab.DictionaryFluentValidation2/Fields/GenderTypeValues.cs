using Lab.DictionaryFluentValidation2.Assistants;

namespace Lab.DictionaryFluentValidation2.Fields;

public class GenderTypeValues
{
    public const string Male = "male";
    public const string Female = "female";
    public const string NotAvailable = "notAvailable";

    private static readonly Lazy<Dictionary<string, string>> s_fieldValuesLazy =
        new(() => ProfileAssistants.GetFieldNames<GenderTypeValues>());

    private static Dictionary<string, string> FieldValues => s_fieldValuesLazy.Value;

    public static Dictionary<string, string> GetFieldValues()
    {
        return FieldValues;
    }
}