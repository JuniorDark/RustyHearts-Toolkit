using RHToolkit.Models.Editor;
using RHToolkit.Models.Localization;
using RHToolkit.Models.MessageBox;
using System.Data;
using System.Data.SQLite;

namespace RHToolkit.Models.SQLite;

public class GMDatabaseManager
{
    private readonly FileManager _fileManager = new();

    public static readonly List<string> RequiredTables =
    [
        "addeffect_string",
        "angela_avatar01_parts",
        "angelaparts",
        "angelaskill",
        "angelaskilltree",
        "angelaavatar01skilltree",
        "angelaskill_string",
        "angelaweapon",
        "auctioncategory",
        "charactertitle",
        "charactertitle_string",
        "costumemix",
        "costumepack",
        "enchantinfo",
        "enemy_string",
        "exp",
        "fortune",
        "frantz_avatar01_parts",
        "frantz_avatar02_parts",
        "frantzparts",
        "frantzskill",
        "frantzskilltree",
        "frantzavatar01skilltree",
        "frantzavatar02skilltree",
        "frantzskill_string",
        "frantzweapon",
        "itemcategory",
        "itemfieldmesh",
        "itemlist",
        "itemlist_armor",
        "itemlist_armor_string",
        "itemlist_costume",
        "itemlist_costume_string",
        "itemlist_string",
        "itemlist_weapon",
        "itemlist_weapon_string",
        "itemmix",
        "itemoptionlist",
        "missionstring",
        "natasha_avatar01_parts",
        "natashaparts",
        "natashaskill",
        "natashaskilltree",
        "natashaavatar01skilltree",
        "natashaskill_string",
        "natashaweapon",
        "new_itemoptioncondition_string",
        "nick_filter",
        "npc",
        "npc_dialog",
        "npcinstance_string",
        "npcshop",
        "peteatitem",
        "petrebirth",
        "questgroup",
        "queststring",
        "rarecardrewarditemlist",
        "rarecarddropgrouplist",
        "questitemdropgrouplist",
        "championitemdropgrouplist",
        "instanceitemdropgrouplist",
        "itemdropgrouplist",
        "itemdropgrouplist_f",
        "worldinstanceitemdropgrouplist",
        "worlditemdropgrouplist",
        "worlditemdropgrouplist_fatigue",
        "eventworlditemdropgrouplist",
        "riddleboxdropgrouplist",
        "serverlobbyid",
        "setitem",
        "setitem_string",
        "startpoint_renewal",
        "string",
        "tradeitemgroup",
        "tradeshop",
        "tude_avatar01_parts",
        "tudeparts",
        "tudeskill",
        "tudeskilltree",
        "tudeavatar01skilltree",
        "tudeskill_string",
        "tudeweapon",
        "unionpackage_string",
        "world_string",
    ];

    public async Task CreateGMDatabase(string dataFolder, Action<string> reportProgress, CancellationToken cancellationToken)
    {
        string resourcesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
        string currentLanguage = LocalizationManager.GetCurrentLanguage();
        string databaseName = $"gmdb_{currentLanguage}.db";
        string database = Path.Combine(resourcesFolder, databaseName);

        try
        {
            List<string> files = Directory.GetFiles(dataFolder, "*.rh", SearchOption.AllDirectories)
                                          .Where(f => RequiredTables.Contains(Path.GetFileNameWithoutExtension(f).ToLower()))
                                          .ToList();

            if (files.Count == 0)
            {
                RHMessageBoxHelper.ShowOKMessage("The selected folder does not contain required .rh files.", "No Files");
                return;
            }

            List<string> missingFiles = [];
            foreach (string requiredTable in RequiredTables)
            {
                if (!files.Any(f => Path.GetFileNameWithoutExtension(f).Equals(requiredTable, StringComparison.OrdinalIgnoreCase)))
                {
                    missingFiles.Add(requiredTable);
                }
            }

            if (missingFiles.Count != 0)
            {
                string missingFilesMessage = "Required table files are missing. The following required files are missing:\n" + string.Join("\n", missingFiles.Select(f => $"{f}.rh"));
                RHMessageBoxHelper.ShowOKMessage(missingFilesMessage, "Missing Files");
                return;
            }

            if (!Directory.Exists(resourcesFolder))
            {
                Directory.CreateDirectory(resourcesFolder);
            }

            reportProgress("Creating database...");

            if (File.Exists(database))
            {
                File.Delete(database);
                reportProgress("Existing database file deleted.");
            }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string tableName = Path.GetFileNameWithoutExtension(file);
                reportProgress($"Processing {tableName}...");

                DataTable? dataTable = await _fileManager.RHFileToDataTableAsync(file);

                if (dataTable != null)
                {
                    DataTableToSQLite(dataTable, database, tableName, reportProgress);
                }
            }

            reportProgress("Operation complete.");
        }
        catch (OperationCanceledException)
        {
            reportProgress("Operation canceled.");

            if (File.Exists(database))
            {
                File.Delete(database);
            }
            throw;
        }
        catch (Exception ex)
        {
            reportProgress("Error: " + ex.Message);
            throw new Exception("Error: " + ex.Message, ex);
        }
    }

    private static void DataTableToSQLite(DataTable dataTable, string dbName, string tableName, Action<string> reportProgress)
    {
        string connectionString = $"Data Source={dbName};Version=3;";
        using SQLiteConnection connection = new(connectionString);
        connection.Open();

        CreateSQLiteTable(connection, dataTable, tableName);
        InsertDataIntoSQLiteTable(connection, dataTable, tableName, reportProgress);
    }

    private static void CreateSQLiteTable(SQLiteConnection connection, DataTable dataTable, string tableName)
    {
        StringBuilder createTableQuery = new($"CREATE TABLE IF NOT EXISTS \"{tableName}\" (");

        foreach (DataColumn column in dataTable.Columns)
        {
            string sqliteDataType = MapDataColumnTypeToSqlite(column.DataType);
            createTableQuery.Append($"\"{column.ColumnName}\" {sqliteDataType}, ");
        }

        createTableQuery.Remove(createTableQuery.Length - 2, 2); // Remove the last comma and space
        createTableQuery.Append(')');

        using SQLiteCommand command = new(createTableQuery.ToString(), connection);
        command.ExecuteNonQuery();
    }


    private static void InsertDataIntoSQLiteTable(SQLiteConnection connection, DataTable dataTable, string tableName, Action<string> reportProgress)
    {
        using SQLiteTransaction transaction = connection.BeginTransaction();
        using SQLiteCommand command = new(connection);

        foreach (DataRow row in dataTable.Rows)
        {
            StringBuilder insertQuery = new($"INSERT INTO \"{tableName}\" VALUES (");

            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                insertQuery.Append($"@param{i}, ");
            }

            insertQuery.Remove(insertQuery.Length - 2, 2); // Remove the last comma and space
            insertQuery.Append(')');

            command.CommandText = insertQuery.ToString();

            command.Parameters.Clear();
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                command.Parameters.AddWithValue($"@param{i}", row[i]);
            }

            command.ExecuteNonQuery();
        }

        transaction.Commit();
        reportProgress($"Data inserted into {tableName} table successfully.");
    }

    private static string MapDataColumnTypeToSqlite(Type dataType)
    {
        return dataType.Name switch
        {
            "Int32" => "INTEGER",
            "Int64" => "INTEGER",
            "String" => "TEXT",
            "Single" => "REAL",
            "Double" => "REAL",
            _ => "TEXT"
        };
    }

}
