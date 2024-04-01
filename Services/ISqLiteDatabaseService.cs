using System.Data.SQLite;

namespace RHGMTool.Services
{
    public interface ISqLiteDatabaseService
    {
        SQLiteConnection OpenSQLiteConnection();
        SQLiteDataReader ExecuteReader(string query, SQLiteConnection connection, params (string, object)[] parameters);
    }

}
