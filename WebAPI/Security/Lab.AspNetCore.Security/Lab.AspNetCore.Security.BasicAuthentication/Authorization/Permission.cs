namespace Lab.AspNetCore.Security.BasicAuthentication;

public class Permission
{
    public class Operation
    {
        public const string Write = $"{nameof(Permission)}.{nameof(Operation)}:{nameof(Write)}";
        public const string Read = $"{nameof(Permission)}.{nameof(Operation)}:{nameof(Read)}";

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