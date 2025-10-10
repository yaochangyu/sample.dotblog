using Microsoft.Data.Sqlite;

namespace Lab.CleanDuplicatesImage;

/// <summary>
/// 資料庫輔助類別，提供統一的資料庫連線和基本操作
/// </summary>
static class DatabaseHelper
{
    private const string DatabaseFileName = "duplicates.db";

    /// <summary>
    /// 建立資料庫連線
    /// </summary>
    public static SqliteConnection CreateConnection()
    {
        return new SqliteConnection($"Data Source={DatabaseFileName}");
    }

    /// <summary>
    /// 執行非查詢命令（INSERT、UPDATE、DELETE）
    /// </summary>
    public static int ExecuteNonQuery(string commandText, Action<SqliteCommand>? configureParameters = null)
    {
        using var connection = CreateConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = commandText;
        configureParameters?.Invoke(command);

        return command.ExecuteNonQuery();
    }

    /// <summary>
    /// 執行查詢並處理結果
    /// </summary>
    public static T ExecuteQuery<T>(string commandText, Func<SqliteDataReader, T> processReader, Action<SqliteCommand>? configureParameters = null)
    {
        using var connection = CreateConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = commandText;
        configureParameters?.Invoke(command);

        using var reader = command.ExecuteReader();
        return processReader(reader);
    }

    /// <summary>
    /// 執行帶交易的批次操作
    /// </summary>
    public static void ExecuteTransaction(Action<SqliteConnection, SqliteTransaction> action)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            action(connection, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}