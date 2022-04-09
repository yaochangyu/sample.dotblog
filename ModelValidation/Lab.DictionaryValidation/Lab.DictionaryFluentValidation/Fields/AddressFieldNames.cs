using Lab.DictionaryFluentValidation.Assistants;

namespace Lab.DictionaryFluentValidation.Fields;

public class AddressFieldNames
{
    public static readonly string Address1 = "address1";
    public static readonly string Address2 = "address2";
    public static readonly string District = "district";
    public static readonly string CityTown = "cityTown";
    public static readonly string Province = "province";
    public static readonly string PostalCode = "postalCode";
    public static readonly string Country = "country";

    private static readonly Lazy<IEnumerable<string>> s_fieldNameLazy =
        new(ProfileAssistants.GetFieldNames<AddressFieldNames>);

    private static readonly Lazy<Dictionary<string, string>> s_fieldLazy =
        new(ProfileAssistants.GetFields<AddressFieldNames>);

    public static IEnumerable<string> GetFieldNames()
    {
        return s_fieldNameLazy.Value;
    }

    public static Dictionary<string, string> GetFields()
    {
        return s_fieldLazy.Value;
    }
}