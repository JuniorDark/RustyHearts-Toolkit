using RHToolkit.Models.Localization;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.SQLite;
using System.Data;
using System.Data.SQLite;

namespace RHToolkit.Services
{
    /// <summary>
    /// Provides methods to interact with a SQLite database.
    /// </summary>
    public class SqLiteDatabaseService : ISqLiteDatabaseService
    {
        public static string? DbFilePath { get; set; }

        public SqLiteDatabaseService()
        {
            DbFilePath = GetDatabaseFilePath();
        }

        /// <summary>
        /// Gets the file path of the SQLite database based on the current language.
        /// </summary>
        /// <returns>The file path of the SQLite database.</returns>
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

        /// <summary>
        /// Validates the existence and structure of the SQLite database.
        /// </summary>
        /// <returns>True if the database is valid; otherwise, false.</returns>
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

        /// <summary>
        /// Gets a list of missing tables in the SQLite database.
        /// </summary>
        /// <returns>A list of missing table names.</returns>
        public List<string> GetMissingTables()
        {
            List<string> requiredTables = GMDatabaseManager.RequiredTables;

            List<string> missingTables = new();

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

        /// <summary>
        /// Opens a connection to the SQLite database.
        /// </summary>
        /// <returns>The opened SQLiteConnection.</returns>
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

        /// <summary>
        /// Executes a query and returns a SQLiteDataReader to read the results.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="connection">The SQLiteConnection to use.</param>
        /// <param name="parameters">The parameters for the query.</param>
        /// <returns>A SQLiteDataReader to read the results of the query.</returns>
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