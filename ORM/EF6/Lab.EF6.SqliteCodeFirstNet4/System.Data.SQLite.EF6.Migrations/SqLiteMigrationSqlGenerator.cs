using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.Migrations.Sql;
using System.Diagnostics;

namespace System.Data.SQLite.EF6.Migrations
{
    /// <summary>
    ///     Migration Ddl generator for SQLite
    /// </summary>
    public class SQLiteMigrationSqlGenerator : MigrationSqlGenerator
    {
        private const string BATCHTERMINATOR = ";\r\n";

        /// <summary>
        ///     Initializes a new instance of the <see cref="SQLiteMigrationSqlGenerator" /> class.
        /// </summary>
        public SQLiteMigrationSqlGenerator()
        {
            this.ProviderManifest =
                ((DbProviderServices) new SQLiteProviderFactory().GetService(typeof(DbProviderServices)))
                .GetProviderManifest("");
        }

        /// <summary>
        ///     Converts a set of migration operations into database provider specific SQL.
        /// </summary>
        /// <param name="migrationOperations">The operations to be converted.</param>
        /// <param name="providerManifestToken">Token representing the version of the database being targeted.</param>
        /// <returns>
        ///     A list of SQL statements to be executed to perform the migration operations.
        /// </returns>
        public override IEnumerable<MigrationStatement> Generate(IEnumerable<MigrationOperation> migrationOperations,
                                                                 string                          providerManifestToken)
        {
            var migrationStatements = new List<MigrationStatement>();

            foreach (var migrationOperation in migrationOperations)
            {
                migrationStatements.Add(this.GenerateStatement(migrationOperation));
            }

            return migrationStatements;
        }

        private string GenerateSqlStatement(MigrationOperation migrationOperation)
        {
            dynamic concreteMigrationOperation = migrationOperation;
            return GenerateSqlStatementConcrete(concreteMigrationOperation);
        }

        private string GenerateSqlStatementConcrete(MigrationOperation migrationOperation)
        {
            Debug.Assert(false);
            return string.Empty;
        }

        #region History operations

        private string GenerateSqlStatementConcrete(HistoryOperation migrationOperation)
        {
            var ddlBuilder = new SQLiteDdlBuilder();

            foreach (var commandTree in migrationOperation.CommandTrees)
            {
                List<DbParameter> parameters;

                // Take care because here we have several queries so we can't use parameters...
                switch (commandTree.CommandTreeKind)
                {
                    case DbCommandTreeKind.Insert:
                        ddlBuilder.AppendSql(SQLiteDmlBuilder.GenerateInsertSql((DbInsertCommandTree) commandTree,
                                                                                out parameters, true));
                        break;
                    case DbCommandTreeKind.Delete:
                        ddlBuilder.AppendSql(SQLiteDmlBuilder.GenerateDeleteSql((DbDeleteCommandTree) commandTree,
                                                                                out parameters, true));
                        break;
                    case DbCommandTreeKind.Update:
                        ddlBuilder.AppendSql(SQLiteDmlBuilder.GenerateUpdateSql((DbUpdateCommandTree) commandTree,
                                                                                out parameters, true));
                        break;
                    case DbCommandTreeKind.Function:
                    case DbCommandTreeKind.Query:
                    default:
                        throw new
                            InvalidOperationException(string
                                                          .Format("Command tree of type {0} not supported in migration of history operations",
                                                                  commandTree.CommandTreeKind));
                }

                ddlBuilder.AppendSql(BATCHTERMINATOR);
            }

            return ddlBuilder.GetCommandText();
        }

        #endregion

        #region Foreign keys creation

        private string GenerateSqlStatementConcrete(AddForeignKeyOperation migrationOperation)
        {
            /* 
             * SQLite supports foreign key creation only during table creation
             * At least, during table creation we could try to create relationships but it
             * Requires that we sort tables in dependency order (and that there is not a circular reference
             *
             * Actually we do not create relationship at all
            */

            return "";
        }

        #endregion

        #region Primary keys creation

