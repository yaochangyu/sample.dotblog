using System;
using Lab.UnitTest;

namespace Lab.EntityModel
{
    public partial class LabEmployee2DB
    {
        private static readonly string    ConnectionName = "SecretLabDbContext";
        private static          string    Symbol         = "secret:";
        private static          Validator Validator      = new Validator("yaochang");

        public static LabEmployee2DB CreateSecretDb()
        {
            var dbConnectString = GetConnectionString(ConnectionName);
            var hasProtect      = dbConnectString.IndexOf(Symbol) > -1;

            if (hasProtect)
            {
                dbConnectString = dbConnectString.Split(new[] {Symbol}, StringSplitOptions.RemoveEmptyEntries)[0];
                //解密
                dbConnectString = Validator.Decrypt(dbConnectString);
                SetConnectionString(ConnectionName, dbConnectString);
            }

            var db = new LabEmployee2DB(ConnectionName);

            db.OnConnectionOpened += Db_OnConnectionOpened;
            db.OnClosing          += Db_OnClosing;
            db.OnClosed           += Db_OnClosed;
            return db;
        }

        private static void Db_OnClosed(object sender, EventArgs e)
        {
            Console.WriteLine("Db Closed");
        }

        private static void Db_OnClosing(object sender, EventArgs e)
        {
            Console.WriteLine("Db Closing");
        }

        private static void Db_OnConnectionOpened(LinqToDB.Data.DataConnection arg1, System.Data.IDbConnection arg2)
        {
            Console.WriteLine("Db Opened");
        }
    }
}