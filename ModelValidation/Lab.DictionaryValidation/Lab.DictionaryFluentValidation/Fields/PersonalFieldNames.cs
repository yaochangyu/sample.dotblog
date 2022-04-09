using Lab.DictionaryFluentValidation.Assistants;

namespace Lab.DictionaryFluentValidation.Fields;

public class PersonalFieldNames
{
    /// <summary>
    ///     身分證字號
    /// </summary>
    public static string IdentityCardId = "identityCardId";

    /// <summary>
    ///     教育程度
    /// </summary>
    public static string Education = "education";

    /// <summary>
    ///     職業
    /// </summary>
    public static string Profession = "profession";

    /// <summary>
    ///     婚姻狀態
    /// </summary>
    public static string MaritalStatus = "maritalStatus";

    /// <summary>
    ///     家屬
    /// </summary>
    public static string Dependents = "dependents";

    /// <summary>
    ///     年收入
    /// </summary>
    public static string AnnualIncome = "annualIncome";

    private static readonly Lazy<IEnumerable<string>> s_fieldNameLazy =
        new(ProfileAssistants.GetFieldNames<PersonalFieldNames>);

    private static readonly Lazy<Dictionary<string, string>> s_fieldLazy =
        new(ProfileAssistants.GetFields<PersonalFieldNames>);

    public static IEnumerable<string> GetFieldNames()
    {
        return s_fieldNameLazy.Value;
    }

    public static Dictionary<string, string> GetFields()
    {
        return s_fieldLazy.Value;
    }
}