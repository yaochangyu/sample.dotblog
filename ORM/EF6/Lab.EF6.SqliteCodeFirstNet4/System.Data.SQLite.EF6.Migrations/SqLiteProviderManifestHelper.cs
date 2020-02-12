namespace System.Data.SQLite.EF6.Migrations
{
    internal static class SQLiteProviderManifestHelper
    {
        public const int MaxObjectNameLength = 255;

        /// <summary>
        ///     Gets the full name of the identifier.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="identifier">Name of the identifier.</param>
        /// <returns>The full name of the identifier</returns>
        internal static string GetFullIdentifierName(string tableName, string identifier)
        {
            return RemoveDbo(tableName) + "_" + identifier;
        }

        /// <summary>
        ///     Gets the full name of the identifier.
        /// </summary>
        /// <param name="objectType">Type prefix to assign basing on the object type. For example PK or FK</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="identifier">The identifier.</param>
        /// <returns>The full name of the identifier</returns>
        internal static string GetFullIdentifierName(string objectType, string tableName, string identifier)
        {
            return objectType + "_" + RemoveDbo(tableName) + "_" + identifier;
        }

        /// <summary>
        ///     Quotes an identifier
        /// </summary>
        /// <param name="name">Identifier name</param>
        /// <returns>The quoted identifier</returns>
        internal static string QuoteIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            return "\"" + name.Replace("\"", "\"\"") + "\"";
        }

        internal static string RemoveDbo(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new ArgumentNullException("identifier");
            }

            return identifier.ToLower().StartsWith("dbo.") ? identifier.Substring(4) : identifier;
        }
    }
}