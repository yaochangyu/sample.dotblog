using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;

namespace System.Data.SQLite.EF6.Migrations
{
    internal static class SQLiteProviderServicesHelper
    {
        /// <summary>
        ///     Creates a SQLiteParameter given a name, type, and direction
        /// </summary>
        internal static SQLiteParameter CreateSQLiteParameter(string name, TypeUsage type, ParameterMode mode,
                                                              object value)
        {
            int? size;

            if (type.GetPrimitiveTypeKind() == PrimitiveTypeKind.Guid)
            {
                type = TypeUsage.CreateStringTypeUsage(
                                                       PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String),
                                                       false, true);
            }

            var result = new SQLiteParameter(name, value);

            // .Direction
            var direction = MetadataHelpers.ParameterModeToParameterDirection(mode);
            if (result.Direction != direction)
            {
                result.Direction = direction;
            }

            // .Size and .DbType
            // output parameters are handled differently (we need to ensure there is space for return
            // values where the user has not given a specific Size/MaxLength)
            var isOutParam = mode != ParameterMode.In;
            var sqlDbType  = GetSQLiteDbType(type, isOutParam, out size);
            if (result.DbType != sqlDbType)
            {
                result.DbType = sqlDbType;
            }

            // Note that we overwrite 'facet' parameters where either the value is different or
            // there is an output parameter.
            if (size.HasValue && (isOutParam || result.Size != size.Value))
            {
                result.Size = size.Value;
            }

            // .IsNullable
            var isNullable = type.GetIsNullable();
            if (isOutParam || isNullable != result.IsNullable)
            {
                result.IsNullable = isNullable;
            }

            return result;
        }

        /// <summary>
        ///     Chooses the appropriate DbType for the given binary type.
        /// </summary>
        private static DbType GetBinaryDbType(TypeUsage type)
        {
            Debug.Assert(type.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType &&
                         PrimitiveTypeKind.Binary     == ((PrimitiveType) type.EdmType).PrimitiveTypeKind,
                         "only valid for binary type");

            // Specific type depends on whether the binary value is fixed length. By default, assume variable length.
            bool fixedLength;
            if (!type.TryGetIsFixedLength(out fixedLength))

                // ReSharper disable once RedundantAssignment
            {
                fixedLength = false;
            }

            return DbType.Binary;

            //            return fixedLength ? DbType.Binary : DbType.VarBinary;
        }

        /// <summary>
        ///     Determines preferred value for SQLiteParameter.Size. Returns null
        ///     where there is no preference.
        /// </summary>
        private static int? GetParameterSize(TypeUsage type, bool isOutParam)
        {
            int maxLength;
            if (type.TryGetMaxLength(out maxLength))
            {
                // if the MaxLength facet has a specific value use it
                return maxLength;
            }

            if (isOutParam)
            {
                // if the parameter is a return/out/inout parameter, ensure there
                // is space for any value
                return int.MaxValue;
            }

            // no value
            return default;
        }

        /// <summary>
        ///     Determines DbType for the given primitive type. Extracts facet
        ///     information as well.
        /// </summary>
        private static DbType GetSQLiteDbType(TypeUsage type, bool isOutParam, out int? size)
        {
            // only supported for primitive type
            var primitiveTypeKind = type.GetPrimitiveTypeKind();

            size = default;

            switch (primitiveTypeKind)
            {
                case PrimitiveTypeKind.Binary:
                    // for output parameters, ensure there is space...
                    size = GetParameterSize(type, isOutParam);
                    return GetBinaryDbType(type);

                case PrimitiveTypeKind.Boolean:
                    return DbType.Boolean;

                case PrimitiveTypeKind.Byte:
                    return DbType.Byte;

                case PrimitiveTypeKind.Time:
                    return DbType.Time;

                case PrimitiveTypeKind.DateTimeOffset:
                    return DbType.DateTimeOffset;

                case PrimitiveTypeKind.DateTime:
                    return DbType.DateTime;

                case PrimitiveTypeKind.Decimal:
                    return DbType.Decimal;

                case PrimitiveTypeKind.Double:
                    return DbType.Double;

                case PrimitiveTypeKind.Guid:
                    return DbType.Guid;

                case PrimitiveTypeKind.Int16:
                    return DbType.Int16;

                case PrimitiveTypeKind.Int32:
                    return DbType.Int32;

                case PrimitiveTypeKind.Int64:
                    return DbType.Int64;

                case PrimitiveTypeKind.SByte:
                    return DbType.SByte;

                case PrimitiveTypeKind.Single:
                    return DbType.Single;

                case PrimitiveTypeKind.String:
                    size = GetParameterSize(type, isOutParam);
                    return GetStringDbType(type);

                default:
                    throw new InvalidOperationException("unknown PrimitiveTypeKind " + primitiveTypeKind);
            }
        }

        /// <summary>
        ///     Chooses the appropriate DbType for the given string type.
        /// </summary>
        private static DbType GetStringDbType(TypeUsage type)
        {
            Debug.Assert(type.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType &&
                         PrimitiveTypeKind.String     == ((PrimitiveType) type.EdmType).PrimitiveTypeKind,
                         "only valid for string type");

            DbType dbType;

            // Specific type depends on whether the string is a unicode string and whether it is a fixed length string.
            // By default, assume widest type (unicode) and most common type (variable length)
            bool unicode;
            bool fixedLength;
            if (!type.TryGetIsFixedLength(out fixedLength))
            {
                fixedLength = false;
            }

            unicode = type.GetIsUnicode();

            if (fixedLength)
            {
                dbType = unicode ? DbType.StringFixedLength : DbType.AnsiStringFixedLength;
            }
            else
            {
                dbType = unicode ? DbType.String : DbType.AnsiString;
            }

            return dbType;
        }
    }
}