using RHGMTool.Models;
using System.Data;
using System.Data.SQLite;
using static RHGMTool.Models.EnumService;

namespace RHGMTool.Services
{
    public class GMDbService : IGMDbService
    {
        private readonly ISqLiteDatabaseService _databaseService;
        private readonly SQLiteConnection _connection;

        public GMDbService(ISqLiteDatabaseService databaseService)
        {
            _databaseService = databaseService;
            _connection = _databaseService.OpenSQLiteConnection();
        }

        public DataTable CreateCachedItemDataTable(DataTable dataTable, ItemType itemType, string itemTableName)
        {
            try
            {
                DataTable itemDataTable = new();

                string query = GetItemQuery(itemTableName);

                using (var adapter = new SQLiteDataAdapter(query, _connection))
                {
                    adapter.Fill(itemDataTable);
                }

                // Add a column for ItemType and set its value
                itemDataTable.Columns.Add("ItemType", typeof(ItemType));
                foreach (DataRow row in itemDataTable.Rows)
                {
                    row["ItemType"] = itemType;
                }

                // Merge the current itemDataTable into the cachedDataTable
                dataTable.Merge(itemDataTable);
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating ItemDataTable.", ex);
            }

            return dataTable;
        }

        private static string GetItemQuery(string itemTableName)
        {
            return $@"
            SELECT i.nID, i.nWeaponID00, i.szIconName, i.nCategory, i.nSubCategory, i.nBranch, i.nSocketCountMin, i.nSocketCountMax, i.nReconstructionMax, i.nJobClass, i.nLevelLimit, 
                i.nItemTrade, i.nOverlapCnt, i.nDurability, i.nDefense, i.nMagicDefense, i.nWeight, i.nSellPrice, i.nOptionCountMin, i.nOptionCountMax, i.nSetId, i.nFixOption00, i.nFixOptionValue00, i.nFixOption01, i.nFixOptionValue01, i.nPetEatGroup,
                s.wszDesc, s.wszItemDescription
            FROM {itemTableName} i
            LEFT JOIN {itemTableName}_string s ON i.nID = s.nID";
        }

