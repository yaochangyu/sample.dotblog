using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Text;

namespace System.Data.SQLite.EF6.Migrations
{
    internal class SQLiteDdlBuilder
    {
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        public void AppendIdentifier(string identifier)
        {
            string correctIdentifier;

            correctIdentifier = SQLiteProviderManifestHelper.RemoveDbo(identifier);

            if (correctIdentifier.Length > SQLiteProviderManifestHelper.MaxObjectNameLength)
            {
                var guid = Guid.NewGuid().ToString().Replace("-", "");
                correctIdentifier =
                    correctIdentifier.Substring(0, SQLiteProviderManifestHelper.MaxObjectNameLength - guid.Length) +
                    guid;
            }

            this.AppendSql(SQLiteProviderManifestHelper.QuoteIdentifier(correctIdentifier));
        }

        public void AppendIdentifierList(IEnumerable<string> identifiers)
        {
            var first = true;
            foreach (var identifier in identifiers)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    this.AppendSql(", ");
                }

                this.AppendIdentifier(identifier);
            }
        }

        /// <summary>
        ///     Appends new line for visual formatting or for ending a comment.
        /// </summary>
        public void AppendNewLine()
        {
            this._stringBuilder.Append("\r\n");
        }

        /// <summary>
        ///     Appends raw SQL into the string builder.
        /// </summary>
        /// <param name="text">Raw SQL string to append into the string builder.</param>
        public void AppendSql(string text)
        {
            this._stringBuilder.Append(text);
        }

        /// <summary>
        ///     Appends raw SQL into the string builder.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="p">The p.</param>
        public void AppendSql(string format, params object[] p)
        {
            this._stringBuilder.AppendFormat(format, p);
        }

        public void AppendStringLiteral(string literalValue)
        {
            this.AppendSql("'" + literalValue.Replace("'", "''") + "'");
        }

        public void AppendType(EdmProperty column)
        {
            this.AppendType(column.TypeUsage, column.Nullable, column.TypeUsage.GetIsIdentity());
        }

        public void AppendType(TypeUsage typeUsage, bool isNullable, bool isIdentity)
        {
            var isTimestamp = false;

            var sqliteTypeName = typeUsage.EdmType.Name;
            var sqliteLength   = "";

            switch (sqliteTypeName)
            {
                case "decimal":
                case "numeric":
                    sqliteLength = string.Format(System.Globalization.CultureInfo.InvariantCulture, "({0}, {1})",
                                                 typeUsage.GetPrecision(), typeUsage.GetScale());
                    break;
                case "binary":
                case "varbinary":
                case "varchar":
                case "nvarchar":
                case "char":
                case "nchar":
                    sqliteLength = string.Format("({0})", typeUsage.GetMaxLength());
                    break;
            }

            this.AppendSql(sqliteTypeName);
            this.AppendSql(sqliteLength);
            this.AppendSql(isNullable ? " null" : " not null");

            if (isTimestamp)
            {
                ; // nothing to generate for identity
            }
            else if (isIdentity && sqliteTypeName == "guid")
            {
                this.AppendSql(" default GenGUID()");
            }
            else if (isIdentity)
            {
                this.AppendSql(" constraint primary key autoincrement");
            }
        }

        public string CreateConstraintName(string constraint, string objectName)
        {
            objectName = SQLiteProviderManifestHelper.RemoveDbo(objectName);

            var name = string.Format("{0}_{1}", constraint, objectName);

            if (name.Length + 9 > SQLiteProviderManifestHelper.MaxObjectNameLength)
            {
                name = name.Substring(0, SQLiteProviderManifestHelper.MaxObjectNameLength - 9);
            }

            name += "_" + this.GetRandomString();

            return name;
        }

        public string GetCommandText()
        {
            return this._stringBuilder.ToString();
        }

        // Returns an eigth nibbles string
        protected string GetRandomString()
        {
            var random      = new Random();
            var randomValue = "";
            for (var n = 0; n < 8; n++)
            {
                var b = (byte) random.Next(15);
                randomValue += string.Format("{0:x1}", b);
            }

            return randomValue;
        }
    }
}