using RHToolkit.Models.Localization;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.SQLite;
using System.Data;
using System.Data.SQLite;

namespace RHToolkit.Services
{
    public class SqLiteDatabaseService : ISqLiteDatabaseService
    {
        public static string? DbFilePath { get; set; }

        public SqLiteDatabaseService()
        {
            DbFilePath = GetDatabaseFilePath();
        }

        public static string GetDatabaseFilePath()
        {
            string resourcesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
            string currentLanguage = LocalizationManager.GetCurrentLanguage();
            string databaseName = $"gmdb_{currentLanguage}.db";
            string databaseFilePath = Path.Combine(resourcesFolder, databaseName);

            if (File.Exists(databaseFilePath))
            {
                return databaseFilePath;
            }

            // Fallback to default language "en-US"
            string defaultDatabaseName = "gmdb_en-US.db";
            string defaultDatabaseFilePath = Path.Combine(resourcesFolder, defaultDatabaseName);

            if (File.Exists(defaultDatabaseFilePath))
            {
                return defaultDatabaseFilePath;
            }

            return string.Empty;
        }

        public bool ValidateDatabase()
        {
            string dbFilePath = GetDatabaseFilePath();

            if (!File.Exists(dbFilePath))
            {
                RHMessageBoxHelper.ShowOKMessage(Resources.MissingDatabaseMessage, Resources.MissingDatabaseTitle);
                return false;
            }

            if (File.Exists(dbFilePath))
            {
                List<string> missingTables = GetMissingTables();

                if (missingTables.Count > 0)
                {
                    string missingTablesMessage = $"{string.Format(Resources.MissingTableMessage, Path.GetFileName(dbFilePath))}:\n";
                    missingTablesMessage += string.Join("\n", missingTables);
                    missingTablesMessage += $"\n{Resources.MissingTableMessage2}";

                    RHMessageBoxHelper.ShowOKMessage(missingTablesMessage, Resources.Error);
                    return false;
                }

            }

            return true;
        }

        public List<string> GetMissingTables()
        {
            List<string> requiredTables = GMDatabaseManager.RequiredTables;

            List<string> missingTables = [];

            using var connection = OpenSQLiteConnection();

            // Check if each required table exists in the database
            foreach (string tableName in requiredTables)
            {
                using var command = new SQLiteCommand($"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'", connection);
                using var reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    missingTables.Add(tableName);
                }
            }

            return missingTables;
        }

        public SQLiteConnection OpenSQLiteConnection()
        {
            if (!File.Exists(DbFilePath))
            {
                throw new FileNotFoundException($"{Resources.MissingDatabaseTitle}\n{Resources.MissingDatabaseMessage}");
            }

            var connection = new SQLiteConnection($"Data Source={DbFilePath};Version=3;");
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