        private string GenerateSqlStatementConcrete(AddPrimaryKeyOperation migrationOperation)
        {
            // Actually primary key creation is supported only during table creation

            var ddlBuilder = new SQLiteDdlBuilder();
            ddlBuilder.AppendSql(" PRIMARY KEY (");
            ddlBuilder.AppendIdentifierList(migrationOperation.Columns);
            ddlBuilder.AppendSql(")");
            return ddlBuilder.GetCommandText();
        }

        #endregion

        #region Index

        private string GenerateSqlStatementConcrete(CreateIndexOperation migrationOperation)
        {
            var ddlBuilder = new SQLiteDdlBuilder();
            ddlBuilder.AppendSql("CREATE ");
            if (migrationOperation.IsUnique)
            {
                ddlBuilder.AppendSql("UNIQUE ");
            }

            ddlBuilder.AppendSql("INDEX ");
            ddlBuilder.AppendIdentifier(SQLiteProviderManifestHelper.GetFullIdentifierName(migrationOperation.Table,
                                                                                           migrationOperation.Name));
            ddlBuilder.AppendSql(" ON ");
            ddlBuilder.AppendIdentifier(migrationOperation.Table);
            ddlBuilder.AppendSql(" (");
            ddlBuilder.AppendIdentifierList(migrationOperation.Columns);
            ddlBuilder.AppendSql(")");

            return ddlBuilder.GetCommandText();
        }

        #endregion

        private MigrationStatement GenerateStatement(MigrationOperation migrationOperation)
        {
            var migrationStatement = new MigrationStatement();
            migrationStatement.BatchTerminator = BATCHTERMINATOR;
            migrationStatement.Sql             = this.GenerateSqlStatement(migrationOperation);
            return migrationStatement;
        }

        #region Move operations (not supported by Jet)

        private string GenerateSqlStatementConcrete(MoveProcedureOperation migrationOperation)
        {
            throw new NotSupportedException("Move operations not supported by SQLite");
        }

        private string GenerateSqlStatementConcrete(MoveTableOperation migrationOperation)
        {
            throw new NotSupportedException("Move operations not supported by SQLite");
        }

        #endregion

        #region Procedure related operations (not supported by Jet)

        private string GenerateSqlStatementConcrete(AlterProcedureOperation migrationOperation)
        {
            throw new NotSupportedException("Procedures are not supported by SQLite");
        }

        private string GenerateSqlStatementConcrete(CreateProcedureOperation migrationOperation)
        {
            throw new NotSupportedException("Procedures are not supported by SQLite");
        }

        private string GenerateSqlStatementConcrete(DropProcedureOperation migrationOperation)
        {
            throw new NotSupportedException("Procedures are not supported by SQLite");
        }

        private string GenerateSqlStatementConcrete(RenameProcedureOperation migrationOperation)
        {
            throw new NotSupportedException("Procedures are not supported by SQLite");
        }

        #endregion

        #region Rename operations

        private string GenerateSqlStatementConcrete(RenameColumnOperation migrationOperation)
        {
            throw new NotSupportedException("Cannot rename objects with Jet");
        }

        private string GenerateSqlStatementConcrete(RenameIndexOperation migrationOperation)
        {
            throw new NotSupportedException("Cannot rename objects with Jet");
        }

