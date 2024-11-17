using RHToolkit.Models;
using RHToolkit.Models.UISettings;
using System.Data.SQLite;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Services
{
    /// <summary>
    /// Service for interacting with the SQLite database.
    /// </summary>
    public class GMDatabaseService(ISqLiteDatabaseService sqLiteDatabaseService) : IGMDatabaseService
    {
        private readonly ISqLiteDatabaseService _sqLiteDatabaseService = sqLiteDatabaseService;

        private readonly string currentLanguage = RegistrySettingsHelper.GetAppLanguage();

        #region ItemData

        /// <summary>
        /// Retrieves a list of item data from the database based on the specified item type and table name.
        /// </summary>
        /// <param name="itemType">The type of item.</param>
        /// <param name="itemTableName">The name of the table containing the item data.</param>
        /// <returns>A list of item data.</returns>
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

        /// <summary>
        /// Retrieves a list of all item data from the database.
        /// </summary>
        /// <returns>A list of all item data.</returns>
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

        /// <summary>
        /// Constructs the SQL query to retrieve item data based on the table name.
        /// </summary>
        /// <param name="itemTableName">The name of the table containing the item data.</param>
        /// <returns>The SQL query string.</returns>
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
                i.nFixOption00, i.nFixOptionValue00, i.nFixOption01, i.nFixOptionValue01, i.nPetEatGroup, i.nTitleList, i.fCooltime, i.nBindingOff, 
                s.wszDesc, s.{descriptionField}
                FROM {itemTableName} i
                LEFT JOIN {itemTableName}_string s ON i.nID = s.nID";
            }
        }

        #endregion

        #region SkillData

        /// <summary>
        /// Retrieves a list of skill data from the database based on the specified skill type and table name.
        /// </summary>
        /// <param name="skillType">The type of skill.</param>
        /// <param name="skillTableName">The name of the table containing the skill data.</param>
        /// <returns>A list of skill data.</returns>
        public List<SkillData> GetSkillDataList(SkillType skillType, string skillTableName)
        {
            List<SkillData> skillList = [];
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = GetSkillQuery(skillTableName);

                using var command = new SQLiteCommand(query, connection);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var characterType = reader["szCharacterType"].ToString();

                    int characterTypeValue = characterType switch
                    {
                        "TYPE_ALL" => 0,
                        "TYPE_A" => 1,
                        "TYPE_B" => 2,
                        "TYPE_C" => 3,
                        _ => 0,
                    };
                    SkillData skill = new()
                    {
                        CharacterSkillType = skillType,
                        ID = Convert.ToInt32(reader["nID"]),
                        SkillID = Convert.ToInt32(reader["nSkillID"]),
                        SkillName = reader["wszName"].ToString(),
                        IconName = reader["szIcon"].ToString(),
                        Description1 = reader["wszDescription"].ToString(),
                        Description2 = reader["wszDescription1"].ToString(),
                        Description3 = reader["wszDescription2"].ToString(),
                        Description4 = reader["wszDescription3"].ToString(),
                        Description5 = reader["wszDescription4"].ToString(),
                        SkillLevel = Convert.ToInt32(reader["nSkillLevel"]),
                        RequiredLevel = Convert.ToInt32(reader["nLearnLevel"]),
                        MPCost = Convert.ToSingle(reader["fMP"]),
                        SPCost = Convert.ToInt32(reader["nCost"]),
                        Cooltime = Convert.ToSingle(reader["fCoolTime"]),
                        SkillType = reader["szSkillType"].ToString(),
                        CharacterType = characterType,
                        CharacterTypeValue = characterTypeValue
                    };

                    skillList.Add(skill);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving SkillData from the database: {ex.Message}\n {ex.StackTrace}", ex);
            }

            return skillList;
        }

        /// <summary>
        /// Retrieves a list of all skill data from the database.
        /// </summary>
        /// <returns>A list of all skill data.</returns>
        public List<SkillData> GetSkillDataLists()
        {
            List<SkillData> skillList =
                [
                    .. GetSkillDataList(SkillType.SkillFrantz, "frantzskill"),
                    .. GetSkillDataList(SkillType.SkillAngela, "angelaskill"),
                    .. GetSkillDataList(SkillType.SkillTude, "tudeskill"),
                    .. GetSkillDataList(SkillType.SkillNatasha, "natashaskill"),
                ];

            return skillList;
        }

        /// <summary>
        /// Constructs the SQL query to retrieve skill data based on the table name.
        /// </summary>
        /// <param name="skillTableName">The name of the table containing the skill data.</param>
        /// <returns>The SQL query string.</returns>
        private static string GetSkillQuery(string skillTableName)
        {
            return $@"
            SELECT 
                i.nID, i.nSkillID, i.nSkillLevel, i.nLearnLevel, i.fMP, i.szIcon, i.nCost, i.fCoolTime, i.szSkillType, i.szCharacterType,
                s.wszName, s.wszDescription, s.wszDescription1, s.wszDescription2, s.wszDescription3, s.wszDescription4
            FROM {skillTableName} i
            LEFT JOIN {skillTableName}_string s ON i.nID = s.nID";
        }
        #endregion

        /// <summary>
        /// Retrieves a list of option items from the database.
        /// </summary>
        /// <returns>A list of option items.</returns>
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
                throw new Exception(ex.Message, ex);
            }

            return optionItems;
        }

        /// <summary>
        /// Retrieves a list of items from the database based on the specified query and type.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="type">The type of items to retrieve.</param>
        /// <returns>A list of items.</returns>
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
                    string name;

                    if (command.FieldCount < 2)
                    {
                        name = id.ToString();
                    }
                    else
                    {
                        name = command.GetString(1);
                    }

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
                throw new Exception(ex.Message, ex);
            }

            return items;
        }

        /// <summary>
        /// Retrieves a list of title items from the database.
        /// </summary>
        /// <returns>A list of title items.</returns>
        public List<NameID> GetTitleItems()
        {
            return GetItemsFromQuery("SELECT nID, wszTitleName FROM charactertitle_string");
        }

        /// <summary>
        /// Retrieves a list of fortune items from the database.
        /// </summary>
        /// <returns>A list of fortune items.</returns>
        public List<NameID> GetFortuneItems()
        {
            return GetItemsFromQuery("SELECT nid, wszFortuneRollDesc FROM fortune WHERE wszFortuneRollDesc <> ''");
        }

        /// <summary>
        /// Retrieves a list of fortune description items from the database.
        /// </summary>
        /// <returns>A list of fortune description items.</returns>
        public List<NameID> GetFortuneDescItems()
        {
            return GetItemsFromQuery("SELECT nid, wszDesc FROM fortune WHERE wszFortuneRollDesc = ''");
        }

        /// <summary>
        /// Retrieves a list of lobby items from the database.
        /// </summary>
        /// <returns>A list of lobby items.</returns>
        public List<NameID> GetLobbyItems()
        {
            return GetItemsFromQuery("SELECT nID, wszDesc FROM serverlobbyid");
        }

        /// <summary>
        /// Retrieves a list of quest list items from the database.
        /// </summary>
        /// <returns>A list of quest list items.</returns>
        public List<NameID> GetQuestListItems()
        {
            return GetItemsFromQuery("SELECT nID, wszTitle FROM queststring");
        }

        /// <summary>
        /// Retrieves a list of add effect items from the database.
        /// </summary>
        /// <returns>A list of add effect items.</returns>
        public List<NameID> GetAddEffectItems()
        {
            return GetItemsFromQuery("SELECT nID, wszDescription FROM addeffect_string");
        }

        /// <summary>
        /// Retrieves a list of NPC shop items from the database.
        /// </summary>
        /// <returns>A list of NPC shop items.</returns>
        public List<NameID> GetNpcShopItems()
        {
            return GetItemsFromQuery("SELECT nID, wszEct FROM npcshop", "NpcShop");
        }

        /// <summary>
        /// Retrieves a list of quest group items from the database.
        /// </summary>
        /// <returns>A list of quest group items.</returns>
        public List<NameID> GetQuestGroupItems()
        {
            return GetItemsFromQuery("SELECT nID, wszNpcNameTitle FROM questgroup");
        }

        /// <summary>
        /// Retrieves a list of string items from the database.
        /// </summary>
        /// <returns>A list of string items.</returns>
        public List<NameID> GetStringItems()
        {
            return GetItemsFromQuery("SELECT nID, wszString FROM string");
        }

        /// <summary>
        /// Retrieves a list of NPC list items from the database.
        /// </summary>
        /// <returns>A list of NPC list items.</returns>
        public List<NameID> GetNpcListItems()
        {
            return GetItemsFromQuery("SELECT nID, wszName FROM npc");
        }

        /// <summary>
        /// Retrieves a list of NPC instance items from the database.
        /// </summary>
        /// <returns>A list of NPC instance items.</returns>
        public List<NameID> GetNpcInstanceItems()
        {
            return GetItemsFromQuery("SELECT nID, wszDesc FROM npcinstance_string");
        }

        /// <summary>
        /// Retrieves a list of NPC dialog items from the database.
        /// </summary>
        /// <returns>A list of NPC dialog items.</returns>
        public List<NameID> GetNpcDialogItems()
        {
            return GetItemsFromQuery("SELECT nID, wszDesc FROM npc_dialog");
        }

        /// <summary>
        /// Retrieves a list of field mesh items from the database.
        /// </summary>
        /// <returns>A list of field mesh items.</returns>
        public List<NameID> GetFielMeshItems()
        {
            return GetItemsFromQuery("SELECT nID, wszDesc FROM itemfieldmesh");
        }

        /// <summary>
        /// Retrieves a list of union package items from the database.
        /// </summary>
        /// <returns>A list of union package items.</returns>
        public List<NameID> GetUnionPackageItems()
        {
            return GetItemsFromQuery("SELECT nID, wszName FROM unionpackage_string");
        }

        /// <summary>
        /// Retrieves a list of costume pack items from the database.
        /// </summary>
        /// <returns>A list of costume pack items.</returns>
        public List<NameID> GetCostumePackItems()
        {
            return GetItemsFromQuery("SELECT nID, wszDesc FROM costumepack");
        }

        /// <summary>
        /// Retrieves a list of title list items from the database.
        /// </summary>
        /// <returns>A list of title list items.</returns>
        public List<NameID> GetTitleListItems()
        {
            return GetItemsFromQuery("SELECT nID, wszTitleName FROM charactertitle_string");
        }

        /// <summary>
        /// Retrieves a list of set item items from the database.
        /// </summary>
        /// <returns>A list of set item items.</returns>
        public List<NameID> GetSetItemItems()
        {
            return GetItemsFromQuery("SELECT nID, wszName FROM setitem_string");
        }

        /// <summary>
        /// Retrieves a list of pet eat items from the database.
        /// </summary>
        /// <returns>A list of pet eat items.</returns>
        public List<NameID> GetPetEatItems()
        {
            return GetItemsFromQuery("SELECT nID, wszDesc FROM peteatitem");
        }

        /// <summary>
        /// Retrieves a list of pet rebirth items from the database.
        /// </summary>
        /// <returns>A list of pet rebirth items.</returns>
        public List<NameID> GetPetRebirthItems()
        {
            return GetItemsFromQuery("SELECT nID, wszMemo FROM petrebirth");
        }

        /// <summary>
        /// Retrieves a list of riddle group items from the database.
        /// </summary>
        /// <returns>A list of riddle group items.</returns>
        public List<NameID> GetRiddleGroupItems()
        {
            return GetItemsFromQuery("SELECT nID, wszLvelDisc FROM riddleboxdropgrouplist");
        }

        /// <summary>
        /// Retrieves a list of world name items from the database.
        /// </summary>
        /// <returns>A list of world name items.</returns>
        public List<NameID> GetWorldNameItems()
        {
            return GetItemsFromQuery("SELECT nID, wszNameUI FROM world_string");
        }

        /// <summary>
        /// Retrieves a list of mission items from the database.
        /// </summary>
        /// <returns>A list of mission items.</returns>
        public List<NameID> GetMissionItems()
        {
            return GetItemsFromQuery("SELECT nID, wszTitle FROM missionstring");
        }

        /// <summary>
        /// Retrieves a list of auction category items from the database.
        /// </summary>
        /// <returns>A list of auction category items.</returns>
        public List<NameID> GetAuctionCategoryItems()
        {
            return GetItemsFromQuery("SELECT nID, wszName00 FROM auctioncategory WHERE wszName00 <> ''");
        }

        /// <summary>
        /// Retrieves a list of enemy name items from the database.
        /// </summary>
        /// <returns>A list of enemy name items.</returns>
        public List<string> GetEnemyNameItems()
        {
            List<string> items = [];
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = $"SELECT wszName FROM enemy_string";

                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection);

                while (command.Read())
                {
                    string item = command.GetString(0);
                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

            return items;
        }

        /// <summary>
        /// Retrieves a list of costume part items from the database based on the specified job class.
        /// </summary>
        /// <param name="jobClass">The job class.</param>
        /// <returns>A list of costume part items.</returns>
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

        /// <summary>
        /// Retrieves a list of skill list items from the database based on the specified job class.
        /// </summary>
        /// <param name="jobClass">The job class.</param>
        /// <returns>A list of skill list items.</returns>
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

        /// <summary>
        /// Retrieves a list of trade item group items from the database.
        /// </summary>
        /// <returns>A list of trade item group items.</returns>
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
                throw new Exception(ex.Message, ex);
            }

            return items;
        }

        /// <summary>
        /// Retrieves a list of rare card reward items from the database.
        /// </summary>
        /// <returns>A list of rare card reward items.</returns>
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
                throw new Exception(ex.Message, ex);
            }

            return items;
        }

        /// <summary>
        /// Retrieves a list of rare card drop group list items from the database.
        /// </summary>
        /// <returns>A list of rare card drop group list items.</returns>
        public List<DropGroupList> GetRareCardDropGroupListItems()
        {
            List<DropGroupList> items = [];
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = $"SELECT nID, nBronzeCardID, nSilverCardID, nGoldCardID FROM rarecarddropgrouplist";

                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection);

                while (command.Read())
                {
                    DropGroupList reward = new()
                    {
                        ID = command.GetInt32(0),
                        BronzeCardID = command.GetInt32(1),
                        SilverCardID = command.GetInt32(2),
                        GoldCardID = command.GetInt32(3)
                    };

                    items.Add(reward);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

            return items;
        }

        /// <summary>
        /// Retrieves a list of drop group list items from the database based on the specified table name and drop item count.
        /// </summary>
        /// <param name="tableName">The name of the table containing the drop group list items.</param>
        /// <param name="dropItemCount">The number of drop items.</param>
        /// <returns>A list of drop group list items.</returns>
        public List<DropGroupList> GetDropGroupListItems(string tableName, int dropItemCount)
        {
            List<DropGroupList> items = [];
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                var columnNames = string.Join(", ", Enumerable.Range(1, dropItemCount).Select(i => $"nDropItemCode{i:D2}"));
                string query = $"SELECT nID, {columnNames} FROM {tableName}";

                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection);

                while (command.Read())
                {
                    DropGroupList dropGroup = new()
                    {
                        ID = command.GetInt32(0)
                    };

                    for (int i = 1; i <= dropItemCount; i++)
                    {
                        var property = typeof(DropGroupList).GetProperty($"DropItemCode{i:D2}");
                        property?.SetValue(dropGroup, command.GetInt32(i));
                    }

                    items.Add(dropGroup);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

            return items;
        }

        /// <summary>
        /// Retrieves a list of NPC shop items from the database based on the specified shop ID.
        /// </summary>
        /// <param name="shopID">The shop ID.</param>
        /// <returns>A list of NPC shop items.</returns>
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
                throw new Exception(ex.Message, ex);
            }

            return items;
        }

        /// <summary>
        /// Retrieves a list of trade shop items from the database based on the specified shop ID.
        /// </summary>
        /// <param name="shopID">The shop ID.</param>
        /// <returns>A list of trade shop items.</returns>
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
                throw new Exception(ex.Message, ex);
            }

            return items;
        }

        /// <summary>
        /// Retrieves a list of item mix items from the database based on the specified group IDs.
        /// </summary>
        /// <param name="groupIDs">The group IDs.</param>
        /// <returns>A list of item mix items.</returns>
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
                throw new Exception(ex.Message, ex);
            }

            return items;
        }

        /// <summary>
        /// Retrieves a list of costume mix items from the database based on the specified group IDs.
        /// </summary>
        /// <param name="groupIDs">The group IDs.</param>
        /// <returns>A list of costume mix items.</returns>
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
                throw new Exception(ex.Message, ex);
            }

            return items;
        }

        /// <summary>
        /// Retrieves a list of item mix data from the database.
        /// </summary>
        /// <returns>A list of item mix data.</returns>
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
                throw new Exception(ex.Message, ex);
            }

            return items;
        }

        /// <summary>
        /// Retrieves a list of category items from the database based on the specified item type and whether it is a subcategory.
        /// </summary>
        /// <param name="itemType">The type of item.</param>
        /// <param name="isSubCategory">Indicates whether the items are subcategories.</param>
        /// <returns>A list of category items.</returns>
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
                throw new Exception(ex.Message, ex);
            }

            return categoryItems;
        }

        /// <summary>
        /// Retrieves a list of subcategory items from the database.
        /// </summary>
        /// <returns>A list of subcategory items.</returns>
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

        /// <summary>
        /// Retrieves a list of unique items from the database based on the specified query and type.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="isInt">Indicates whether the items are integers.</param>
        /// <param name="type">The type of items to retrieve.</param>
        /// <returns>A list of unique items.</returns>
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

        /// <summary>
        /// Retrieves a list of unique items from the database based on the specified query.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="isInt">Indicates whether the items are integers.</param>
        /// <returns>A list of unique items.</returns>
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
                throw new Exception(ex.Message, ex);
            }

            return items;
        }

        /// <summary>
        /// Retrieves a list of item mix group items from the database.
        /// </summary>
        /// <returns>A list of item mix group items.</returns>
        public List<NameID> GetItemMixGroupItems()
        {
            return GetUniqueItems("SELECT szGroup FROM itemmix", false, "ItemMix");
        }

        /// <summary>
        /// Retrieves a list of costume mix group items from the database.
        /// </summary>
        /// <returns>A list of costume mix group items.</returns>
        public List<NameID> GetCostumeMixGroupItems()
        {
            return GetUniqueItems("SELECT nGroup FROM costumemix", true, "CostumeMix");
        }

        /// <summary>
        /// Retrieves a list of trade shop group items from the database.
        /// </summary>
        /// <returns>A list of trade shop group items.</returns>
        public List<NameID> GetTradeShopGroupItems()
        {
            return GetUniqueItems("SELECT nGroupID FROM tradeshop", true, "TradeShop");
        }

        /// <summary>
        /// Retrieves a list of NPC shop items and trade shop group items from the database.
        /// </summary>
        /// <returns>A list of NPC shop items and trade shop group items.</returns>
        public List<NameID> GetNpcShopsItems()
        {
            var npcShopItems = GetNpcShopItems();
            var tradeShopGroupItems = GetTradeShopGroupItems();

            var mergedItems = npcShopItems.Concat(tradeShopGroupItems).ToList();

            mergedItems = [.. mergedItems.OrderBy(item => item.ID)];

            return mergedItems;
        }

        /// <summary>
        /// Retrieves a string value from the database based on the specified query and parameters.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">The parameters for the query.</param>
        /// <returns>The string value.</returns>
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
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Retrieves a string from the database based on the specified string ID.
        /// </summary>
        /// <param name="stringID">The string ID.</param>
        /// <returns>The string value.</returns>
        public string GetString(int stringID)
        {
            string query = "SELECT wszString FROM string WHERE nID = @stringID";
            return GetStringValueFromQuery(query, ("@stringID", stringID));
        }

        /// <summary>
        /// Retrieves the category name from the database based on the specified category ID.
        /// </summary>
        /// <param name="categoryID">The category ID.</param>
        /// <returns>The category name.</returns>
        public string GetCategoryName(int categoryID)
        {
            string query = "SELECT wszName00 FROM itemcategory WHERE nID = @categoryID";
            return GetStringValueFromQuery(query, ("@categoryID", categoryID));
        }

        /// <summary>
        /// Retrieves the subcategory name from the database based on the specified category ID.
        /// </summary>
        /// <param name="categoryID">The category ID.</param>
        /// <returns>The subcategory name.</returns>
        public string GetSubCategoryName(int categoryID)
        {
            string query = "SELECT wszName01 FROM itemcategory WHERE nID = @categoryID";
            return GetStringValueFromQuery(query, ("@categoryID", categoryID));
        }

        /// <summary>
        /// Retrieves the secondary subcategory name from the database based on the specified category ID.
        /// </summary>
        /// <param name="categoryID">The category ID.</param>
        /// <returns>The secondary subcategory name.</returns>
        public string GetSubCategory02Name(int categoryID)
        {
            string query = "SELECT wszName02 FROM itemcategory WHERE nID = @categoryID";
            return GetStringValueFromQuery(query, ("@categoryID", categoryID));
        }

        /// <summary>
        /// Retrieves the set name from the database based on the specified set ID.
        /// </summary>
        /// <param name="setId">The set ID.</param>
        /// <returns>The set name.</returns>
        public string GetSetName(int setId)
        {
            string query = "SELECT wszName FROM setitem_string WHERE nID = @setId";
            return GetStringValueFromQuery(query, ("@setId", setId));
        }

        /// <summary>
        /// Retrieves the option name from the database based on the specified option ID.
        /// </summary>
        /// <param name="optionID">The option ID.</param>
        /// <returns>The option name.</returns>
        public string GetOptionName(int optionID)
        {
            string query = "SELECT wszDesc FROM itemoptionlist WHERE nID = @optionID";
            return GetStringValueFromQuery(query, ("@optionID", optionID));
        }

        /// <summary>
        /// Retrieves the option group name from the database based on the specified option ID.
        /// </summary>
        /// <param name="optionID">The option ID.</param>
        /// <returns>The option group name.</returns>
        public string GetOptionGroupName(int optionID)
        {
            string query = "SELECT wszDesc FROM new_itemoptioncondition_string WHERE nID = @optionID";
            return GetStringValueFromQuery(query, ("@optionID", optionID));
        }

        /// <summary>
        /// Retrieves the fortune description from the database based on the specified fortune ID.
        /// </summary>
        /// <param name="fortuneID">The fortune ID.</param>
        /// <returns>The fortune description.</returns>
        public string GetFortuneDesc(int fortuneID)
        {
            string query = "SELECT wszDesc FROM fortune WHERE nID = @fortuneID";
            return GetStringValueFromQuery(query, ("@fortuneID", fortuneID));
        }

        /// <summary>
        /// Retrieves the title name from the database based on the specified title ID.
        /// </summary>
        /// <param name="titleID">The title ID.</param>
        /// <returns>The title name.</returns>
        public string GetTitleName(int titleID)
        {
            string query = "SELECT wszTitleName FROM charactertitle_string WHERE nID = @titleID";
            return GetStringValueFromQuery(query, ("@titleID", titleID));
        }

        /// <summary>
        /// Retrieves the add effect name from the database based on the specified effect ID.
        /// </summary>
        /// <param name="effectID">The effect ID.</param>
        /// <returns>The add effect name.</returns>
        public string GetAddEffectName(int effectID)
        {
            string query = "SELECT wszDescription FROM addeffect_string WHERE nID = @optionID";
            return GetStringValueFromQuery(query, ("@optionID", effectID));
        }

        /// <summary>
        /// Retrieves an integer value from the database based on the specified query and parameters.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">The parameters for the query.</param>
        /// <returns>The integer value.</returns>
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
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Retrieves the title category from the database based on the specified title ID.
        /// </summary>
        /// <param name="titleID">The title ID.</param>
        /// <returns>The title category.</returns>
        public int GetTitleCategory(int titleID)
        {
            string query = "SELECT nTitleCategory FROM charactertitle WHERE nID = @titleID";
            return GetIntValueFromQuery(query, ("@titleID", titleID));
        }

        /// <summary>
        /// Retrieves the title remain time from the database based on the specified title ID.
        /// </summary>
        /// <param name="titleID">The title ID.</param>
        /// <returns>The title remain time.</returns>
        public int GetTitleRemainTime(int titleID)
        {
            string query = "SELECT nRemainTime FROM charactertitle WHERE nID = @titleID";
            return GetIntValueFromQuery(query, ("@titleID", titleID));
        }

        /// <summary>
        /// Retrieves the option values from the database based on the specified option ID.
        /// </summary>
        /// <param name="optionID">The option ID.</param>
        /// <returns>A tuple containing the option values.</returns>
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
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Retrieves the weapon stats from the database based on the specified job class and weapon ID.
        /// </summary>
        /// <param name="jbClass">The job class.</param>
        /// <param name="weaponID">The weapon ID.</param>
        /// <returns>A tuple containing the weapon stats.</returns>
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
                throw new Exception(ex.Message, ex);
            }

        }

        /// <summary>
        /// Retrieves the fortune values from the database based on the specified fortune ID.
        /// </summary>
        /// <param name="fortuneID">The fortune ID.</param>
        /// <returns>A tuple containing the fortune values.</returns>
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
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Checks if the specified character name is in the nick filter.
        /// </summary>
        /// <param name="characterName">The character name.</param>
        /// <returns>True if the character name is in the nick filter, otherwise false.</returns>
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
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Retrieves the experience from the database based on the specified level.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>The experience value.</returns>
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
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Retrieves the title information from the database based on the specified title ID.
        /// </summary>
        /// <param name="titleID">The title ID.</param>
        /// <returns>A tuple containing the title information.</returns>
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
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Retrieves the set information from the database based on the specified set ID.
        /// </summary>
        /// <param name="setID">The set ID.</param>
        /// <returns>A tuple containing the set information.</returns>
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
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Retrieves the weapon enhance value from the database based on the specified enhance value.
        /// </summary>
        /// <param name="enhanceValue">The enhance value.</param>
        /// <returns>A tuple containing the weapon enhance value.</returns>
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
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Retrieves the armor enhance value from the database based on the specified enhance value.
        /// </summary>
        /// <param name="enhanceValue">The enhance value.</param>
        /// <returns>A tuple containing the armor enhance value.</returns>
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
                throw new Exception(ex.Message, ex);
            }
        }

        #region Skills

        /// <summary>
        /// Formats the previous skill information based on the specified skill type and skill ID.
        /// </summary>
        /// <param name="skillType">The skill type.</param>
        /// <param name="skillID">The skill ID.</param>
        /// <returns>The formatted previous skill information.</returns>
        public string FormatPreviousSkill(SkillType skillType, int skillID)
        {
            try
            {
                (int beforeSkillID00, int beforeSkillLevel00, int beforeSkillID01, int beforeSkillLevel01, int beforeSkillID02, int beforeSkillLevel02) = GetSkillTreeValues(skillType, skillID);

                if (beforeSkillID00 == 0 && beforeSkillID01 == 0 && beforeSkillID02 == 0)
                    return string.Empty;

                string skillName01 = GetSkillName(skillType, beforeSkillID00, beforeSkillLevel00);
                string skillName02 = GetSkillName(skillType, beforeSkillID01, beforeSkillLevel01);
                string skillName03 = GetSkillName(skillType, beforeSkillID02, beforeSkillLevel02);

                string skillName = $"<{Resources.RequiredLearnedSkills}>\n";
                if (beforeSkillID00 != 0)
                    skillName += $"Lv. {beforeSkillLevel00} {skillName01}\n";
                if (beforeSkillID01 != 0)
                    skillName += $"Lv. {beforeSkillLevel01} {skillName02}\n";
                if (beforeSkillID02 != 0)
                    skillName += $"Lv. {beforeSkillLevel02} {skillName03}\n";

                return skillName;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

        }

        /// <summary>
        /// Retrieves the skill tree values from the database based on the specified skill type and skill ID.
        /// </summary>
        /// <param name="characterSkillType">The character skill type.</param>
        /// <param name="skillID">The skill ID.</param>
        /// <returns>A tuple containing the skill tree values.</returns>
        public (int beforeSkillID00, int beforeSkillLevel00, int beforeSkillID01, int beforeSkillLevel01, int beforeSkillID02, int beforeSkillLevel02) GetSkillTreeValues(SkillType characterSkillType, int skillID)
        {
            string tableName = characterSkillType switch
            {
                SkillType.SkillFrantz => "frantzskilltree",
                SkillType.SkillAngela => "angelaskilltree",
                SkillType.SkillTude => "tudeskilltree",
                SkillType.SkillNatasha => "natashaskilltree",
                _ => throw new Exception($"Invalid class: {characterSkillType}")
            };

            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                string query = $"SELECT nBeforeSkillID00, nBeforeSkillLevel00, nBeforeSkillID01, nBeforeSkillLevel01, nBeforeSkillID02, nBeforeSkillLevel02 FROM {tableName} WHERE nSkillID = @SkillID";
                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection, ("@SkillID", skillID));

                return command.Read() ?
                (command.GetInt32(0),
                command.GetInt32(1),
                command.GetInt32(2),
                command.GetInt32(3),
                command.GetInt32(4),
                command.GetInt32(5)) :
                (0, 0, 0, 0, 0, 0);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

        }

        /// <summary>
        /// Retrieves the skill name from the database based on the specified skill type, skill ID, and skill level.
        /// </summary>
        /// <param name="characterSkillType">The character skill type.</param>
        /// <param name="skillID">The skill ID.</param>
        /// <param name="skillLevel">The skill level.</param>
        /// <returns>The skill name.</returns>
        public string GetSkillName(SkillType characterSkillType, int skillID, int skillLevel)
        {
            string tableName = characterSkillType switch
            {
                SkillType.SkillFrantz => "frantzskill",
                SkillType.SkillAngela => "angelaskill",
                SkillType.SkillTude => "tudeskill",
                SkillType.SkillNatasha => "natashaskill",
                _ => throw new Exception($"Invalid class: {characterSkillType}")
            };

            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();
            try
            {
                int id = GetSkillID(tableName, skillID, skillLevel);

                string query = $"SELECT wszName FROM {tableName}_string WHERE nID = @SkillID";
                using var command = _sqLiteDatabaseService.ExecuteReader(query, connection, ("@SkillID", id));

                return command.Read() ?
                command.GetString(0) :
                string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

        }

        /// <summary>
        /// Retrieves the skill ID from the database based on the specified table name, skill ID, and skill level.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="skillID">The skill ID.</param>
        /// <param name="skillLevel">The skill level.</param>
        /// <returns>The skill ID.</returns>
        private int GetSkillID(string tableName, int skillID, int skillLevel)
        {
            using var connection = _sqLiteDatabaseService.OpenSQLiteConnection();

            string query = $"SELECT nID FROM {tableName} WHERE nSkillID = @SkillID AND nSkillLevel = @SkillLevel";
            using var command = _sqLiteDatabaseService.ExecuteReader(query, connection, ("@SkillID", skillID), ("@SkillLevel", skillLevel));

            return command.Read() ? command.GetInt32(0) : 0;
        }

        #endregion
    }
}