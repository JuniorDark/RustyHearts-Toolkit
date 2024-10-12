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
                        BindingOff = Convert.ToInt32(reader["nBindingOff"]),
                        ReconstructionMax = Convert.ToByte(reader["nReconstructionMax"]),
                        SellPrice = Convert.ToInt32(reader["nSellPrice"]),
                        PetFood = Convert.ToInt32(reader["nPetEatGroup"]),
                        JobClass = Convert.ToInt32(reader["nJobClass"]),
                        SetId = Convert.ToInt32(reader["nSetId"]),
                        TitleList = Convert.ToInt32(reader["nTitleList"]),
                        Cooltime = Convert.ToSingle(reader["fCooltime"]),
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
                throw new Exception($"Error retrieving ItemData from the database: {ex.Message}", ex);
            }

            return itemList;
        }

        public List<ItemData> GetItemDataLists()
        {
            List<ItemData> itemList =
                [
                    .. GetItemDataList(ItemType.Item, "itemlist"),
                    .. GetItemDataList(ItemType.Costume, "itemlist_costume"),
                    .. GetItemDataList(ItemType.Armor, "itemlist_armor"),
                    .. GetItemDataList(ItemType.Weapon, "itemlist_weapon"),
                ];

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
                    nFixOption00, nFixOptionValue00, nFixOption01, nFixOptionValue01, nPetEatGroup, nTitleList, fCooltime, nBindingOff,  
                    wszDesc, {descriptionField}
                FROM {itemTableName}";
                }
                else
                {
                    return $@"
                SELECT 
                    nID, nWeaponID00, szIconName, nCategory, nSubCategory, nBranch, nInventoryType, nAccountStorage, nSocketCountMax, nReconstructionMax, nJobClass, nLevelLimit, 
                    nItemTrade, nOverlapCnt, nDurability, nDefense, nMagicDefense, nWeight, nSellPrice, nOptionCountMax, nSetId, 
                    nFixOption00, nFixOptionValue00, nFixOption01, nFixOptionValue01, nPetEatGroup, nTitleList, fCooltime, nBindingOff, 
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
                i.nFixOption00, i.nFixOptionValue00, i.nFixOption01, i.nFixOptionValue01, i.nPetEatGroup, nTitleList, fCooltime, nBindingOff, 
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

        private List<NameID> GetItemsFromQuery(string query, string? type = null)
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

                    string formattedName;

                    if (type != null)
                    {
                        formattedName = $"({type}) ({id}) {name}";
                    }
                    else
                    {
                        formattedName = $"({id}) {name}";
                    }

                    items.Add(new NameID { ID = id, Name = formattedName, Type = type });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving items from the database: {ex.Message}", ex);
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

        public List<NameID> GetQuestListItems()
        {
            return GetItemsFromQuery("SELECT nID, wszTitle FROM queststring");
        }

        public List<NameID> GetAddEffectItems()
        {
            return GetItemsFromQuery("SELECT nID, wszDescription FROM addeffect_string");
        }

        public List<NameID> GetNpcShopItems()
        {
            return GetItemsFromQuery("SELECT nID, wszEct FROM npcshop", "NpcShop");
        }

        public List<NameID> GetQuestGroupItems()
        {
            return GetItemsFromQuery("SELECT nID, wszNpcNameTitle FROM questgroup");
        }

        public List<NameID> GetStringItems()
        {
            return GetItemsFromQuery("SELECT nID, wszString FROM string");
        }

        public List<NameID> GetNpcListItems()
        {
            return GetItemsFromQuery("SELECT nID, wszName FROM npc");
        }

        public List<NameID> GetNpcInstanceItems()
        {
            return GetItemsFromQuery("SELECT nID, wszDesc FROM npcinstance_string");
        }

        public List<NameID> GetNpcDialogItems()
        {
            return GetItemsFromQuery("SELECT nID, wszDesc FROM npc_dialog");
        }

        public List<NameID> GetFielMeshItems()
        {
            return GetItemsFromQuery("SELECT nID, wszDesc FROM itemfieldmesh");
        }

        public List<NameID> GetUnionPackageItems()
        {
            return GetItemsFromQuery("SELECT nID, wszName FROM unionpackage_string");
        }

        public List<NameID> GetCostumePackItems()
        {
            return GetItemsFromQuery("SELECT nID, wszDesc FROM costumepack");
        }

        public List<NameID> GetTitleListItems()
        {
            return GetItemsFromQuery("SELECT nID, wszTitleName FROM charactertitle_string");
        }

        public List<NameID> GetSetItemItems()
        {
            return GetItemsFromQuery("SELECT nID, wszName FROM setitem_string");
        }

        public List<NameID> GetPetEatItems()
        {
            return GetItemsFromQuery("SELECT nID, wszDesc FROM peteatitem");
        }

        public List<NameID> GetPetRebirthItems()
        {
            return GetItemsFromQuery("SELECT nID, wszMemo FROM petrebirth");
        }

        public List<NameID> GetRiddleGroupItems()
        {
            return GetItemsFromQuery("SELECT nID, wszLvelDisc FROM riddleboxdropgrouplist");
        }

        public List<NameID> GetWorldNameItems()
        {
            return GetItemsFromQuery("SELECT nID, wszNameUI FROM world_string");
        }

        public List<NameID> GetMissionItems()
        {
            return GetItemsFromQuery("SELECT nID, wszTitle FROM missionstring");
        }

        public List<NameID> GetAuctionCategoryItems()
        {
            return GetItemsFromQuery("SELECT nID, wszName00 FROM auctioncategory WHERE wszName00 <> ''");
        }

        public List<NameID> GetCostumePartItems(int jobClass)
        {
            string tableName = jobClass switch
            {
                0 or 1 => "frantzparts",
                2 => "angelaparts",
                3 => "tudeparts",
                4 => "natashaparts",
                101 => "frantz_avatar01_parts",
                102 => "frantz_avatar02_parts",
                201 => "angela_avatar01_parts",
                301 => "tude_avatar01_parts",
                401 => "natasha_avatar01_parts",
                _ => throw new Exception($"Invalid class: {jobClass}")
            };

            return GetItemsFromQuery($"SELECT nID, wszName FROM {tableName}");
        }

        public List<NameID> GetSkillListItems(int jobClass)
        {
            (string skillTableName, string stringTableName) = jobClass switch
            {
                1 or 101 or 102 => ("frantzskill", "frantzskill_string"),
                2 or 201 => ("angelaskill", "angelaskill_string"),
                3 or 301 => ("tudeskill", "tudeskill_string"),
                4 or 401 => ("natashaskill", "natashaskill_string"),
                _ => throw new Exception($"Invalid class: {jobClass}")
            };

            string query = $@"
            SELECT s.nSkillID, ss.wszName 
            FROM {skillTableName} s
            JOIN {stringTableName} ss ON s.nID = ss.nID";

            return GetItemsFromQuery(query);
        }

        public List<NameID> GetTradeItemGroupItems()
        {
            List<NameID> items = [];
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = "SELECT nID, nCategoryName FROM tradeitemgroup";

                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection);

                items.Add(new NameID { ID = 0, Name = Resources.None });

                while (command.Read())
                {
                    int id = command.GetInt32(0);
                    int name = command.GetInt32(1);

                    string formattedName = $"({id}) {GetString(name)}";

                    items.Add(new NameID { ID = id, Name = formattedName });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving items from the database: {ex.Message}", ex);
            }

            return items;
        }

        public List<RareCardReward> GetRareCardRewardItems()
        {
            List<RareCardReward> items = [];
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                var columnNames = string.Join(", ", Enumerable.Range(1, 40).Select(i => $"nRewardItem{i:D2}"));
                string query = $"SELECT nID, {columnNames} FROM rarecardrewarditemlist";

                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection);

                while (command.Read())
                {
                    RareCardReward reward = new()
                    {
                        ID = command.GetInt32(0)
                    };

                    for (int i = 1; i <= 40; i++)
                    {
                        var property = typeof(RareCardReward).GetProperty($"RewardItem{i:D2}");
                        property?.SetValue(reward, command.GetInt32(i));
                    }

                    items.Add(reward);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving items from the database: {ex.Message}", ex);
            }

            return items;
        }

        public List<int> GetNpcShopItems(int shopID)
        {
            List<int> items = [];
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string columns = string.Join(", ", Enumerable.Range(0, 20).Select(i => $"nItem{i:00}"));
                string query = $"SELECT {columns} FROM npcshop WHERE nID = {shopID}";

                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection);

                while (command.Read())
                {
                    for (int i = 0; i < 20; i++)
                    {
                        int item = command.GetInt32(i);
                        items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving npcshop items from the database: {ex.Message}", ex);
            }

            return items;
        }

        public List<int> GetTradeShopItems(int shopID)
        {
            List<int> items = [];
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = $"SELECT nItemID FROM tradeshop WHERE nGroupID = {shopID}";

                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection);

                while (command.Read())
                {
                    int item = command.GetInt32(0);
                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving tradeshop items from the database: {ex.Message}", ex);
            }

            return items;
        }

        public List<int> GetItemMixItems(string groupIDs)
        {
            List<int> items = [];
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                var groupIDList = groupIDs.Split(',').Select(id => id.Trim()).ToList();

                string query = "SELECT nID FROM itemmix WHERE szGroup IN (" + string.Join(",", groupIDList.Select(id => $"'{id}'")) + ")";

                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection);

                while (command.Read())
                {
                    int item = command.GetInt32(0);
                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving itemmix items from the database: {ex.Message}", ex);
            }

            return items;
        }

        public List<int> GetCostumeMixItems(string groupIDs)
        {
            List<int> items = [];
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                var groupIDList = groupIDs.Split(',').Select(id => id.Trim()).ToList();

                string query = "SELECT nID FROM costumemix WHERE nGroup IN (" + string.Join(",", groupIDList) + ")";

                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection);

                while (command.Read())
                {
                    int item = command.GetInt32(0);
                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving costumemix items from the database: {ex.Message}", ex);
            }

            return items;
        }

        public ObservableCollection<ItemMixData> GetItemMixList()
        {
            ObservableCollection<ItemMixData> items = [];
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();

            try
            {
                var itemList = GetItemDataLists();

                var itemDictionary = itemList.ToDictionary(i => i.ItemId, i => i);

                string query = @"
                SELECT 
                    nID, 
                    nMixAble, 
                    fItemMixPro00, 
                    nItemMixCo00, 
                    fItemMixPro01, 
                    nItemMixCo01, 
                    fItemMixPro02, 
                    nItemMixCo02, 
                    nCost, 
                    nItemCode00, 
                    nItemCount00, 
                    nItemCode01, 
                    nItemCount01, 
                    nItemCode02, 
                    nItemCount02, 
                    nItemCode03, 
                    nItemCount03, 
                    nItemCode04, 
                    nItemCount04, 
                    szGroup AS MixGroup,
                    '1' AS MixType 
                FROM itemmix
                UNION ALL
                SELECT 
                    nID, 
                    nMixAble, 
                    fItemMixPro00, 
                    nItemMixCo00, 
                    fItemMixPro01, 
                    nItemMixCo01, 
                    fItemMixPro02, 
                    nItemMixCo02, 
                    nCost, 
                    nItemCode00, 
                    nItemCount00, 
                    nItemCode01, 
                    nItemCount01, 
                    nItemCode02, 
                    nItemCount02, 
                    nItemCode03, 
                    nItemCount03, 
                    nItemCode04, 
                    nItemCount04, 
                    CAST(nGroup AS TEXT) AS MixGroup,
                    '2' AS MixType 
                FROM costumemix
                ";

                using var reader = _sqLiteDatabaseService.ExecuteReader(query, connection);

                while (reader.Read())
                {
                    int itemId = Convert.ToInt32(reader["nID"]);

                    itemDictionary.TryGetValue(itemId, out var cachedItem);

                    ItemMixData item = new()
                    {
                        ID = itemId,
                        MixAble = Convert.ToInt32(reader["nMixAble"]),
                        ItemMixPro00 = Convert.ToSingle(reader["fItemMixPro00"]),
                        ItemMixCo00 = Convert.ToInt32(reader["nItemMixCo00"]),
                        ItemMixPro01 = Convert.ToSingle(reader["fItemMixPro01"]),
                        ItemMixCo01 = Convert.ToInt32(reader["nItemMixCo01"]),
                        ItemMixPro02 = Convert.ToSingle(reader["fItemMixPro02"]),
                        ItemMixCo02 = Convert.ToInt32(reader["nItemMixCo02"]),
                        Cost = Convert.ToInt32(reader["nCost"]),
                        ItemCode00 = Convert.ToInt32(reader["nItemCode00"]),
                        ItemCount00 = Convert.ToInt32(reader["nItemCount00"]),
                        ItemCode01 = Convert.ToInt32(reader["nItemCode01"]),
                        ItemCount01 = Convert.ToInt32(reader["nItemCount01"]),
                        ItemCode02 = Convert.ToInt32(reader["nItemCode02"]),
                        ItemCount02 = Convert.ToInt32(reader["nItemCount02"]),
                        ItemCode03 = Convert.ToInt32(reader["nItemCode03"]),
                        ItemCount03 = Convert.ToInt32(reader["nItemCount03"]),
                        ItemCode04 = Convert.ToInt32(reader["nItemCode04"]),
                        ItemCount04 = Convert.ToInt32(reader["nItemCount04"]),
                        MixGroup = reader["MixGroup"].ToString(),
                        MixType = Convert.ToInt32(reader["MixType"]),
                        ItemName = cachedItem?.ItemName ?? "Unknown Item",
                        IconName = cachedItem?.IconName ?? "icon_empty_def",
                        ItemBranch = cachedItem?.Branch ?? 0,
                    };

                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving item mix data from the database: {ex.Message}", ex);
            }

            return items;
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

                categoryItems.Add(new NameID { ID = 0, Name = Resources.None });

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

        public List<NameID> GetSubCategoryItems()
        {
            return GetItemsFromQuery("SELECT nID, wszName02 FROM itemcategory WHERE wszName02 <> ''");
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

        private List<NameID> GetUniqueItems(string query, bool isInt, string? type = null)
        {
            var items = GetUniqueItemsFromQuery(query, isInt);

            HashSet<string> uniqueGroups = [];

            foreach (var item in items)
            {
                if (!isInt)
                {
                    var groups = (item.Name?.Split(',') ?? [])
                                  .Select(g => g.Trim());

                    foreach (var group in groups)
                    {
                        if (!string.IsNullOrEmpty(group))
                        {
                            uniqueGroups.Add(group);
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(item.Name))
                    {
                        uniqueGroups.Add(item.Name);
                    }
                }
            }

            List<NameID> result = [.. uniqueGroups
                .Select(group => new NameID { ID = int.Parse(group), Name = type != null ?  $"({type}) {group}": group, Type = type})
                .OrderBy(item => item.ID)];

            return result;
        }

        private List<NameID> GetUniqueItemsFromQuery(string query, bool isInt)
        {
            List<NameID> items = [];

            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection);

                while (command.Read())
                {
                    if (isInt)
                    {
                        int id = command.GetInt32(0);
                        items.Add(new NameID { ID = id, Name = id.ToString() });
                    }
                    else
                    {
                        string id = command.GetString(0);
                        items.Add(new NameID { ID = 0, Name = id });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving items from the database: {ex.Message}", ex);
            }

            return items;
        }

        public List<NameID> GetItemMixGroupItems()
        {
            return GetUniqueItems("SELECT szGroup FROM itemmix", false, "ItemMix");
        }

        public List<NameID> GetCostumeMixGroupItems()
        {
            return GetUniqueItems("SELECT nGroup FROM costumemix", true, "CostumeMix");
        }

        public List<NameID> GetTradeShopGroupItems()
        {
            return GetUniqueItems("SELECT nGroupID FROM tradeshop", true, "TradeShop");
        }

        public List<NameID> GetNpcShopsItems()
        {
            var npcShopItems = GetNpcShopItems();
            var tradeShopGroupItems = GetTradeShopGroupItems();

            var mergedItems = npcShopItems.Concat(tradeShopGroupItems).ToList();

            mergedItems = [.. mergedItems.OrderBy(item => item.ID)];

            return mergedItems;
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

        public string GetSubCategory02Name(int categoryID)
        {
            string query = "SELECT wszName02 FROM itemcategory WHERE nID = @categoryID";
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

        public (float weaponValue, int weaponPlus) GetWeaponEnhanceValue(int enhanceValue)
        {
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = "SELECT fWeaponValue, nWeaponPlus FROM enchantinfo WHERE nID = @enhanceValue";
                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection, ("@enhanceValue", enhanceValue));

                return command.Read() ?
                    (command.GetFloat(0),
                    command.GetInt32(1)) :
                    (0, 0);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving enchant values from the database", ex);
            }
        }

        public (float defenseValue, int defensePlus) GetArmorEnhanceValue(int enhanceValue)
        {
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = "SELECT fDefenseValue, nDefensePlus FROM enchantinfo WHERE nID = @enhanceValue";
                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection, ("@enhanceValue", enhanceValue));

                return command.Read() ?
                    (command.GetFloat(0),
                    command.GetInt32(1)) :
                    (0, 0);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving enchant values from the database", ex);
            }
        }
    }
}