        public List<NameID> GetOptionItems()
        {
            List<NameID> optionItems = [];

            try
            {
                string query = "SELECT nID, wszDescNoColor FROM itemoptionlist";
                using var optionReader = _databaseService.ExecuteReader(query, _connection);

                optionItems.Add(new NameID { ID = 0, Name = "None" });

                while (optionReader.Read())
                {
                    int id = optionReader.GetInt32(0);
                    string name = optionReader.GetString(1);

                    optionItems.Add(new NameID { ID = id, Name = name });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving option items from the database.", ex);
            }

            return optionItems;
        }

        public (int minValue, int maxValue) GetOptionValue(int itemID)
        {
            try
            {
                string query = "SELECT nCheckMinValue, nCheckMaxValue FROM itemoptionlist WHERE nID = @itemID";
                using var optionReader = _databaseService.ExecuteReader(query, _connection, ("@itemID", itemID));

                return optionReader.Read() ? (optionReader.GetInt32(0), optionReader.GetInt32(1)) : (0, 0);
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving option value from the database.", ex);
            }
        }


        private List<NameID> GetItemsFromQuery(string query)
        {
            List<NameID> items = [];

            try
            {
                using var command = _databaseService.ExecuteReader(query, _connection);

                while (command.Read())
                {
                    int id = command.GetInt32(0);
                    string name = command.GetString(1);

                    items.Add(new NameID { ID = id, Name = name });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving items from the database", ex);
            }

            return items;
        }

        public List<NameID> GetTitleItems()
        {
            return GetItemsFromQuery("SELECT nID, wszTitleName FROM charactertitle_string");
        }

        public List<NameID> GetFortuneDescItems()
        {
            return GetItemsFromQuery("SELECT nid, wszDesc FROM fortune WHERE wszFortuneRollDesc = ''");
        }

        public List<NameID> GetLobbyItems()
        {
            return GetItemsFromQuery("SELECT nID, wszDesc FROM serverlobbyid");
        }

        public List<NameID> GetCategoryItems(ItemType itemType, bool isSubCategory)
        {
            List<NameID> categoryItems = [];

            try
            {
                string query;

                if (itemType == ItemType.All || itemType == ItemType.Item)
                {
                    query = isSubCategory ? "SELECT nID, wszName01 FROM itemcategory WHERE wszName01 <> ''" :
                                            "SELECT nID, wszName00 FROM itemcategory WHERE wszName00 <> ''";
                }
                else
                {
                    int[] categoryIDs = isSubCategory ? GetSubCategoryIDsForItemType(itemType) : GetCategoryIDsForItemType(itemType);
                    string categoryIDsStr = string.Join(",", categoryIDs);
                    query = isSubCategory ? $"SELECT nID, wszName01 FROM itemcategory WHERE nID IN ({categoryIDsStr}) AND wszName01 <> ''" :
                                            $"SELECT nID, wszName00 FROM itemcategory WHERE nID IN ({categoryIDsStr}) AND wszName00 <> ''";
                }

                using var command = _connection.CreateCommand();
                command.CommandText = query;

                using var reader = _databaseService.ExecuteReader(query, _connection);

                categoryItems.Add(new NameID { ID = 0, Name = "All" }); // Add an option to show all categories or subcategories

                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string name = reader.GetString(1);

                    categoryItems.Add(new NameID { ID = id, Name = name });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error populating the {(isSubCategory ? "subcategory" : "category")} combobox for {itemType}", ex);
            }

            return categoryItems;
        }

        private static int[] GetCategoryIDsForItemType(ItemType itemType)
        {
            return itemType switch
            {
                ItemType.Weapon => [5, 6, 7, 8, 9, 10, 11, 12, 55, 56, 57, 58],
                ItemType.Armor => [1, 2, 3, 4, 17],
                ItemType.Costume => [18],
                _ => [],
            };
        }

        private static int[] GetSubCategoryIDsForItemType(ItemType itemType)
        {
            return itemType switch
            {
                ItemType.Weapon => [1],
                ItemType.Armor => [2, 3, 4, 5, 6, 7, 8, 9, 10],
                ItemType.Costume => [11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 48, 101, 102, 104, 105, 106, 107, 108, 109],
                _ => [],
            };
        }

        private string GetStringValueFromQuery(string query, params (string, object)[] parameters)
        {
            try
            {
                using var command = _databaseService.ExecuteReader(query, _connection, parameters);

                return command.Read() ? command.GetString(0) : string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving string value from the database", ex);
            }
        }

        public string GetCategoryName(int categoryID)
        {
            string query = "SELECT wszName00 FROM itemcategory WHERE nID = @categoryID";
            return GetStringValueFromQuery(query, ("@categoryID", categoryID));
        }

        public string GetSubCategoryName(int categoryID)
        {
            string query = "SELECT wszName01 FROM itemcategory WHERE nID = @categoryID";
            return GetStringValueFromQuery(query, ("@categoryID", categoryID));
        }

        public string GetSetName(int setId)
        {
            string query = "SELECT wszName FROM setitem_string WHERE nID = @setId";
            return GetStringValueFromQuery(query, ("@setId", setId));
        }

        public string GetOptionName(int optionID)
        {
            string query = "SELECT wszDesc FROM itemoptionlist WHERE nID = @optionID";
            return GetStringValueFromQuery(query, ("@optionID", optionID));
        }

        public string GetFortuneDesc(int fortuneID)
        {
            string query = "SELECT wszDesc FROM fortune WHERE nID = @fortuneID";
            return GetStringValueFromQuery(query, ("@fortuneID", fortuneID));
        }

        public string GetTitleName(int titleID)
        {
            string query = "SELECT wszTitleName FROM charactertitle_string WHERE nID = @titleID";
            return GetStringValueFromQuery(query, ("@titleID", titleID));
        }

        public string GetAddEffectName(int effectID)
        {
            string query = "SELECT wszDescription FROM addeffect_string WHERE nID = @optionID";
            return GetStringValueFromQuery(query, ("@optionID", effectID));
        }

        private int GetIntValueFromQuery(string query, params (string, object)[] parameters)
        {
            try
            {
                using var command = _databaseService.ExecuteReader(query, _connection, parameters);
                return command.Read() ? command.GetInt32(0) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving integer value from the database", ex);
            }
        }

        public int GetTitleCategory(int titleID)
        {
            string query = "SELECT nTitleCategory FROM charactertitle WHERE nID = @titleID";
            return GetIntValueFromQuery(query, ("@titleID", titleID));
        }

        public int GetTitleRemainTime(int titleID)
        {
            string query = "SELECT nRemainTime FROM charactertitle WHERE nID = @titleID";
            return GetIntValueFromQuery(query, ("@titleID", titleID));
        }

        public (int secTime, float value, int maxValue) GetOptionValues(int optionID)
        {
            try
            {
                string query = "SELECT nSecTime, fValue, nCheckMaxValue FROM itemoptionlist WHERE nID = @optionID";
                using var optionCommand = _databaseService.ExecuteReader(query, _connection, ("@optionID", optionID));

                return optionCommand.Read() ? (optionCommand.GetInt32(0), optionCommand.GetFloat(1), optionCommand.GetInt32(2)) : (0, 0, 0);
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving option values from the database: ", ex);
            }
        }

        public (int nPhysicalAttackMin, int nPhysicalAttackMax, int nMagicAttackMin, int nMagicAttackMax) GetWeaponStats(int jbClass, int weaponID)
        {
            var classToTableMap = new Dictionary<int, string>
            {
                { 1, "frantzweapon" },
                { 2, "angelaweapon" },
                { 3, "tudeweapon" },
                { 4, "natashaweapon" }
            };

            try
            {
                if (classToTableMap.TryGetValue(jbClass, out string? tableName))
                {
                    string query = $"SELECT nPhysicalAttackMin, nPhysicalAttackMax, nMagicAttackMin, nMagicAttackMax FROM {tableName} WHERE nID = @WeaponID";
                    using var command = _databaseService.ExecuteReader(query, _connection, ("@WeaponID", weaponID));


                    return command.Read() ? (command.GetInt32(0), command.GetInt32(1), command.GetInt32(2), command.GetInt32(3)) : (0, 0, 0, 0);
                }

                return (0, 0, 0, 0);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving weapon stats from the database", ex);
            }

        }

        public (string fortuneName, string AddEffectDesc00, string AddEffectDesc01, string AddEffectDesc02, string fortuneDesc) GetFortuneValues(int fortuneID)
        {
            try
            {
                string query = "SELECT wszFortuneRollDesc, wszAddEffectDesc00, wszAddEffectDesc01, wszAddEffectDesc02, wszDesc FROM fortune WHERE nID = @fortuneID";
                using var command = _databaseService.ExecuteReader(query, _connection, ("@fortuneID", fortuneID));

                return command.Read() ? (command.IsDBNull(0) ? "Secondary Desc" : command.GetString(0),
                                         command.GetString(1),
                                         command.GetString(2),
                                         command.GetString(3),
                                         command.GetString(4)) :
                                        (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving fortune values from the database", ex);
            }
        }

        public bool IsNameInNickFilter(string characterName)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM nick_filter WHERE wszNick LIKE '%' || @characterName || '%'";
                using var command = _databaseService.ExecuteReader(query, _connection, ("@characterName", characterName));

                long count = command.Read() ? command.GetInt64(0) : 0;

                return count > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking if name is in nick filter", ex);
            }
        }

        public long GetExperienceFromLevel(int level)
        {
            try
            {
                string query = "SELECT i64Exp FROM exp WHERE nID = @level";
                using var command = _databaseService.ExecuteReader(query, _connection, ("@level", level - 1));

                return command.Read() && !command.IsDBNull(0) ? command.GetInt64(0) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving experience from the database", ex);
            }
        }

        public (int titleCategory, int remainTime, int nAddEffectID00, int nAddEffectID01, int nAddEffectID02, int nAddEffectID03, int nAddEffectID04, int nAddEffectID05, string titleDesc) GetTitleInfo(int titleID)
        {
            try
            {
                string query = "SELECT c.nTitleCategory, c.nRemainTime, c.nAddEffectID00, c.nAddEffectID01, c.nAddEffectID02, c.nAddEffectID03, c.nAddEffectID04, c.nAddEffectID05, s.wszTitleDesc FROM charactertitle c LEFT JOIN charactertitle_string s ON c.nID = s.nID WHERE c.nID = @titleID";
                using var command = _databaseService.ExecuteReader(query, _connection, ("@titleID", titleID));

                return command.Read() ?
                    (command.GetInt32(0),
                    command.GetInt32(1),
                    command.GetInt32(2),
                    command.GetInt32(3),
                    command.GetInt32(4),
                    command.GetInt32(5),
                    command.GetInt32(6),
                    command.GetInt32(7),
                    command.GetString(8)) :
                    (0, 0, 0, 0, 0, 0, 0, 0, string.Empty);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving title values from the database", ex);
            }
        }
    }
}
