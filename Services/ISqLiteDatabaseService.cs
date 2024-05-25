using System.Data.SQLite;

namespace RHToolkit.Services
{
    public interface ISqLiteDatabaseService
    {
        SQLiteDataReader ExecuteReader(string query, SQLiteConnection connection, params (string, object)[] parameters);
        List<string> GetMissingTables();
        SQLiteConnection OpenSQLiteConnection();
        bool ValidateDatabase();
    }
}