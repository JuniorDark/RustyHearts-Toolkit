using System.Data;
using System.Data.SQLite;
using System.IO;

namespace RHToolkit.Services
{
    public class SqLiteDatabaseService : ISqLiteDatabaseService
    {
        private readonly string _dbFilePath;

        public SqLiteDatabaseService()
        {
            _dbFilePath = GetDatabaseFilePath();
        }

        public static string GetDatabaseFilePath()
        {
            string resourcesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
            return Path.Combine(resourcesFolder, "gmdb.db");
        }

        public SQLiteConnection OpenSQLiteConnection()
        {
            if (!File.Exists(_dbFilePath))
            {
                throw new FileNotFoundException($"Database file not found...");
            }

            var connection = new SQLiteConnection($"Data Source={_dbFilePath};Version=3;");
            connection.Open();
            return connection;
        }

        public SQLiteDataReader ExecuteReader(string query, SQLiteConnection connection, params (string, object)[] parameters)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var command = new SQLiteCommand(query, connection);
            AddParametersToCommand(command, parameters);

            return command.ExecuteReader();
        }

        private static void AddParametersToCommand(SQLiteCommand command, (string, object)[] parameters)
        {
            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value);
            }
        }
    }
}
