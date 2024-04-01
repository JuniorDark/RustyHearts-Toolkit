using System.Data;
using System.Data.SQLite;
using System.IO;

namespace RHGMTool.Services
{
    public class SqLiteDatabaseService : ISqLiteDatabaseService
    {
        public static string GetDatabaseFilePath()
        {
            string resourcesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
            return Path.Combine(resourcesFolder, "gmdb.db");
        }

        public SQLiteConnection OpenSQLiteConnection()
        {
            string dbFilePath = GetDatabaseFilePath();

            if (!File.Exists(dbFilePath))
            {
                throw new FileNotFoundException($"Database file ({Path.GetFileName(dbFilePath)}) not found in the expected location. Please ensure to create the gmdb and place it in the Resources folder.");
            }

            var connection = new SQLiteConnection($"Data Source={dbFilePath};Version=3;");
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
