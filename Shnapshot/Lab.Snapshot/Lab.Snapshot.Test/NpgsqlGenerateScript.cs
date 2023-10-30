namespace Lab.Snapshot.Test;

internal class NpgsqlGenerateScript
{
    public static string ClearAllRecord()
    {
        return @"
DO $$
DECLARE row RECORD;
BEGIN
  FOR row IN SELECT table_name
    FROM information_schema.tables
    WHERE table_type='BASE TABLE'
    AND table_schema='public'
    AND table_name NOT IN ('admins', 'admin_roles', '__EFMigrationsHistory') 
  LOOP 
    EXECUTE format('TRUNCATE TABLE %I CONTINUE IDENTITY CASCADE;', row.table_name);
  END LOOP;
END;
$$;
";
    }

    public static string ReseedMemberCollectionSeq()
    {
        return "ALTER SEQUENCE member_collection_seqno RESTART WITH 1;";
    }
}