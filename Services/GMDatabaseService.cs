using RHToolkit.Models;
using RHToolkit.Models.UISettings;
using RHToolkit.Properties;
using System.Data.SQLite;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Services
{
    public class GMDatabaseService(ISqLiteDatabaseService sqLiteDatabaseService) : IGMDatabaseService
    {
        private readonly ISqLiteDatabaseService _sqLiteDatabaseService = sqLiteDatabaseService;

        private readonly string currentLanguage = RegistrySettingsHelper.GetAppLanguage();

        public List<ItemData> GetItemDataList(ItemType itemType, string itemTableName)
        {
            List<ItemData> itemList = [];
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = GetItemQuery(itemTableName);

                using var command = new SQLiteCommand(query, connection);

                command.Parameters.AddWithValue("@ItemType", itemType);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    ItemData item = new()
                    {
                        ItemId = Convert.ToInt32(reader["nID"]),
                        ItemName = reader["wszDesc"].ToString(),
                        Description = currentLanguage == "ko-KR" && itemTableName == "itemlist_costume" ? reader["szItemDescription"].ToString() : reader["wszItemDescription"].ToString(),
                        Type = (int)itemType,
                        WeaponID00 = Convert.ToInt32(reader["nWeaponID00"]),
                        Category = Convert.ToInt32(reader["nCategory"]),
                        SubCategory = Convert.ToInt32(reader["nSubCategory"]),
                        Weight = Convert.ToInt32(reader["nWeight"]),
                        LevelLimit = Convert.ToInt32(reader["nLevelLimit"]),
                        ItemTrade = Convert.ToInt32(reader["nItemTrade"]),
                        OverlapCnt = Convert.ToInt32(reader["nOverlapCnt"]),
                        Durability = Convert.ToInt32(reader["nDurability"]),
                        Defense = Convert.ToInt32(reader["nDefense"]),
                        MagicDefense = Convert.ToInt32(reader["nMagicDefense"]),
                        Branch = Convert.ToInt32(reader["nBranch"]),
                        InventoryType = Convert.ToInt32(reader["nInventoryType"]),
                        AccountStorage = Convert.ToInt32(reader["nAccountStorage"]),
                        OptionCountMax = Convert.ToInt32(reader["nOptionCountMax"]),
                        SocketCountMax = Convert.ToInt32(reader["nSocketCountMax"]),
                        ReconstructionMax = Convert.ToByte(reader["nReconstructionMax"]),
                        SellPrice = Convert.ToInt32(reader["nSellPrice"]),
                        PetFood = Convert.ToInt32(reader["nPetEatGroup"]),
                        JobClass = Convert.ToInt32(reader["nJobClass"]),
                        SetId = Convert.ToInt32(reader["nSetId"]),
                        FixOption1Code = Convert.ToInt32(reader["nFixOption00"]),
                        FixOption1Value = Convert.ToInt32(reader["nFixOptionValue00"]),
                        FixOption2Code = Convert.ToInt32(reader["nFixOption01"]),
                        FixOption2Value = Convert.ToInt32(reader["nFixOptionValue01"]),
                        IconName = reader["szIconName"].ToString()
                    };

                    itemList.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving ItemData from the database.", ex);
            }

            return itemList;
        }

        private string GetItemQuery(string itemTableName)
        {
            string descriptionField = currentLanguage == "ko-KR" && itemTableName == "itemlist_costume" ? "szItemDescription" : "wszItemDescription";

            if (currentLanguage == "ko-KR")
            {
                if (itemTableName == "itemlist_costume")
                {
                    return $@"
                SELECT 
                    nID, nWeaponID00, szIconName, nCategory, nSubCategory, nBranch, nInventoryType, nAccountStorage, nSocketCountMax, nReconstructionMax, nJobClass, nLevelLimit, 
                    nItemTrade, nOverlapCnt, nDurability, nDefense, nMagicDefense, nWeight, nSellPrice, nOptionCountMax, nSetId, 
                    nFixOption00, nFixOptionValue00, nFixOption01, nFixOptionValue01, nPetEatGroup,
                    wszDesc, {descriptionField}
                FROM {itemTableName}";
                }
                else
                {
                    return $@"
                SELECT 
                    nID, nWeaponID00, szIconName, nCategory, nSubCategory, nBranch, nInventoryType, nAccountStorage, nSocketCountMax, nReconstructionMax, nJobClass, nLevelLimit, 
                    nItemTrade, nOverlapCnt, nDurability, nDefense, nMagicDefense, nWeight, nSellPrice, nOptionCountMax, nSetId, 
                    nFixOption00, nFixOptionValue00, nFixOption01, nFixOptionValue01, nPetEatGroup,
                    wszDesc, {descriptionField}
                FROM {itemTableName}";
                }
            }
            else
            {
                return $@"
            SELECT 
                i.nID, i.nWeaponID00, i.szIconName, i.nCategory, i.nSubCategory, i.nBranch, i.nInventoryType, i.nAccountStorage, i.nSocketCountMax, i.nReconstructionMax, i.nJobClass, i.nLevelLimit, 
                i.nItemTrade, i.nOverlapCnt, i.nDurability, i.nDefense, i.nMagicDefense, i.nWeight, i.nSellPrice, i.nOptionCountMax, i.nSetId, 
                i.nFixOption00, i.nFixOptionValue00, i.nFixOption01, i.nFixOptionValue01, i.nPetEatGroup,
                s.wszDesc, s.{descriptionField}
            FROM {itemTableName} i
            LEFT JOIN {itemTableName}_string s ON i.nID = s.nID";
            }
        }

        public List<NameID> GetOptionItems()
        {
            List<NameID> optionItems = [];
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = "SELECT nID, wszDescNoColor FROM itemoptionlist";
                using var optionReader = _sqLiteDatabaseService.ExecuteReader(query, connection);
                optionItems.Add(new NameID { ID = 0, Name = Resources.None });

                while (optionReader.Read())
                {
                    int id = optionReader.GetInt32(0);
                    string name = optionReader.GetString(1);

                    string formattedName = $"({id}) {name}";

                    optionItems.Add(new NameID { ID = id, Name = formattedName });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving option items from the database.", ex);
            }

            return optionItems;
        }

        private List<NameID> GetItemsFromQuery(string query)
        {
            List<NameID> items = [];
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection);

                items.Add(new NameID { ID = 0, Name = Resources.None });

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

        public List<NameID> GetFortuneItems()
        {
            return GetItemsFromQuery("SELECT nid, wszFortuneRollDesc FROM fortune WHERE wszFortuneRollDesc <> ''");
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
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
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

                using var command = connection.CreateCommand();
                command.CommandText = query;

                using var reader = _sqLiteDatabaseService.ExecuteReader(query, connection);

                categoryItems.Add(new NameID { ID = 0, Name = Resources.All }); // Add an option to show all categories or subcategories

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
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection, parameters);

                return command.Read() ? command.GetString(0) : string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving string value from the database", ex);
            }
        }

        public string GetString(int stringID)
        {
            string query = "SELECT wszString FROM string WHERE nID = @stringID";
            return GetStringValueFromQuery(query, ("@stringID", stringID));
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

        public string GetOptionGroupName(int optionID)
        {
            string query = "SELECT wszDesc FROM new_itemoptioncondition_string WHERE nID = @optionID";
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
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection, parameters);
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

        public (int secTime, float value) GetOptionValues(int optionID)
        {
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = "SELECT nSecTime, fValue FROM itemoptionlist WHERE nID = @optionID";
                using var optionCommand = _sqLiteDatabaseService.ExecuteReader(query, connection, ("@optionID", optionID));

                return optionCommand.Read() ? (optionCommand.GetInt32(0), optionCommand.GetFloat(1)) : (0, 0);
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
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                if (classToTableMap.TryGetValue(jbClass, out string? tableName))
                {
                    string query = $"SELECT nPhysicalAttackMin, nPhysicalAttackMax, nMagicAttackMin, nMagicAttackMax FROM {tableName} WHERE nID = @WeaponID";
                    using var command = _sqLiteDatabaseService.ExecuteReader(query, connection, ("@WeaponID", weaponID));


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
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = "SELECT wszFortuneRollDesc, wszAddEffectDesc00, wszAddEffectDesc01, wszAddEffectDesc02, wszDesc FROM fortune WHERE nID = @fortuneID";
                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection, ("@fortuneID", fortuneID));

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
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = "SELECT COUNT(*) FROM nick_filter WHERE wszNick LIKE '%' || @characterName || '%'";
                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection, ("@characterName", characterName));

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
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = "SELECT i64Exp FROM exp WHERE nID = @level";
                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection, ("@level", level - 1));

                return command.Read() && !command.IsDBNull(0) ? command.GetInt64(0) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving experience from the database", ex);
            }
        }

        public (int titleCategory, int remainTime, int nAddEffectID00, int nAddEffectID01, int nAddEffectID02, int nAddEffectID03, int nAddEffectID04, int nAddEffectID05, string titleDesc) GetTitleInfo(int titleID)
        {
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = "SELECT c.nTitleCategory, c.nRemainTime, c.nAddEffectID00, c.nAddEffectID01, c.nAddEffectID02, c.nAddEffectID03, c.nAddEffectID04, c.nAddEffectID05, s.wszTitleDesc FROM charactertitle c LEFT JOIN charactertitle_string s ON c.nID = s.nID WHERE c.nID = @titleID";
                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection, ("@titleID", titleID));

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

        public (int nSetOption00, int nSetOptionvlue00, int nSetOption01, int nSetOptionvlue01, int nSetOption02, int nSetOptionvlue02, int nSetOption03, int nSetOptionvlue03, int nSetOption04, int nSetOptionvlue04) GetSetInfo(int setID)
        {
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = "SELECT nSetOption00, nSetOptionvlue00, nSetOption01, nSetOptionvlue01, nSetOption02, nSetOptionvlue02, nSetOption03, nSetOptionvlue03, nSetOption04, nSetOptionvlue04 FROM setitem WHERE nID = @setID";
                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection, ("@setID", setID));

                return command.Read() ?
                    (command.GetInt32(0),
                    command.GetInt32(1),
                    command.GetInt32(2),
                    command.GetInt32(3),
                    command.GetInt32(4),
                    command.GetInt32(5),
                    command.GetInt32(6),
                    command.GetInt32(7),
                    command.GetInt32(8),
                    command.GetInt32(9)) :
                    (0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving set values from the database", ex);
            }
        }

        private double GetDoubleValueFromQuery(string query, params (string, object)[] parameters)
        {
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection, parameters);
                return command.Read() ? command.GetDouble(0) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving double value from the database", ex);
            }
        }

        public double GetEnhanceValue(int enhanceValue)
        {
            string query = "SELECT fWeaponValue FROM enchantinfo WHERE nID = @enhanceValue";
            return GetDoubleValueFromQuery(query, ("@enhanceValue", enhanceValue));
        }
    }
}