        private string GenerateSqlStatementConcrete(RenameTableOperation migrationOperation)
        {
            var ddlBuilder = new SQLiteDdlBuilder();

            ddlBuilder.AppendSql("ALTER TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name);
            ddlBuilder.AppendSql(" RENAME TO ");
            ddlBuilder.AppendIdentifier(migrationOperation.NewName);

            return ddlBuilder.GetCommandText();
        }

        #endregion

        #region Columns

        private string GenerateSqlStatementConcrete(AddColumnOperation migrationOperation)
        {
            var ddlBuilder = new SQLiteDdlBuilder();

            ddlBuilder.AppendSql("ALTER TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.Table);
            ddlBuilder.AppendSql(" ADD COLUMN ");

            var column = migrationOperation.Column;

            ddlBuilder.AppendIdentifier(column.Name);
            ddlBuilder.AppendSql(" ");
            var storeType = this.ProviderManifest.GetStoreType(column.TypeUsage);
            ddlBuilder.AppendType(storeType, column.IsNullable ?? true, column.IsIdentity);
            ddlBuilder.AppendNewLine();

            return ddlBuilder.GetCommandText();
        }

        private string GenerateSqlStatementConcrete(DropColumnOperation migrationOperation)
        {
            throw new NotSupportedException("Drop column not supported by SQLite");
        }

        private string GenerateSqlStatementConcrete(AlterColumnOperation migrationOperation)
        {
            throw new NotSupportedException("Alter column not supported by SQLite");
        }

        #endregion

        #region Table operations

        private string GenerateSqlStatementConcrete(AlterTableOperation migrationOperation)
        {
            /* 
             * SQLite does not support alter table
             * We should rename old table, create the new table, copy old data to new table and drop old table
            */

            throw new NotSupportedException("Alter column not supported by SQLite");
        }

        private string GenerateSqlStatementConcrete(CreateTableOperation migrationOperation)
        {
            var ddlBuilder = new SQLiteDdlBuilder();

            ddlBuilder.AppendSql("CREATE TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name);
            ddlBuilder.AppendSql(" (");
            ddlBuilder.AppendNewLine();

            var    first                   = true;
            string autoincrementColumnName = null;
            foreach (var column in migrationOperation.Columns)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    ddlBuilder.AppendSql(",");
                }

                ddlBuilder.AppendSql(" ");
                ddlBuilder.AppendIdentifier(column.Name);
                ddlBuilder.AppendSql(" ");
                if (column.IsIdentity)
                {
                    autoincrementColumnName = column.Name;
                    ddlBuilder.AppendSql(" integer constraint ");
                    ddlBuilder.AppendIdentifier(ddlBuilder.CreateConstraintName("PK", migrationOperation.Name));
                    ddlBuilder.AppendSql(" primary key autoincrement");
                    ddlBuilder.AppendNewLine();
                }
                else
                {
                    var storeTypeUsage = this.ProviderManifest.GetStoreType(column.TypeUsage);
                    ddlBuilder.AppendType(storeTypeUsage, column.IsNullable ?? true, column.IsIdentity);
                    ddlBuilder.AppendNewLine();
                }
            }

            if (migrationOperation.PrimaryKey != null && autoincrementColumnName == null)
            {
                ddlBuilder.AppendSql(",");
                ddlBuilder.AppendSql(this.GenerateSqlStatementConcrete(migrationOperation.PrimaryKey));
            }

            ddlBuilder.AppendSql(")");

            return ddlBuilder.GetCommandText();
        }

        #endregion

        #region Drop

        private string GenerateSqlStatementConcrete(DropForeignKeyOperation migrationOperation)
        {
            var ddlBuilder = new SQLiteDdlBuilder();
            ddlBuilder.AppendSql("ALTER TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.PrincipalTable);
            ddlBuilder.AppendSql(" DROP CONSTRAINT ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name);
            return ddlBuilder.GetCommandText();
        }

        private string GenerateSqlStatementConcrete(DropPrimaryKeyOperation migrationOperation)
        {
            var ddlBuilder = new SQLiteDdlBuilder();
            ddlBuilder.AppendSql("ALTER TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.Table);
            ddlBuilder.AppendSql(" DROP CONSTRAINT ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name);
            return ddlBuilder.GetCommandText();
        }

        private string GenerateSqlStatementConcrete(DropIndexOperation migrationOperation)
        {
            var ddlBuilder = new SQLiteDdlBuilder();
            ddlBuilder.AppendSql("DROP INDEX ");
            ddlBuilder.AppendIdentifier(SQLiteProviderManifestHelper.GetFullIdentifierName(migrationOperation.Table,
                                                                                           migrationOperation.Name));
            ddlBuilder.AppendSql(" ON ");
            ddlBuilder.AppendIdentifier(migrationOperation.Table);
            return ddlBuilder.GetCommandText();
        }

        private string GenerateSqlStatementConcrete(DropTableOperation migrationOperation)
        {
            var ddlBuilder = new SQLiteDdlBuilder();
            ddlBuilder.AppendSql("DROP TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name);
            return ddlBuilder.GetCommandText();
        }

        #endregion
    }
}