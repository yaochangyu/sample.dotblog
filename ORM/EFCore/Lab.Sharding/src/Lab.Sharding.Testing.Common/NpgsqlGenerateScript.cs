namespace Lab.Sharding.Testing.Common;

public class NpgsqlGenerateScript
{
    public static string ClearAllRecord()
    {
        return @"
DO $$
DECLARE row RECORD;
DECLARE seq TEXT;
BEGIN
  FOR row IN SELECT table_name
    FROM information_schema.tables
    WHERE table_type='BASE TABLE'
    AND table_schema='public'
    AND table_name NOT IN ('admins', 'admin_roles', '__EFMigrationsHistory') 
  LOOP 
    EXECUTE format('TRUNCATE TABLE %I CONTINUE IDENTITY CASCADE;', row.table_name);
  END LOOP;
  FOR row IN SELECT table_name
    FROM information_schema.tables
    WHERE table_type='BASE TABLE'
    AND table_schema='histories'
    AND table_name NOT IN ('admins', 'admin_roles', '__EFMigrationsHistory') 
  LOOP 
    EXECUTE format('TRUNCATE TABLE histories.%I CONTINUE IDENTITY CASCADE;', row.table_name);
  END LOOP;
  FOR seq IN (select sequencename FROM pg_sequences) LOOP
         EXECUTE 'ALTER SEQUENCE IF EXISTS '||seq||' RESTART WITH 1;';
  END LOOP;
END;
$$;
";
    }

    public static string CreateChannelIdSeq()
    {
        return "Create SEQUENCE channel_channel_id_seq RESTART WITH 1;";
    }

    public static string ReseedChannelIdSeq()
    {
        return "ALTER SEQUENCE channel_channel_id_seq RESTART WITH 1;";
    }

    // public static void OnlySupportLocal(string connectionString)
    // {
    //     var builder = new NpgsqlConnectionStringBuilder(connectionString);
    //     if (string.Compare(builder.Host, "LOCALHOST", StringComparison.InvariantCultureIgnoreCase) != 0
    //         && string.Compare(builder.Host, "127.0.0.1", StringComparison.InvariantCultureIgnoreCase) != 0
    //         && string.Compare(builder.Host, "172.17.0.1", StringComparison.InvariantCultureIgnoreCase) !=
    //         0 // docker 建立容器時的預設位置
    //        )
    //     {
    //         throw new NotSupportedException($"伺服器只支援 localhost，目前連線字串為 {connectionString}");
    //     }
    // }
}