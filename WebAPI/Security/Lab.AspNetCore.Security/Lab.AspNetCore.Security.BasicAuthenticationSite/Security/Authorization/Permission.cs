namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authorization;

public class Permission
{
    public class Operation
    {
        public const string Write = $"{nameof(Operation)}.{nameof(Write)}";
        public const string Read = $"{nameof(Operation)}.{nameof(Read)}";

        private static readonly Lazy<Dictionary<string, Type>> s_values
            = new(() =>
            {
                return FieldTypeAssistant.GetStaticFieldName<Operation>()
                    .ToDictionary(p => p.Key,
                        p => p.Value,
                        StringComparer.InvariantCultureIgnoreCase);
            });

        public static Dictionary<string, Type> GetValues()
            => s_values.Value;
    }
}