using Microsoft.Data.SqlClient;
using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
using RHToolkit.Utilities;
using System.Data;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Services
{
    /// <summary>
    /// Provides database services for database management.
    /// </summary>
    public class DatabaseService(ISqlDatabaseService databaseService, IGMDatabaseService gmDatabaseService) : IDatabaseService
    {
        private readonly ISqlDatabaseService _sqlDatabaseService = databaseService;
        private readonly IGMDatabaseService _gmDatabaseService = gmDatabaseService;

        #region RustyHearts

        #region Character

        #region Write
        /// <summary>
        /// Updates character data.
        /// </summary>
        /// <param name="characterData">The new character data.</param>
        public async Task UpdateCharacterDataAsync(NewCharacterData characterData)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _sqlDatabaseService.ExecuteNonQueryAsync(
                    "UPDATE CharacterTable SET Level = @level, Experience = @experience, SP = @sp, Total_SP = @total_sp, LobbyID = @lobbyID, gold = @gold, Hearts = @hearts, Block_YN = @block_YN, storage_gold = @storage_gold, storage_count = @storage_count, IsTradeEnable = @isTradeEnable, Permission = @permission, GuildPoint = @guild_point, IsMoveEnable = @isMoveEnable WHERE character_id = @character_id",
                    connection,
                    transaction,
                    ("@character_id", characterData.CharacterID),
                    ("@level", characterData.Level),
                    ("@experience", characterData.Experience),
                    ("@sp", characterData.SP),
                    ("@total_sp", characterData.TotalSP),
                    ("@lobbyID", characterData.LobbyID),
                    ("@gold", characterData.Gold),
                    ("@hearts", characterData.Hearts),
                    ("@block_YN", characterData.BlockYN),
                    ("@storage_gold", characterData.StorageGold),
                    ("@storage_count", characterData.StorageCount),
                    ("@isTradeEnable", characterData.IsTradeEnable),
                    ("@permission", characterData.Permission),
                    ("@guild_point", characterData.GuildPoint),
                    ("@isMoveEnable", characterData.IsMoveEnable)
                );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Updates the character name.
        /// </summary>
        /// <param name="characterId">The character ID.</param>
        /// <param name="characterName">The new character name.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> UpdateCharacterNameAsync(Guid characterId, string characterName)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            try
            {
                return (int)await _sqlDatabaseService.ExecuteProcedureAsync(
                    "up_update_character_name",
                    connection,
                    null,
                    ("@character_id", characterId),
                    ("@character_name", characterName)
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Deletes a character.
        /// </summary>
        /// <param name="authId">The authentication ID.</param>
        /// <param name="characterId">The character ID.</param>
        public async Task DeleteCharacterAsync(Guid authId, Guid characterId)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _sqlDatabaseService.ExecuteProcedureAsync(
                     "up_delete_character",
                     connection,
                transaction,
                ("@auth_id", authId),
                ("@character_id", characterId)
                 );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Restores a deleted character.
        /// </summary>
        /// <param name="characterId">The character ID.</param>
        public async Task RestoreCharacterAsync(Guid characterId)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _sqlDatabaseService.ExecuteNonQueryAsync(
                   "INSERT into CharacterTable select * FROM CharacterTable_DELETE WHERE character_id = @character_id",
                   connection,
                   transaction,
                   ("@character_id", characterId)
               );
                await _sqlDatabaseService.ExecuteNonQueryAsync(
                   "DELETE FROM CharacterTable_DELETE WHERE character_id = @character_id",
                   connection,
                   transaction,
                   ("@character_id", characterId)
               );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Updates the character class and resets skills.
        /// </summary>
        /// <param name="characterData">The character data.</param>
        /// <param name="newCharacterClass">The new character class.</param>
        public async Task UpdateCharacterClassAsync(CharacterData characterData, int newCharacterClass)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            try
            {
                using (var skillTransaction = connection.BeginTransaction())
                {
                    // Update character class
                    await _sqlDatabaseService.ExecuteNonQueryAsync(
                        "UPDATE CharacterTable SET Class = @class WHERE character_id = @character_id",
                        connection,
                        skillTransaction,
                        ("@character_id", characterData.CharacterID),
                        ("@class", newCharacterClass)
                    );

                    // Reset skills
                    await _sqlDatabaseService.ExecuteProcedureAsync(
                        "up_skill_reset",
                        connection,
                        skillTransaction,
                        ("@character_id", characterData.CharacterID)
                    );

                    // Remove skills from quickslot - optimize by batching all updates into a single query
                    List<string> setColumns = [];
                    for (int i = 1; i <= 26; i++)
                    {
                        setColumns.Add($"type_{i:D2} = 0");
                        setColumns.Add($"item_id_{i:D2} = @emptyGuid");
                    }

                    await _sqlDatabaseService.ExecuteNonQueryAsync(
                        $"UPDATE QuickSlotExTable SET {string.Join(", ", setColumns)} WHERE character_id = @character_id",
                        connection,
                        skillTransaction,
                        ("@character_id", characterData.CharacterID),
                        ("@emptyGuid", Guid.Empty)
                    );

                    skillTransaction.Commit();
                }

                // mail message

                // Fetch equipped items
                ObservableCollection<ItemData> equipItems = await GetItemList(characterData.CharacterID, "N_EquipItem");

                // Filter items with SlotIndex 0 (weapon) and 10 to 19 (costumes)
                List<ItemData> filteredItems = equipItems
                    .Where(item => item.SlotIndex == 0 || (item.SlotIndex >= 10 && item.SlotIndex <= 19))
                    .ToList();

                Guid senderId = Guid.Empty; // GM Guid
                string message = $"{Resources.MailClassChangeMessage}<br><br><right>{DateTime.Now:yyyy-MM-dd HH:mm}";

                // Check if there are items to process
                if (filteredItems.Count == 0)
                {
                    using var mailTransaction = connection.BeginTransaction();
                    try
                    {
                        Guid mailId = Guid.NewGuid();
                        await InsertMailAsync(connection, mailTransaction, senderId, senderId, Resources.MailSenderGM, characterData.CharacterName!, message, 0, 7, 0, mailId, 0);
                        mailTransaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        mailTransaction.Rollback();
                        throw new Exception(ex.Message, ex);
                    }
                }
                else
                {
                    // Move weapon/costumes from EquipItem to mail
                    // Loop through filtered items in batches of 3
                    for (int i = 0; i < filteredItems.Count; i += 3)
                    {
                        using var itemTransaction = connection.BeginTransaction();
                        try
                        {
                            Guid mailId = Guid.NewGuid();
                            await InsertMailAsync(connection, itemTransaction, senderId, senderId, Resources.MailSenderGM, characterData.CharacterName!, message, 0, 7, 0, mailId, 0);

                            for (int j = 0; j < 3 && i + j < filteredItems.Count; j++)
                            {
                                ItemData item = filteredItems[i + j];
                                await InsertMailItemAsync(connection, itemTransaction, item, characterData.AuthID, characterData.CharacterID, mailId, j);

                                await _sqlDatabaseService.ExecuteNonQueryAsync(
                                    "DELETE FROM N_EquipItem WHERE item_uid = @item_uid",
                                    connection,
                                    itemTransaction,
                                    ("@item_uid", item.ItemUid)
                                );
                            }

                            itemTransaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            itemTransaction.Rollback();
                            throw new Exception(ex.Message, ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Updates the character job and resets skills.
        /// </summary>
        /// <param name="characterData">The character data.</param>
        /// <param name="newCharacterJob">The new character job.</param>
        public async Task UpdateCharacterJobAsync(CharacterData characterData, int newCharacterJob)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                // Update character job
                await _sqlDatabaseService.ExecuteNonQueryAsync(
                    "UPDATE CharacterTable SET Job = @job WHERE character_id = @character_id",
                    connection,
                    transaction,
                    ("@character_id", characterData.CharacterID),
                    ("@job", newCharacterJob)
                );

                // Reset skills
                await _sqlDatabaseService.ExecuteProcedureAsync(
                    "up_skill_reset",
                    connection,
                    transaction,
                    ("@character_id", characterData.CharacterID)
                );

                // Remove skills from quickslot - optimize by batching all updates into a single query
                List<string> setColumns = [];
                for (int i = 1; i <= 26; i++)
                {
                    setColumns.Add($"type_{i:D2} = 0");
                    setColumns.Add($"item_id_{i:D2} = @emptyGuid");
                }

                await _sqlDatabaseService.ExecuteNonQueryAsync(
                    $"UPDATE QuickSlotExTable SET {string.Join(", ", setColumns)} WHERE character_id = @character_id",
                    connection,
                    transaction,
                    ("@character_id", characterData.CharacterID),
                    ("@emptyGuid", Guid.Empty)
                );

                // Prepare mail message
                Guid senderId = Guid.Empty;

                string message = $"{Resources.MailFocusChangeMessage}<br><br><right>{DateTime.Now:yyyy-MM-dd HH:mm}";

                await InsertMailAsync(connection, transaction, senderId, senderId, Resources.MailSenderGM, characterData.CharacterName!, message, 0, 7, 0, Guid.NewGuid(), 0);

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        #endregion

        #region Read

        /// <summary>
        /// Retrieves character data by character identifier.
        /// </summary>
        /// <param name="characterIdentifier">The character identifier.</param>
        /// <returns>The character data.</returns>
        public async Task<CharacterData?> GetCharacterDataAsync(string characterIdentifier)
        {
            string selectQuery = "SELECT * FROM CharacterTable WHERE [Name] = @characterIdentifier";
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            DataTable dataTable;

            dataTable = await _sqlDatabaseService.ExecuteDataQueryAsync(selectQuery, connection, null, ("@characterIdentifier", characterIdentifier));

            if (dataTable.Rows.Count > 0)
            {
                DataRow row = dataTable.Rows[0];
                var characterData = new CharacterData
                {
                    CharacterID = (Guid)row["character_id"],
                    AccountName = (string)row["bcust_id"],
                    AuthID = (Guid)row["authid"],
                    Server = (int)row["server"],
                    CharacterName = (string)row["name"],
                    Class = (int)row["class"],
                    Job = (byte)row["job"],
                    Level = (int)row["level"],
                    Experience = (long)row["experience"],
                    SP = (int)row["sp"],
                    TotalSP = (int)row["total_sp"],
                    Fatigue = (int)row["fatigue"],
                    LobbyID = (int)row["lobbyid"],
                    Gold = (int)row["gold"],
                    CreateTime = (DateTime)row["createtime"],
                    LastLogin = (DateTime)row["lastlogin"],
                    Hearts = (int)row["hearts"],
                    BlockType = (byte)row["block_type"],
                    BlockYN = (string)row["block_yn"],
                    IsConnect = (string)row["isconnect"],
                    StorageGold = (int)row["storage_gold"],
                    StorageCount = (int)row["storage_count"],
                    IsTradeEnable = (string)row["istradeenable"],
                    Permission = (short)row["permission"],
                    GuildPoint = (int)row["guildpoint"],
                    IsMoveEnable = (string)row["ismoveenable"]
                };

                if (row["guildid"] != DBNull.Value)
                {
                    characterData.GuildName = await GetGuildNameAsync((Guid)row["guildid"]);
                    characterData.HasGuild = true;
                }
                else
                {
                    characterData.GuildName = Resources.NoGuild;
                    characterData.HasGuild = false;
                }

                return characterData;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves a list of character data by character identifier.
        /// </summary>
        /// <param name="characterIdentifier">The character identifier.</param>
        /// <param name="isConnect">The connection status.</param>
        /// <param name="isDeletedCharacter">Indicates if the character is deleted.</param>
        /// <returns>A list of character data.</returns>
        public async Task<List<CharacterData>> GetCharacterDataListAsync(string characterIdentifier, string isConnect = "", bool isDeletedCharacter = false)
        {
            string tableName = isDeletedCharacter ? "CharacterTable_DELETE" : "CharacterTable";

            string selectQuery = $"SELECT * FROM {tableName} WHERE ([bcust_id] = @characterIdentifier OR [name] = @characterIdentifier)";

            if (!string.IsNullOrEmpty(isConnect))
            {
                selectQuery += " AND [IsConnect] = @isConnect";
            }

            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            DataTable dataTable = await _sqlDatabaseService.ExecuteDataQueryAsync(
                selectQuery,
                connection,
                null,
                ("@characterIdentifier", characterIdentifier),
                ("@isConnect", isConnect)
            );

            List<CharacterData> characterDataList = [];

            // Collect unique guild IDs to avoid N+1 query problem
            HashSet<Guid> uniqueGuildIds = [];
            foreach (DataRow row in dataTable.Rows)
            {
                if (row["guildid"] != DBNull.Value)
                {
                    uniqueGuildIds.Add((Guid)row["guildid"]);
                }
            }

            // Fetch all guild names in a single query
            Dictionary<Guid, string> guildNameCache = [];
            if (uniqueGuildIds.Count > 0)
            {
                guildNameCache = await GetGuildNamesBatchAsync([.. uniqueGuildIds]);
            }

            foreach (DataRow row in dataTable.Rows)
            {
                var characterData = new CharacterData
                {
                    CharacterID = (Guid)row["character_id"],
                    AccountName = (string)row["bcust_id"],
                    AuthID = (Guid)row["authid"],
                    Server = (int)row["server"],
                    CharacterName = (string)row["name"],
                    Class = (int)row["class"],
                    Job = (byte)row["job"],
                    Level = (int)row["level"],
                    Experience = (long)row["experience"],
                    SP = (int)row["sp"],
                    TotalSP = (int)row["total_sp"],
                    Fatigue = (int)row["fatigue"],
                    LobbyID = (int)row["lobbyid"],
                    Gold = (int)row["gold"],
                    CreateTime = (DateTime)row["createtime"],
                    LastLogin = (DateTime)row["lastlogin"],
                    Hearts = (int)row["hearts"],
                    BlockType = (byte)row["block_type"],
                    BlockYN = (string)row["block_yn"],
                    IsConnect = (string)row["isconnect"],
                    StorageGold = (int)row["storage_gold"],
                    StorageCount = (int)row["storage_count"],
                    IsTradeEnable = (string)row["istradeenable"],
                    Permission = (short)row["permission"],
                    GuildPoint = (int)row["guildpoint"],
                    IsMoveEnable = (string)row["ismoveenable"]
                };

                if (row["guildid"] != DBNull.Value)
                {
                    Guid guildId = (Guid)row["guildid"];
                    characterData.GuildName = guildNameCache.TryGetValue(guildId, out string? guildName) ? guildName : Resources.NoGuild;
                    characterData.HasGuild = true;
                }
                else
                {
                    characterData.GuildName = Resources.NoGuild;
                    characterData.HasGuild = false;
                }

                characterDataList.Add(characterData);
            }

            return characterDataList;
        }

        /// <summary>
        /// Checks if a character is online.
        /// </summary>
        /// <param name="characterName">The character name.</param>
        /// <returns>True if the character is online, otherwise false.</returns>
        public async Task<bool> GetCharacterOnlineAsync(string characterName)
        {
            string selectQuery = "SELECT IsConnect FROM CharacterTable WHERE name = @characterName";
            var parameters = new (string, object)[] { ("@characterName", characterName) };

            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            object? result = await _sqlDatabaseService.ExecuteScalarAsync(selectQuery, connection, parameters);

            if (result != null && result != DBNull.Value)
            {
                return result.ToString() == "Y";
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a character is online and shows a message box if true.
        /// </summary>
        /// <param name="characterName">The character name.</param>
        /// <returns>True if the character is online, otherwise false.</returns>
        public async Task<bool> IsCharacterOnlineAsync(string characterName)
        {
            if (await GetCharacterOnlineAsync(characterName))
            {
                RHMessageBoxHelper.ShowOKMessage(string.Format(Resources.IsOnlineMessage, characterName), Resources.Information);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves all character names.
        /// </summary>
        /// <param name="isConnect">The connection status.</param>
        /// <returns>An array of character names.</returns>
        public async Task<string[]> GetAllCharacterNamesAsync(string isConnect = "")
        {
            List<string> characterNames = [];

            string query = "SELECT name FROM CharacterTable WHERE ([Block_YN] = 'N')";

            if (!string.IsNullOrEmpty(isConnect))
            {
                query += " AND [IsConnect] = @isConnect";
            }

            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            DataTable dataTable = await _sqlDatabaseService.ExecuteDataQueryAsync(
                query,
                connection,
                null,
                ("@isConnect", isConnect)
            );

            foreach (DataRow row in dataTable.Rows)
            {
                string characterName = row["name"]?.ToString() ?? string.Empty;
                characterNames.Add(characterName);
            }

            return [.. characterNames];
        }

        /// <summary>
        /// Retrieves character information by character name.
        /// </summary>
        /// <param name="characterName">The character name.</param>
        /// <returns>A tuple containing character ID, auth ID, and account name.</returns>
        public async Task<(Guid? characterId, Guid? authid, string? accountName)> GetCharacterInfoAsync(string characterName)
        {
            string selectQuery = "SELECT character_id, authid, bcust_id FROM CharacterTable WHERE name = @characterName";
            var parameters = new (string, object)[] { ("@characterName", characterName) };

            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            DataTable dataTable = await _sqlDatabaseService.ExecuteDataQueryAsync(selectQuery, connection, null, parameters);

            if (dataTable.Rows.Count > 0)
            {
                DataRow row = dataTable.Rows[0];
                return (
                    (Guid)row["character_id"],
                    (Guid)row["authid"],
                    row["bcust_id"].ToString()
                );
            }
            else
            {
                return (null, null, string.Empty);
            }
        }

        #endregion

        #endregion

        #region AccountInfo

        /// <summary>
        /// Retrieves account information by auth ID.
        /// </summary>
        /// <param name="authId">The auth ID.</param>
        /// <returns>A DataRow containing account information.</returns>
        public async Task<DataRow?> GetUniAccountInfoAsync(Guid authId)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            string selectQuery = "SELECT AccountStorage_Count, AccountStorage_Gold FROM UniAccountInfo WHERE AuthID = @authId";

            DataTable? dataTable = await _sqlDatabaseService.ExecuteDataQueryAsync(selectQuery, connection, null, ("@authId", authId));

            return dataTable.Rows.Count > 0 ? dataTable.Rows[0] : null;
        }
        #endregion

        #region Item

        /// <summary>
        /// Retrieves a list of items for a character.
        /// </summary>
        /// <param name="characterId">The character ID.</param>
        /// <param name="tableName">The table name.</param>
        /// <returns>An ObservableCollection of ItemData.</returns>
        public async Task<ObservableCollection<ItemData>> GetItemList(Guid characterId, string tableName)
        {
            string selectQuery = tableName switch
            {
                "N_EquipItem" => $"SELECT * FROM N_EquipItem WHERE character_id = @character_id AND page_index = 0 AND slot_index >= 0;",
                "N_InventoryItem" => $"SELECT * FROM N_InventoryItem WHERE character_id = @character_id AND page_index BETWEEN 1 AND 6 AND slot_index >= 0;",
                "tbl_Personal_Storage" => $"SELECT * FROM N_InventoryItem WHERE character_id = @character_id AND page_index = 21 AND slot_index >= 0;",
                "tbl_Account_Storage" => $"SELECT * FROM tbl_Account_Storage WHERE auth_id = @character_id AND page_index = 3 AND slot_index >= 0;",
                _ => throw new ArgumentException($"Invalid Item table name {tableName}.")
            };

            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            DataTable dataTable = await _sqlDatabaseService.ExecuteDataQueryAsync(selectQuery, connection, null, ("@character_id", characterId));

            ObservableCollection<ItemData> itemList = [];

            foreach (DataRow row in dataTable.Rows)
            {
                ItemData item = new()
                {
                    ItemUid = (Guid)row["item_uid"],
                    CharacterId = row.Table.Columns.Contains("character_id") ? (Guid)row["character_id"] : Guid.Empty,
                    AuthId = (Guid)row["auth_id"],
                    PageIndex = (int)row["page_index"],
                    SlotIndex = (int)row["slot_index"],
                    ItemId = (int)row["code"],
                    ItemAmount = (int)row["use_cnt"],
                    RemainTime = (int)row["remain_time"],
                    CreateTime = (DateTime)row["create_time"],
                    UpdateTime = (DateTime)row["update_time"],
                    GCode = (int)row["gcode"],
                    Durability = (int)row["durability"],
                    EnhanceLevel = (int)row["enhance_level"],
                    Option1Code = (int)row["option_1_code"],
                    Option1Value = (int)row["option_1_value"],
                    Option2Code = (int)row["option_2_code"],
                    Option2Value = (int)row["option_2_value"],
                    Option3Code = (int)row["option_3_code"],
                    Option3Value = (int)row["option_3_value"],
                    OptionGroup = (int)row["option_group"],
                    Reconstruction = (int)row["ReconNum"],
                    ReconstructionMax = (byte)row["ReconState"],
                    SocketCount = (int)row["socket_count"],
                    Socket1Code = (int)row["socket_1_code"],
                    Socket1Value = (int)row["socket_1_value"],
                    Socket2Code = (int)row["socket_2_code"],
                    Socket2Value = (int)row["socket_2_value"],
                    Socket3Code = (int)row["socket_3_code"],
                    Socket3Value = (int)row["socket_3_value"],
                    ExpireTime = (int)row["expire_time"],
                    LockPassword = (string)row["lock_pwd"],
                    AugmentStone = (int)row["activity_value"],
                    LinkId = row.Table.Columns.Contains("link_id") ? (Guid)row["link_id"] : Guid.Empty,
                    IsSeizure = (byte)row["is_seizure"],
                    Socket1Color = (byte)row["socket_1_color"],
                    Socket2Color = (byte)row["socket_2_color"],
                    Socket3Color = (byte)row["socket_3_color"],
                    DefermentTime = row.Table.Columns.Contains("deferment_time") ? (int)row["deferment_time"] : 0,
                    Rank = (byte)row["rank"],
                    AcquireRoute = (byte)row["acquireroute"],
                    Physical = (int)row["physical"],
                    Magical = (int)row["magical"],
                    DurabilityMax = (int)row["durabilitymax"],
                    Weight = (int)row["weight"]
                };

                itemList.Add(item);
            }

            return itemList;
        }

        /// <summary>
        /// Saves inventory items for a character.
        /// </summary>
        /// <param name="characterData">The character data.</param>
        /// <param name="itemDataList">The list of items to save.</param>
        /// <param name="deletedItemDataList">The list of items to delete.</param>
        /// <param name="tableName">The table name.</param>
        public async Task SaveInventoryItem(CharacterData characterData, ObservableCollection<ItemData>? itemDataList, ObservableCollection<ItemData>? deletedItemDataList, string tableName)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                if (deletedItemDataList != null)
                {
                    foreach (var deletedItem in deletedItemDataList)
                    {
                        await DeleteInventoryItemAsync(connection, transaction, deletedItem, tableName);
                        await GMAuditAsync(characterData, "Delete Item", $"<font color=blue>Delete Item</font>]<br><font color=red>Item GUID:{{{deletedItem.ItemUid}}}<br>{characterData.CharacterName}, GUID:{{{characterData.CharacterID}}}<br></font>");
                    }
                }

                if (itemDataList != null)
                {
                    foreach (var item in itemDataList)
                    {
                        if (item.IsNewItem)
                        {
                            await InsertInventoryItemAsync(connection, transaction, item, tableName);
                            await GMAuditAsync(characterData, "Add Item", $"<font color=blue>Add Item</font>]<br><font color=red>Item GUID:{{{item.ItemUid}}} <br>{characterData.CharacterName}, GUID:{{{characterData.CharacterID}}}<br></font>");
                        }
                        else if (item.IsEditedItem)
                        {
                            await UpdateInventoryItemAsync(connection, transaction, item, tableName);
                            await GMAuditAsync(characterData, "Update Item", $"<font color=blue>Update Item</font>]<br><font color=red>Item GUID:{{{item.ItemUid}}}<br>{characterData.CharacterName}, GUID:{{{characterData.CharacterID}}}<br></font>");
                        }
                    }
                }

                transaction.Commit();

            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Inserts a new inventory item.
        /// </summary>
        /// <param name="connection">The SQL connection.</param>
        /// <param name="transaction">The SQL transaction.</param>
        /// <param name="itemData">The item data.</param>
        /// <param name="tableName">The table name.</param>
        public async Task InsertInventoryItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData, string tableName)
        {
            try
            {
                string columns = "item_uid, ";
                string values = "@item_uid, ";

                if (tableName != "tbl_Account_Storage")
                {
                    columns += "character_id, link_id, deferment_time, ";
                    values += "@character_id, @link_id, @deferment_time, ";
                }

                columns += "auth_id, page_index, slot_index, code, use_cnt, remain_time, create_time, update_time, gcode, durability, " +
                           "enhance_level, option_1_code, option_1_value, option_2_code, option_2_value, option_3_code, option_3_value, " +
                           "option_group, ReconNum, ReconState, socket_count, socket_1_code, socket_1_value, socket_2_code, socket_2_value, " +
                           "socket_3_code, socket_3_value, expire_time, lock_pwd, activity_value, is_seizure, socket_1_color, " +
                           "socket_2_color, socket_3_color, rank, acquireroute, physical, magical, durabilitymax, weight";

                values += "@auth_id, @page_index, @slot_index, @code, @use_cnt, @remain_time, @create_time, @update_time, @gcode, @durability, " +
                          "@enhance_level, @option_1_code, @option_1_value, @option_2_code, @option_2_value, @option_3_code, @option_3_value, " +
                          "@option_group, @ReconNum, @ReconState, @socket_count, @socket_1_code, @socket_1_value, @socket_2_code, @socket_2_value, " +
                          "@socket_3_code, @socket_3_value, @expire_time, @lock_pwd, @activity_value, @is_seizure, @socket_1_color, " +
                          "@socket_2_color, @socket_3_color, @rank, @acquireroute, @physical, @magical, @durabilitymax, @weight";

                var parameters = new List<(string, object)>
                {
                    ("@item_uid", itemData.ItemUid),
                    ("@auth_id", itemData.AuthId),
                    ("@page_index", itemData.PageIndex),
                    ("@slot_index", itemData.SlotIndex),
                    ("@code", itemData.ItemId),
                    ("@use_cnt", itemData.ItemAmount),
                    ("@remain_time", itemData.RemainTime),
                    ("@create_time", itemData.CreateTime),
                    ("@update_time", itemData.UpdateTime),
                    ("@gcode", itemData.GCode),
                    ("@durability", itemData.Durability),
                    ("@enhance_level", itemData.EnhanceLevel),
                    ("@option_1_code", itemData.Option1Code),
                    ("@option_1_value", itemData.Option1Value),
                    ("@option_2_code", itemData.Option2Code),
                    ("@option_2_value", itemData.Option2Value),
                    ("@option_3_code", itemData.Option3Code),
                    ("@option_3_value", itemData.Option3Value),
                    ("@option_group", itemData.OptionGroup),
                    ("@ReconNum", itemData.Reconstruction),
                    ("@ReconState", itemData.ReconstructionMax),
                    ("@socket_count", itemData.SocketCount),
                    ("@socket_1_code", itemData.Socket1Code),
                    ("@socket_1_value", itemData.Socket1Value),
                    ("@socket_2_code", itemData.Socket2Code),
                    ("@socket_2_value", itemData.Socket2Value),
                    ("@socket_3_code", itemData.Socket3Code),
                    ("@socket_3_value", itemData.Socket3Value),
                    ("@expire_time", itemData.ExpireTime),
                    ("@lock_pwd", itemData.LockPassword ?? string.Empty),
                    ("@activity_value", itemData.AugmentStone),
                    ("@is_seizure", itemData.IsSeizure),
                    ("@socket_1_color", itemData.Socket1Color),
                    ("@socket_2_color", itemData.Socket2Color),
                    ("@socket_3_color", itemData.Socket3Color),
                    ("@rank", itemData.Rank),
                    ("@acquireroute", itemData.AcquireRoute),
                    ("@physical", itemData.Physical),
                    ("@magical", itemData.Magical),
                    ("@durabilitymax", itemData.DurabilityMax),
                    ("@weight", itemData.Weight)
                };

                if (tableName != "tbl_Account_Storage")
                {
                    parameters.Add(("@character_id", itemData.CharacterId));
                    parameters.Add(("@link_id", itemData.LinkId));
                    parameters.Add(("@deferment_time", itemData.DefermentTime));
                }

                await _sqlDatabaseService.ExecuteNonQueryAsync(
                    $"INSERT INTO {tableName} ({columns}) VALUES ({values})",
                    connection,
                    transaction,
                    [.. parameters]
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Updates an existing inventory item.
        /// </summary>
        /// <param name="connection">The SQL connection.</param>
        /// <param name="transaction">The SQL transaction.</param>
        /// <param name="itemData">The item data.</param>
        /// <param name="tableName">The table name.</param>
        public async Task UpdateInventoryItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData, string tableName)
        {
            try
            {
                await _sqlDatabaseService.ExecuteNonQueryAsync(
                    $"UPDATE {tableName} SET " +
                    "page_index = @page_index, " +
                    "slot_index = @slot_index, " +
                    "use_cnt = @use_cnt, " +
                    "remain_time = @remain_time, " +
                    "update_time = @update_time, " +
                    "gcode = @gcode, " +
                    "durability = @durability, " +
                    "enhance_level = @enhance_level, " +
                    "option_1_code = @option_1_code, " +
                    "option_1_value = @option_1_value, " +
                    "option_2_code = @option_2_code, " +
                    "option_2_value = @option_2_value, " +
                    "option_3_code = @option_3_code, " +
                    "option_3_value = @option_3_value, " +
                    "option_group = @option_group, " +
                    "ReconNum = @ReconNum, " +
                    "ReconState = @ReconState, " +
                    "socket_count = @socket_count, " +
                    "socket_1_code = @socket_1_code, " +
                    "socket_1_value = @socket_1_value, " +
                    "socket_2_code = @socket_2_code, " +
                    "socket_2_value = @socket_2_value, " +
                    "socket_3_code = @socket_3_code, " +
                    "socket_3_value = @socket_3_value, " +
                    "expire_time = @expire_time, " +
                    "lock_pwd = @lock_pwd, " +
                    "activity_value = @activity_value, " +
                    "is_seizure = @is_seizure, " +
                    "socket_1_color = @socket_1_color, " +
                    "socket_2_color = @socket_2_color, " +
                    "socket_3_color = @socket_3_color, " +
                    "rank = @rank, " +
                    "acquireroute = @acquireroute, " +
                    "physical = @physical, " +
                    "magical = @magical, " +
                    "durabilitymax = @durabilitymax, " +
                    "weight = @weight " +
                    "WHERE item_uid = @item_uid",
                    connection,
                    transaction,
                    ("@item_uid", itemData.ItemUid),
                    ("@page_index", itemData.PageIndex),
                    ("@slot_index", itemData.SlotIndex),
                    ("@use_cnt", itemData.ItemAmount),
                    ("@remain_time", itemData.RemainTime),
                    ("@update_time", itemData.UpdateTime),
                    ("@gcode", itemData.GCode),
                    ("@durability", itemData.Durability),
                    ("@enhance_level", itemData.EnhanceLevel),
                    ("@option_1_code", itemData.Option1Code),
                    ("@option_1_value", itemData.Option1Value),
                    ("@option_2_code", itemData.Option2Code),
                    ("@option_2_value", itemData.Option2Value),
                    ("@option_3_code", itemData.Option3Code),
                    ("@option_3_value", itemData.Option3Value),
                    ("@option_group", itemData.OptionGroup),
                    ("@ReconNum", itemData.Reconstruction),
                    ("@ReconState", itemData.ReconstructionMax),
                    ("@socket_count", itemData.SocketCount),
                    ("@socket_1_code", itemData.Socket1Code),
                    ("@socket_1_value", itemData.Socket1Value),
                    ("@socket_2_code", itemData.Socket2Code),
                    ("@socket_2_value", itemData.Socket2Value),
                    ("@socket_3_code", itemData.Socket3Code),
                    ("@socket_3_value", itemData.Socket3Value),
                    ("@expire_time", itemData.ExpireTime),
                    ("@lock_pwd", itemData.LockPassword ?? string.Empty),
                    ("@activity_value", itemData.AugmentStone),
                    ("@is_seizure", itemData.IsSeizure),
                    ("@socket_1_color", itemData.Socket1Color),
                    ("@socket_2_color", itemData.Socket2Color),
                    ("@socket_3_color", itemData.Socket3Color),
                    ("@rank", itemData.Rank),
                    ("@acquireroute", itemData.AcquireRoute),
                    ("@physical", itemData.Physical),
                    ("@magical", itemData.Magical),
                    ("@durabilitymax", itemData.DurabilityMax),
                    ("@weight", itemData.Weight)
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Deletes an inventory item from the specified table.
        /// </summary>
        /// <param name="connection">The SQL connection.</param>
        /// <param name="transaction">The SQL transaction.</param>
        /// <param name="itemData">The item data to delete.</param>
        /// <param name="tableName">The name of the table from which to delete the item.</param>
        public async Task DeleteInventoryItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData, string tableName)
        {
            try
            {
                await _sqlDatabaseService.ExecuteNonQueryAsync(
                    $"DELETE FROM {tableName} WHERE [item_uid] = @item_uid",
                    connection,
                    transaction,
                    ("@item_uid", itemData.ItemUid)
                );

                await _sqlDatabaseService.ExecuteNonQueryAsync(
                    "DELETE FROM N_InventoryItem_DELETE WHERE [item_uid] = @item_uid AND [page_index] = @page_index",
                    connection,
                    transaction,
                    ("@item_uid", itemData.ItemUid),
                    ("@page_index", -25)
                );
                await InsertInventoryDeleteItemAsync(connection, transaction, itemData);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Inserts a deleted inventory item into the N_InventoryItem_DELETE table.
        /// </summary>
        /// <param name="connection">The SQL connection.</param>
        /// <param name="transaction">The SQL transaction.</param>
        /// <param name="itemData">The item data to insert.</param>
        public async Task InsertInventoryDeleteItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData)
        {
            try
            {
                await _sqlDatabaseService.ExecuteNonQueryAsync(
                    $"INSERT INTO N_InventoryItem_DELETE (" +
                    "item_uid, character_id, auth_id, page_index, slot_index, code, use_cnt, remain_time, " +
                    "create_time, update_time, gcode, durability, enhance_level, option_1_code, option_1_value, " +
                    "option_2_code, option_2_value, option_3_code, option_3_value, option_group, ReconNum, " +
                    "ReconState, socket_count, socket_1_code, socket_1_value, socket_2_code, socket_2_value, " +
                    "socket_3_code, socket_3_value, expire_time, lock_pwd, activity_value, link_id, is_seizure, " +
                    "socket_1_color, socket_2_color, socket_3_color, deferment_time, rank, acquireroute, physical, " +
                    "magical, durabilitymax, weight) " +
                    "VALUES (" +
                    "@item_uid, @character_id, @auth_id, @page_index, @slot_index, @code, @use_cnt, @remain_time, " +
                    "@create_time, @update_time, @gcode, @durability, @enhance_level, @option_1_code, @option_1_value, " +
                    "@option_2_code, @option_2_value, @option_3_code, @option_3_value, @option_group, @ReconNum, " +
                    "@ReconState, @socket_count, @socket_1_code, @socket_1_value, @socket_2_code, @socket_2_value, " +
                    "@socket_3_code, @socket_3_value, @expire_time, @lock_pwd, @activity_value, @link_id, @is_seizure, " +
                    "@socket_1_color, @socket_2_color, @socket_3_color, @deferment_time, @rank, @acquireroute, @physical, " +
                    "@magical, @durabilitymax, @weight)",
                    connection,
                    transaction,
                    ("@item_uid", itemData.ItemUid),
                    ("@character_id", itemData.CharacterId),
                    ("@auth_id", itemData.AuthId),
                    ("@page_index", -25),
                    ("@slot_index", itemData.SlotIndex),
                    ("@code", itemData.ItemId),
                    ("@use_cnt", itemData.ItemAmount),
                    ("@remain_time", itemData.RemainTime),
                    ("@create_time", itemData.CreateTime),
                    ("@update_time", DateTime.Now),
                    ("@gcode", itemData.GCode),
                    ("@durability", itemData.Durability),
                    ("@enhance_level", itemData.EnhanceLevel),
                    ("@option_1_code", itemData.Option1Code),
                    ("@option_1_value", itemData.Option1Value),
                    ("@option_2_code", itemData.Option2Code),
                    ("@option_2_value", itemData.Option2Value),
                    ("@option_3_code", itemData.Option3Code),
                    ("@option_3_value", itemData.Option3Value),
                    ("@option_group", itemData.OptionGroup),
                    ("@ReconNum", itemData.ReconstructionMax),
                    ("@ReconState", itemData.Reconstruction),
                    ("@socket_count", itemData.SocketCount),
                    ("@socket_1_code", itemData.Socket1Code),
                    ("@socket_1_value", itemData.Socket1Value),
                    ("@socket_2_code", itemData.Socket2Code),
                    ("@socket_2_value", itemData.Socket2Value),
                    ("@socket_3_code", itemData.Socket3Code),
                    ("@socket_3_value", itemData.Socket3Value),
                    ("@expire_time", itemData.ExpireTime),
                    ("@lock_pwd", itemData.LockPassword ?? string.Empty),
                    ("@activity_value", itemData.AugmentStone),
                    ("@link_id", itemData.LinkId),
                    ("@is_seizure", itemData.IsSeizure),
                    ("@socket_1_color", itemData.Socket1Color),
                    ("@socket_2_color", itemData.Socket2Color),
                    ("@socket_3_color", itemData.Socket3Color),
                    ("@deferment_time", itemData.DefermentTime),
                    ("@rank", itemData.Rank),
                    ("@acquireroute", itemData.AcquireRoute),
                    ("@physical", itemData.Physical),
                    ("@magical", itemData.Magical),
                    ("@durabilitymax", itemData.DurabilityMax),
                    ("@weight", itemData.Weight)
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Inserts a new inventory item for a pet.
        /// </summary>
        /// <param name="connection">The SQL connection.</param>
        /// <param name="transaction">The SQL transaction.</param>
        /// <param name="itemData">The item data to insert.</param>
        /// <param name="petId">The pet ID.</param>
        public async Task InsertPetInventoryItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData, Guid petId)
        {
            try
            {
                await _sqlDatabaseService.ExecuteNonQueryAsync(
                    "INSERT INTO tbl_Pet_Inventory (" +
                    "item_uid, pet_id, auth_id, page_index, slot_index, code, use_cnt, remain_time, " +
                    "create_time, update_time, gcode, durability, enhance_level, option_1_code, option_1_value, " +
                    "option_2_code, option_2_value, option_3_code, option_3_value, option_group, ReconNum, " +
                    "ReconState, socket_count, socket_1_code, socket_1_value, socket_2_code, socket_2_value, " +
                    "socket_3_code, socket_3_value, expire_time, lock_pwd, activity_value, is_seizure, " +
                    "socket_1_color, socket_2_color, socket_3_color, rank, acquireroute, physical, " +
                    "magical, durabilitymax, weight) " +
                    "VALUES (" +
                    "@item_uid, pet_id, @auth_id, @page_index, @slot_index, @code, @use_cnt, @remain_time, " +
                    "@create_time, @update_time, @gcode, @durability, @enhance_level, @option_1_code, @option_1_value, " +
                    "@option_2_code, @option_2_value, @option_3_code, @option_3_value, @option_group, @ReconNum, " +
                    "@ReconState, @socket_count, @socket_1_code, @socket_1_value, @socket_2_code, @socket_2_value, " +
                    "@socket_3_code, @socket_3_value, @expire_time, @lock_pwd, @activity_value, @is_seizure, " +
                    "@socket_1_color, @socket_2_color, @socket_3_color, @rank, @acquireroute, @physical, " +
                    "@magical, @durabilitymax, @weight)",
                    connection,
                    transaction,
                    ("@item_uid", itemData.ItemUid),
                    ("@pet_id", petId),
                    ("@auth_id", itemData.AuthId),
                    ("@page_index", itemData.PageIndex),
                    ("@slot_index", itemData.SlotIndex),
                    ("@code", itemData.ItemId),
                    ("@use_cnt", itemData.ItemAmount),
                    ("@remain_time", itemData.RemainTime),
                    ("@create_time", itemData.CreateTime),
                    ("@update_time", itemData.UpdateTime),
                    ("@gcode", itemData.GCode),
                    ("@durability", itemData.Durability),
                    ("@enhance_level", itemData.EnhanceLevel),
                    ("@option_1_code", itemData.Option1Code),
                    ("@option_1_value", itemData.Option1Value),
                    ("@option_2_code", itemData.Option2Code),
                    ("@option_2_value", itemData.Option2Value),
                    ("@option_3_code", itemData.Option3Code),
                    ("@option_3_value", itemData.Option3Value),
                    ("@option_group", itemData.OptionGroup),
                    ("@ReconNum", itemData.ReconstructionMax),
                    ("@ReconState", itemData.Reconstruction),
                    ("@socket_count", itemData.SocketCount),
                    ("@socket_1_code", itemData.Socket1Code),
                    ("@socket_1_value", itemData.Socket1Value),
                    ("@socket_2_code", itemData.Socket2Code),
                    ("@socket_2_value", itemData.Socket2Value),
                    ("@socket_3_code", itemData.Socket3Code),
                    ("@socket_3_value", itemData.Socket3Value),
                    ("@expire_time", itemData.ExpireTime),
                    ("@lock_pwd", itemData.LockPassword ?? string.Empty),
                    ("@activity_value", itemData.AugmentStone),
                    ("@is_seizure", itemData.IsSeizure),
                    ("@socket_1_color", itemData.Socket1Color),
                    ("@socket_2_color", itemData.Socket2Color),
                    ("@socket_3_color", itemData.Socket3Color),
                    ("@rank", itemData.Rank),
                    ("@acquireroute", itemData.AcquireRoute),
                    ("@physical", itemData.Physical),
                    ("@magical", itemData.Magical),
                    ("@durabilitymax", itemData.DurabilityMax),
                    ("@weight", itemData.Weight)
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Updates an existing inventory item for a pet.
        /// </summary>
        /// <param name="connection">The SQL connection.</param>
        /// <param name="transaction">The SQL transaction.</param>
        /// <param name="itemData">The item data to update.</param>
        public async Task UpdatePetInventoryItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData)
        {
            try
            {
                await _sqlDatabaseService.ExecuteNonQueryAsync(
                    $"UPDATE tbl_Pet_Inventory SET " +
                    "page_index = @page_index, " +
                    "slot_index = @slot_index, " +
                    "use_cnt = @use_cnt, " +
                    "remain_time = @remain_time, " +
                    "update_time = @update_time, " +
                    "gcode = @gcode, " +
                    "durability = @durability, " +
                    "enhance_level = @enhance_level, " +
                    "option_1_code = @option_1_code, " +
                    "option_1_value = @option_1_value, " +
                    "option_2_code = @option_2_code, " +
                    "option_2_value = @option_2_value, " +
                    "option_3_code = @option_3_code, " +
                    "option_3_value = @option_3_value, " +
                    "option_group = @option_group, " +
                    "ReconNum = @ReconNum, " +
                    "ReconState = @ReconState, " +
                    "socket_count = @socket_count, " +
                    "socket_1_code = @socket_1_code, " +
                    "socket_1_value = @socket_1_value, " +
                    "socket_2_code = @socket_2_code, " +
                    "socket_2_value = @socket_2_value, " +
                    "socket_3_code = @socket_3_code, " +
                    "socket_3_value = @socket_3_value, " +
                    "expire_time = @expire_time, " +
                    "lock_pwd = @lock_pwd, " +
                    "activity_value = @activity_value, " +
                    "is_seizure = @is_seizure, " +
                    "socket_1_color = @socket_1_color, " +
                    "socket_2_color = @socket_2_color, " +
                    "socket_3_color = @socket_3_color, " +
                    "rank = @rank, " +
                    "acquireroute = @acquireroute, " +
                    "physical = @physical, " +
                    "magical = @magical, " +
                    "durabilitymax = @durabilitymax, " +
                    "weight = @weight " +
                    "WHERE item_uid = @item_uid",
                    connection,
                    transaction,
                    ("@item_uid", itemData.ItemUid),
                    ("@page_index", itemData.PageIndex),
                    ("@slot_index", itemData.SlotIndex),
                    ("@use_cnt", itemData.ItemAmount),
                    ("@remain_time", itemData.RemainTime),
                    ("@update_time", itemData.UpdateTime),
                    ("@gcode", itemData.GCode),
                    ("@durability", itemData.Durability),
                    ("@enhance_level", itemData.EnhanceLevel),
                    ("@option_1_code", itemData.Option1Code),
                    ("@option_1_value", itemData.Option1Value),
                    ("@option_2_code", itemData.Option2Code),
                    ("@option_2_value", itemData.Option2Value),
                    ("@option_3_code", itemData.Option3Code),
                    ("@option_3_value", itemData.Option3Value),
                    ("@option_group", itemData.OptionGroup),
                    ("@ReconNum", itemData.ReconstructionMax),
                    ("@ReconState", itemData.Reconstruction),
                    ("@socket_count", itemData.SocketCount),
                    ("@socket_1_code", itemData.Socket1Code),
                    ("@socket_1_value", itemData.Socket1Value),
                    ("@socket_2_code", itemData.Socket2Code),
                    ("@socket_2_value", itemData.Socket2Value),
                    ("@socket_3_code", itemData.Socket3Code),
                    ("@socket_3_value", itemData.Socket3Value),
                    ("@expire_time", itemData.ExpireTime),
                    ("@lock_pwd", itemData.LockPassword ?? string.Empty),
                    ("@activity_value", itemData.AugmentStone),
                    ("@is_seizure", itemData.IsSeizure),
                    ("@socket_1_color", itemData.Socket1Color),
                    ("@socket_2_color", itemData.Socket2Color),
                    ("@socket_3_color", itemData.Socket3Color),
                    ("@rank", itemData.Rank),
                    ("@acquireroute", itemData.AcquireRoute),
                    ("@physical", itemData.Physical),
                    ("@magical", itemData.Magical),
                    ("@durabilitymax", itemData.DurabilityMax),
                    ("@weight", itemData.Weight)
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }
        #endregion

        #region Character Title

        /// <summary>
        /// Reads the list of character titles.
        /// </summary>
        /// <param name="characterId">The character ID.</param>
        /// <returns>A DataTable containing the character titles.</returns>
        public async Task<DataTable?> ReadCharacterTitleListAsync(Guid characterId)
        {
            DataTable dataTable = new();
            dataTable.Columns.Add("TitleUid", typeof(Guid));
            dataTable.Columns.Add("TitleId", typeof(int));
            dataTable.Columns.Add("ExpireTime", typeof(int));
            dataTable.Columns.Add("TitleType", typeof(string));
            dataTable.Columns.Add("TitleName", typeof(string));
            dataTable.Columns.Add("FormattedRemainTime", typeof(string));
            dataTable.Columns.Add("FormattedExpireTime", typeof(string));

            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            DataTable titleDataTable = await _sqlDatabaseService.ExecuteDataProcedureAsync("up_read_title_list", connection, null, ("@character_id", characterId));

            if (titleDataTable.Rows.Count > 0)
            {
                foreach (DataRow row in titleDataTable.Rows)
                {
                    Guid titleUid = (Guid)row["ID"];
                    int titleId = (int)row["title_code"];
                    int remainTime = (int)row["remain_time"];
                    int expireTime = (int)row["expire_time"];

                    int titleCategory = _gmDatabaseService.GetTitleCategory(titleId);
                    string formattedtitleCategory = titleCategory == 0 ? Resources.TitleNormal : Resources.TitleSpecial;
                    string titleName = _gmDatabaseService.GetTitleName(titleId);
                    string formattedTitleName = $"{titleName} ({titleId})";
                    string formattedRemainTime = DateTimeFormatter.FormatRemainTime(remainTime);
                    string formattedExpireTime = DateTimeFormatter.FormatExpireTime(expireTime);

                    dataTable.Rows.Add(titleUid, titleId, expireTime, formattedtitleCategory, formattedTitleName, formattedRemainTime, formattedExpireTime);
                }

                return dataTable;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Reads the equipped title of a character.
        /// </summary>
        /// <param name="characterId">The character ID.</param>
        /// <returns>A DataRow containing the equipped title.</returns>
        public async Task<DataRow?> ReadCharacterEquipTitleAsync(Guid characterId)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            DataTable dataTable = await _sqlDatabaseService.ExecuteDataProcedureAsync("up_read_equip_title", connection, null, ("@character_id", characterId));

            return dataTable.Rows.Count > 0 ? dataTable.Rows[0] : null;
        }

        /// <summary>
        /// Checks if a character has a specific title.
        /// </summary>
        /// <param name="characterId">The character ID.</param>
        /// <param name="titleID">The title ID.</param>
        /// <returns>True if the character has the title, otherwise false.</returns>
        public async Task<bool> CharacterHasTitle(Guid characterId, int titleID)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            object? result = await _sqlDatabaseService.ExecuteScalarAsync(
                "SELECT COUNT(*) FROM CharacterTitle WHERE [character_id] = @character_id AND [title_code] = @title_code",
                connection,
                ("@character_id", characterId),
                ("@title_code", titleID)
            );

            int titleCount = result != null ? (int)result : 0;

            return titleCount > 0;
        }

        /// <summary>
        /// Adds a title to a character.
        /// </summary>
        /// <param name="characterData">The character data.</param>
        /// <param name="titleId">The title ID.</param>
        /// <param name="remainTime">The remaining time for the title.</param>
        /// <param name="expireTime">The expiration time for the title.</param>
        public async Task AddCharacterTitleAsync(CharacterData characterData, int titleId, int remainTime, int expireTime)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _sqlDatabaseService.ExecuteProcedureAsync(
                     "up_add_character_title",
                     connection,
                     transaction,
                     ("@character_id", characterData.CharacterID),
                     ("@new_id", Guid.NewGuid()),
                     ("@title_code", titleId),
                     ("@remain_time", remainTime),
                     ("@expire_time", expireTime)
                 );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Equips a title for a character.
        /// </summary>
        /// <param name="characterData">The character data.</param>
        /// <param name="titleId">The title ID.</param>
        public async Task EquipCharacterTitleAsync(CharacterData characterData, Guid titleId)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _sqlDatabaseService.ExecuteProcedureAsync(
                     "up_equip_character_title",
                     connection,
                     transaction,
                     ("@character_id", characterData.CharacterID),
                     ("@title_id", titleId)
                 );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Unequips a title for a character.
        /// </summary>
        /// <param name="characterData">The character data.</param>
        /// <param name="titleId">The title ID.</param>
        public async Task UnequipCharacterTitleAsync(CharacterData characterData, Guid titleId)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _sqlDatabaseService.ExecuteProcedureAsync(
                     "up_unequip_character_title",
                     connection,
                     transaction,
                     ("@title_id", titleId)
                 );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Deletes a title from a character.
        /// </summary>
        /// <param name="characterData">The character data.</param>
        /// <param name="titleUid">The title UID.</param>
        public async Task DeleteCharacterTitleAsync(CharacterData characterData, Guid titleUid)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _sqlDatabaseService.ExecuteProcedureAsync(
                     "up_delete_character_title",
                     connection,
                     transaction,
                     ("@character_id", characterData.CharacterID),
                     ("@del_title_id", titleUid)
                 );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        #endregion

        #region Fortune

        /// <summary>
        /// Reads the fortune of a character.
        /// </summary>
        /// <param name="characterId">The character ID.</param>
        /// <returns>A DataRow containing the fortune data.</returns>
        public async Task<DataRow?> ReadCharacterFortuneAsync(Guid characterId)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            DataTable dataTable = await _sqlDatabaseService.ExecuteDataProcedureAsync("up_read_fortune", connection, null, ("@character_id", characterId));

            return dataTable.Rows.Count > 0 ? dataTable.Rows[0] : null;
        }

        /// <summary>
        /// Updates the fortune of a character.
        /// </summary>
        /// <param name="characterData">The character data.</param>
        /// <param name="fortune">The fortune value.</param>
        /// <param name="selectedFortuneID1">The first selected fortune ID.</param>
        /// <param name="selectedFortuneID2">The second selected fortune ID.</param>
        /// <param name="selectedFortuneID3">The third selected fortune ID.</param>
        public async Task UpdateFortuneAsync(CharacterData characterData, int fortune, int selectedFortuneID1, int selectedFortuneID2, int selectedFortuneID3)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _sqlDatabaseService.ExecuteProcedureAsync(
                     "up_update_fortune",
                     connection,
                     transaction,
                     ("@character_id", characterData.CharacterID),
                     ("@fortune", fortune),
                     ("@type_1", selectedFortuneID1),
                     ("@type_2", selectedFortuneID2),
                     ("@type_3", selectedFortuneID3),
                     ("@count", 1)
                 );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Removes the fortune of a character.
        /// </summary>
        /// <param name="characterData">The character data.</param>
        /// <param name="fortuneState">The fortune state.</param>
        /// <param name="fortuneID1">The first fortune ID.</param>
        /// <param name="fortuneID2">The second fortune ID.</param>
        /// <param name="fortuneID3">The third fortune ID.</param>
        public async Task RemoveFortuneAsync(CharacterData characterData, int fortuneState, int fortuneID1, int fortuneID2, int fortuneID3)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _sqlDatabaseService.ExecuteNonQueryAsync(
                "DELETE FROM FortuneTable WHERE character_id = @character_id",
                connection,
                transaction,
                ("@character_id", characterData.CharacterID)
                );

                await _sqlDatabaseService.ExecuteProcedureAsync(
                     "up_update_character_fortune",
                     connection,
                     transaction,
                     ("@character_id", characterData.CharacterID),
                     ("@fortune", fortuneState)
                 );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        #endregion

        #region Sanction

        /// <summary>
        /// Applies a sanction to a character.
        /// </summary>
        /// <param name="characterData">The character data.</param>
        /// <param name="operationType">The operation type.</param>
        /// <param name="sanctionUid">The sanction UID.</param>
        /// <param name="sanctionKind">The sanction kind.</param>
        /// <param name="reasonDetails">The reason details.</param>
        /// <param name="releaser">The releaser.</param>
        /// <param name="comment">The comment.</param>
        /// <param name="sanctionType">The sanction type.</param>
        /// <param name="sanctionPeriod">The sanction period.</param>
        /// <param name="sanctionCount">The sanction count.</param>
        public async Task CharacterSanctionAsync(CharacterData characterData, SanctionOperationType operationType, Guid sanctionUid, int sanctionKind, string reasonDetails, string releaser, string comment, int sanctionType, int sanctionPeriod, int sanctionCount)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                var result = await _sqlDatabaseService.ExecuteProcedureAsync(
                "up_char_sanction",
                connection,
                transaction,
                ("@kind", sanctionKind),
                ("@sanction_uid", sanctionUid),
                ("@personnel", "RHToolkit"),
                ("@releaser", releaser),
                ("@comment", comment),
                ("@character_id", characterData.CharacterID),
                ("@sanction_type", sanctionType),
                ("@sanction_count", sanctionCount),
                ("@sanction_period_type", sanctionPeriod)
                );

                transaction.Commit();

                if (operationType == SanctionOperationType.Add)
                {
                    (DateTime startTime, DateTime endTime) = await GetSanctionTimesAsync((Guid)result);
                    await SanctionLogAsync(characterData, (Guid)result, startTime, endTime, reasonDetails);
                    await GMAuditAsync(characterData, "Character Sanction", reasonDetails);
                }
                else
                {
                    await UpdateSanctionLogAsync((Guid)result, releaser, comment, 1);
                    await GMAuditAsync(characterData, "Character Sanction Release", reasonDetails);
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Reads the list of sanctions for a character.
        /// </summary>
        /// <param name="characterId">The character ID.</param>
        /// <returns>A DataTable containing the sanctions.</returns>
        public async Task<DataTable> ReadCharacterSanctionListAsync(Guid characterId)
        {
            DataTable dataTable = new();
            dataTable.Columns.Add("SanctionUid", typeof(Guid));
            dataTable.Columns.Add("StartDate", typeof(string));
            dataTable.Columns.Add("ReleaseDate", typeof(string));
            dataTable.Columns.Add("Reason", typeof(string));
            dataTable.Columns.Add("AddedBy", typeof(string));
            dataTable.Columns.Add("RemovedBy", typeof(string));
            dataTable.Columns.Add("Comment", typeof(string));
            dataTable.Columns.Add("IsApply", typeof(int));

            try
            {
                string selectQuery = "SELECT * FROM Character_Sanction WHERE [character_id] = @character_id";

                using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

                await _sqlDatabaseService.ExecuteQueryAsync(
                     selectQuery,
                     connection,
                     reader =>
                     {
                         while (reader.Read())
                         {
                             Guid sanctionUid = reader.GetGuid(0);
                             byte sanctionType = reader.GetByte(2);
                             byte sanctionCount = reader.GetByte(3);
                             byte sanctionPeriod = reader.GetByte(4);

                             DateTime startDate = reader.GetDateTime(6);
                             DateTime? releaseDate = !reader.IsDBNull(7) ? reader.GetDateTime(7) : null;
                             string addedBy = reader.GetString(8);
                             string removedBy = reader.GetString(9);
                             string comment = reader.GetString(10);

                             byte isApply = reader.GetByte(11);

                             string sanctionTypeName = GetEnumDescription((SanctionType)sanctionType);
                             string sanctionCountName = GetEnumDescription((SanctionCount)sanctionCount);
                             string sanctionPeriodName = GetEnumDescription((SanctionPeriod)sanctionPeriod);
                             string reason = $"{sanctionTypeName}|{sanctionCountName}|{sanctionPeriodName}";

                             dataTable.Rows.Add(sanctionUid, startDate.ToString(), releaseDate?.ToString(), reason, addedBy, removedBy, comment, isApply);
                         }
                     },
                     ("@character_id", characterId)
                 );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading sanction: {ex.Message}");
            }

            return dataTable;
        }

        /// <summary>
        /// Gets the start and end times of a sanction.
        /// </summary>
        /// <param name="sanctionUid">The sanction UID.</param>
        /// <returns>A tuple containing the start and end times.</returns>
        public async Task<(DateTime startTime, DateTime endTime)> GetSanctionTimesAsync(Guid sanctionUid)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            var result = await _sqlDatabaseService.ExecuteDataQueryAsync(
                "SELECT start_time, end_time FROM Character_Sanction WHERE uid = @uid",
                connection,
                null,
                ("@uid", sanctionUid)
            );

            if (result.Rows.Count > 0)
            {
                var startTime = (DateTime)result.Rows[0]["start_time"];
                var endTime = (DateTime)result.Rows[0]["end_time"];
                return (startTime, endTime);
            }
            else
            {
                return (DateTime.Now, DateTime.Now);
            }
        }

        /// <summary>
        /// Checks if a character has an active sanction.
        /// </summary>
        /// <param name="characterId">The character ID.</param>
        /// <returns>True if the character has an active sanction, otherwise false.</returns>
        public async Task<bool> CharacterHasSanctionAsync(Guid characterId)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            object? result = await _sqlDatabaseService.ExecuteScalarAsync(
                "SELECT COUNT(*) FROM Character_Sanction WHERE [character_id] = @character_id AND [is_apply] = 1",
                connection,
                ("@character_id", characterId)
            );

            int sanctionCount = result != null ? (int)result : 0;

            return sanctionCount > 0;
        }

        #endregion

        #region Guild

        /// <summary>
        /// Gets the name of a guild by its ID.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <returns>The name of the guild.</returns>
        public async Task<string?> GetGuildNameAsync(Guid guildId)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            string selectQuery = "SELECT name FROM GuildTable WHERE guild_id = @guildId";
            var parameters = new (string, object)[] { ("@guildId", guildId) };

            object? result = await _sqlDatabaseService.ExecuteScalarAsync(selectQuery, connection, parameters);
            return result != null ? result.ToString() : Resources.NoGuild;
        }

        /// <summary>
        /// Gets the names of multiple guilds by their IDs in a single query.
        /// </summary>
        /// <param name="guildIds">The guild IDs.</param>
        /// <returns>A dictionary mapping guild IDs to guild names.</returns>
        private async Task<Dictionary<Guid, string>> GetGuildNamesBatchAsync(Guid[] guildIds)
        {
            Dictionary<Guid, string> guildNames = [];

            if (guildIds == null || guildIds.Length == 0)
            {
                return guildNames;
            }

            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            // Build a parameterized IN clause
            List<string> parameterNames = [];
            List<(string, object)> parameters = [];
            
            for (int i = 0; i < guildIds.Length; i++)
            {
                string paramName = $"@guildId{i}";
                parameterNames.Add(paramName);
                parameters.Add((paramName, guildIds[i]));
            }

            string selectQuery = $"SELECT guild_id, name FROM GuildTable WHERE guild_id IN ({string.Join(", ", parameterNames)})";

            DataTable dataTable = await _sqlDatabaseService.ExecuteDataQueryAsync(selectQuery, connection, null, [.. parameters]);

            foreach (DataRow row in dataTable.Rows)
            {
                Guid guildId = (Guid)row["guild_id"];
                string guildName = row["name"]?.ToString() ?? Resources.NoGuild;
                guildNames[guildId] = guildName;
            }

            return guildNames;
        }

        #endregion

        #region Mail

        /// <summary>
        /// Sends mail to multiple recipients.
        /// </summary>
        /// <param name="sender">The sender's name.</param>
        /// <param name="message">The message content.</param>
        /// <param name="gold">The amount of gold to send.</param>
        /// <param name="itemCharge">The item charge.</param>
        /// <param name="returnDays">The return days.</param>
        /// <param name="recipients">The list of recipients.</param>
        /// <param name="itemDataList">The list of items to send.</param>
        /// <returns>A tuple containing the lists of successful and failed recipients.</returns>
        public async Task<(List<string> successfulRecipients, List<string> failedRecipients)> SendMailAsync(
            string sender,
            string? message,
            int gold,
            int itemCharge,
            int returnDays,
            string[] recipients,
            List<ItemData> itemDataList)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                if (message != null)
                {
                    message = message.Replace("'", "''");
                }

                message += $"<br><br><right>{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                (Guid? senderCharacterId, Guid? senderAuthId, string? senderAccountName) = await GetCharacterInfoAsync(sender);
                int createType = (itemCharge != 0) ? 5 : 0;
                List<string> successfulRecipients = [];
                List<string> failedRecipients = [];

                foreach (var currentRecipient in recipients)
                {
                    Guid mailId = Guid.NewGuid();

                    (Guid? recipientCharacterId, Guid? recipientAuthId, string? recipientAccountName) = await GetCharacterInfoAsync(currentRecipient);

                    if (senderCharacterId == null && createType == 5)
                    {
                        string failedRecipientMessage = string.Format(Resources.InvalidSenderDesc, sender);
                        failedRecipients.Add(failedRecipientMessage);
                        continue;
                    }

                    if (senderCharacterId == null)
                    {
                        senderCharacterId = Guid.Empty;
                        senderAuthId = Guid.Empty;
                    }

                    if (senderCharacterId == recipientCharacterId)
                    {
                        string failedRecipientMessage = string.Format(Resources.SendMailSameName, sender);
                        failedRecipients.Add(failedRecipientMessage);
                        continue;
                    }

                    if (recipientCharacterId == null)
                    {
                        string failedRecipientMessage = string.Format(Resources.NonExistentRecipient, currentRecipient);
                        failedRecipients.Add(failedRecipientMessage);
                        continue;
                    }

                    string auditMessage = "";

                    if (gold > 0)
                    {
                        auditMessage += $"[<font color=blue>{Resources.GMAuditAttachGold} - {gold}</font>]<br></font>";
                    }

                    await InsertMailAsync(connection, transaction, senderAuthId, senderCharacterId, sender, currentRecipient, message, gold, returnDays, itemCharge, mailId, createType);

                    if (itemDataList != null)
                    {
                        foreach (ItemData itemData in itemDataList)
                        {
                            if (itemData.ItemId != 0)
                            {
                                auditMessage += $"[<font color=blue>{Resources.GMAuditAttachItem} - {itemData.ItemId} ({itemData.ItemAmount})</font>]<br></font>";
                                await InsertMailItemAsync(connection, transaction, itemData, recipientAuthId, recipientCharacterId, mailId, itemData.SlotIndex);
                            }
                        }
                    }

                    await GMAuditMailAsync(recipientAccountName!, recipientCharacterId, currentRecipient, Resources.SendMail, $"<font color=blue>{Resources.SendMail}</font>]<br><font color=red>{Resources.Sender}: RHToolkit: {senderCharacterId}, {Resources.Recipient}: {currentRecipient}, GUID:{{{recipientCharacterId}}}<br></font>" + auditMessage);

                    successfulRecipients.Add(currentRecipient);
                }

                transaction.Commit();

                return (successfulRecipients, failedRecipients);

            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Inserts a new mail record.
        /// </summary>
        /// <param name="connection">The SQL connection.</param>
        /// <param name="transaction">The SQL transaction.</param>
        /// <param name="senderAuthId">The sender's auth ID.</param>
        /// <param name="senderCharacterId">The sender's character ID.</param>
        /// <param name="mailSender">The mail sender's name.</param>
        /// <param name="recipient">The recipient's name.</param>
        /// <param name="content">The mail content.</param>
        /// <param name="gold">The amount of gold to send.</param>
        /// <param name="returnDay">The return day.</param>
        /// <param name="reqGold">The required gold.</param>
        /// <param name="mailId">The mail ID.</param>
        /// <param name="createType">The create type.</param>
        public async Task InsertMailAsync(SqlConnection connection, SqlTransaction transaction, Guid? senderAuthId, Guid? senderCharacterId, string mailSender, string recipient, string content, int gold, int returnDay, int reqGold, Guid mailId, int createType)
        {
            try
            {
                await _sqlDatabaseService.ExecuteProcedureAsync(
                    "up_insert_mail",
                    connection,
                    transaction,
                    ("@sender_auth_id", senderAuthId ?? Guid.Empty),
                    ("@sender_character_id", senderCharacterId ?? Guid.Empty),
                    ("@sender_name", mailSender),
                    ("@recver_name", recipient),
                    ("@msg", content),
                    ("@money", gold),
                    ("@return_day", returnDay),
                    ("@req_money", reqGold),
                    ("@NewMailID", mailId),
                    ("@createType", createType)
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inserting mail: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Inserts a new mail item record.
        /// </summary>
        /// <param name="connection">The SQL connection.</param>
        /// <param name="transaction">The SQL transaction.</param>
        /// <param name="itemData">The item data.</param>
        /// <param name="recipientAuthId">The recipient's auth ID.</param>
        /// <param name="recipientCharacterId">The recipient's character ID.</param>
        /// <param name="mailId">The mail ID.</param>
        /// <param name="slotIndex">The slot index.</param>
        public async Task InsertMailItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData, Guid? recipientAuthId, Guid? recipientCharacterId, Guid mailId, int slotIndex)
        {
            try
            {
                await _sqlDatabaseService.ExecuteNonQueryAsync(
                     "INSERT INTO n_mailitem (item_uid, mail_uid, character_id, auth_id, page_index, slot_index, code, use_cnt, remain_time, create_time, update_time, gcode, durability, enhance_level, option_1_code, option_1_value, option_2_code, option_2_value, option_3_code, option_3_value, option_group, ReconNum, ReconState, socket_count, socket_1_code, socket_1_value, socket_2_code, socket_2_value, socket_3_code, socket_3_value, expire_time, lock_pwd, activity_value, link_id, is_seizure, socket_1_color, socket_2_color, socket_3_color, rank, acquireroute, physical, magical, durabilitymax, weight) " +
                     "VALUES (@item_id, @MailId, @character_id, @auth_id, @page_index, @slot_index, @code, @use_cnt, @remain_time, GETDATE(), GETDATE(), @gcode, @durability, @enhance_level, @option_1_code, @option_1_value, @option_2_code, @option_2_value, @option_3_code, @option_3_value, 0, @ReconNum, @ReconState, @socket_count, @socket_1_code, @socket_1_value, @socket_2_code, @socket_2_value, @socket_3_code, @socket_3_value, @expire_time, '', @activity_value, @link_id, 0, @socket_1_color, @socket_2_color, @socket_3_color, @rank, @acquireroute, @physical, @magical, @durabilitymax, @weight)",
                     connection,
                     transaction,
                     ("@character_id", recipientCharacterId ?? Guid.Empty),
                     ("@auth_id", recipientAuthId ?? Guid.Empty),
                     ("@item_id", Guid.NewGuid()),
                     ("@code", itemData.ItemId),
                     ("@use_cnt", itemData.ItemAmount),
                     ("@remain_time", 0),
                     ("@gcode", 0),
                     ("@page_index", 61),
                     ("@slot_index", slotIndex),
                     ("@durability", itemData.Durability),
                     ("@enhance_level", itemData.EnhanceLevel),
                     ("@option_1_code", itemData.Option1Code),
                     ("@option_1_value", itemData.Option1Value),
                     ("@option_2_code", itemData.Option2Code),
                     ("@option_2_value", itemData.Option2Value),
                     ("@option_3_code", itemData.Option3Code),
                     ("@option_3_value", itemData.Option3Value),
                     ("@ReconNum", itemData.Reconstruction),
                     ("@ReconState", itemData.ReconstructionMax),
                     ("@socket_count", itemData.SocketCount),
                     ("@socket_1_code", itemData.Socket1Code),
                     ("@socket_1_value", itemData.Socket1Value),
                     ("@socket_2_code", itemData.Socket2Code),
                     ("@socket_2_value", itemData.Socket2Value),
                     ("@socket_3_code", itemData.Socket3Code),
                     ("@socket_3_value", itemData.Socket3Value),
                     ("@socket_1_color", itemData.Socket1Color),
                     ("@socket_2_color", itemData.Socket2Color),
                     ("@socket_3_color", itemData.Socket3Color),
                     ("@expire_time", 0),
                     ("@MailId", mailId),
                     ("@activity_value", itemData.AugmentStone),
                     ("@link_id", "00000000-0000-0000-0000-000000000000"),
                     ("@rank", itemData.Rank),
                     ("@acquireroute", 0),
                     ("@physical", 0),
                     ("@magical", 0),
                     ("@durabilitymax", itemData.DurabilityMax),
                     ("@weight", itemData.Weight)
                 );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inserting mail item: {ex.Message}", ex);
            }
        }

        #endregion

        #region Coupon

        /// <summary>
        /// Reads the list of coupons.
        /// </summary>
        /// <returns>A DataTable containing the coupons.</returns>
        public async Task<DataTable?> ReadCouponListAsync()
        {
            DataTable dataTable = new();
            dataTable.Columns.Add("no", typeof(int));
            dataTable.Columns.Add("use", typeof(byte));
            dataTable.Columns.Add("use_date", typeof(DateTime));
            dataTable.Columns.Add("bcust_id", typeof(string));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("coupon_type", typeof(short));
            dataTable.Columns.Add("coupon", typeof(string));
            dataTable.Columns.Add("valid_date", typeof(DateTime));
            dataTable.Columns.Add("item_code", typeof(int));
            dataTable.Columns.Add("item_count", typeof(short));
            dataTable.Columns.Add("CouponStatus", typeof(string));

            try
            {
                string selectQuery = "SELECT * FROM Coupon";

                using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

                await _sqlDatabaseService.ExecuteQueryAsync(
                     selectQuery,
                     connection,
                     reader =>
                     {
                         while (reader.Read())
                         {
                             int no = reader.GetInt32(0);
                             byte use = reader.GetByte(1);
                             DateTime useDate = reader.GetDateTime(2);
                             string? accountName = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty;
                             string? characterName = !reader.IsDBNull(6) ? reader.GetString(6) : string.Empty;
                             short couponType = reader.GetInt16(7);
                             string couponCode = reader.GetString(8);
                             DateTime? validDate = !reader.IsDBNull(10) ? reader.GetDateTime(10) : null;
                             int itemCode = reader.GetInt32(11);
                             short itemCount = reader.GetInt16(12);

                             string couponStatus = use == 0 ? Resources.Unused : Resources.Used;

                             dataTable.Rows.Add(no, use, useDate, accountName, characterName, couponType, couponCode, validDate, itemCode, itemCount, couponStatus);
                         }
                     }
                 );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

            return dataTable;
        }

        /// <summary>
        /// Checks if a coupon exists.
        /// </summary>
        /// <param name="couponCode">The coupon code.</param>
        /// <returns>True if the coupon exists, otherwise false.</returns>
        public async Task<bool> CouponExists(string couponCode)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");

            object? result = await _sqlDatabaseService.ExecuteScalarAsync(
                "SELECT COUNT(*) FROM Coupon WHERE coupon = @coupon",
                connection,
                ("@coupon", couponCode)
            );

            int couponCount = result != null ? (int)result : 0;

            return couponCount > 0;
        }

        /// <summary>
        /// Adds a new coupon.
        /// </summary>
        /// <param name="couponCode">The coupon code.</param>
        /// <param name="validDate">The valid date.</param>
        /// <param name="itemData">The item data.</param>
        public async Task AddCouponAsync(string couponCode, DateTime validDate, ItemData itemData)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _sqlDatabaseService.ExecuteNonQueryAsync(
                     "INSERT INTO Coupon ([use], use_date, coupon_type, coupon, valid_date, item_code, item_count) " +
                     "VALUES (@use, @use_date, @coupon_type, @coupon, @valid_date, @item_code, @item_count)",
                     connection,
                     transaction,
                     ("@use", 0),
                     ("@use_date", DateTime.Now),
                     ("@coupon_type", 2),
                     ("@coupon", couponCode),
                     ("@valid_date", validDate),
                     ("@item_code", itemData.ItemId),
                     ("@item_count", itemData.ItemAmount)
                 );
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error adding coupon: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes a coupon.
        /// </summary>
        /// <param name="couponNumber">The coupon number.</param>
        public async Task DeleteCouponAsync(int couponNumber)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _sqlDatabaseService.ExecuteNonQueryAsync(
               "DELETE FROM Coupon WHERE no = @couponNumber",
               connection,
               transaction,
               ("@couponNumber", couponNumber)
               );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error deleting coupon: {ex.Message}", ex);
            }
        }

        #endregion

        #endregion

        #region GMRustyHearts

        /// <summary>
        /// Logs a GM audit action.
        /// </summary>
        /// <param name="characterData">The character data.</param>
        /// <param name="action">The action performed.</param>
        /// <param name="auditMessage">The audit message.</param>
        public async Task GMAuditAsync(CharacterData characterData, string action, string auditMessage)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("GMRustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                string changes = $"<font color=blue>{action}</font>]<br><font color=red>{auditMessage}<br>{characterData.CharacterName}, GUID:{{{characterData.CharacterID.ToString().ToUpper()}}}<br></font>";

                await _sqlDatabaseService.ExecuteNonQueryAsync("INSERT INTO GMAudit(audit_id, AdminID, world_index, bcust_id, character_id, char_name, Type, Modify, Memo, date) " +
                       "VALUES (@audit_id, @AdminID, @world_index, @bcust_id, @character_id, @char_name, @Type, @Modify, @Memo, @date)",
                       connection,
                       transaction,
                       ("@audit_id", Guid.NewGuid()),
                       ("@AdminID", "RHToolkit"),
                       ("@world_index", 1),
                       ("@bcust_id", characterData.AccountName!),
                       ("@character_id", characterData.CharacterID),
                       ("@char_name", characterData.CharacterName!),
                       ("@Type", action),
                       ("@Modify", changes),
                       ("@Memo", ""),
                       ("@date", DateTime.Now)
                  );
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Logs a GM audit action for mail.
        /// </summary>
        /// <param name="accountName">The account name.</param>
        /// <param name="characterId">The character ID.</param>
        /// <param name="characterName">The character name.</param>
        /// <param name="action">The action performed.</param>
        /// <param name="auditMessage">The audit message.</param>
        public async Task GMAuditMailAsync(string accountName, Guid? characterId, string characterName, string action, string auditMessage)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("GMRustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                string changes = $"<font color=blue>{action}</font>]<br><font color=red>{auditMessage}<br>{characterName}, GUID:{{{characterId?.ToString().ToUpper()}}}<br></font>";

                await _sqlDatabaseService.ExecuteNonQueryAsync("INSERT INTO GMAudit(audit_id, AdminID, world_index, bcust_id, character_id, char_name, Type, Modify, Memo, date) " +
                       "VALUES (@audit_id, @AdminID, @world_index, @bcust_id, @character_id, @char_name, @Type, @Modify, @Memo, @date)",
                       connection,
                       transaction,
                       ("@audit_id", Guid.NewGuid()),
                       ("@AdminID", "RHToolkit"),
                       ("@world_index", 1),
                       ("@bcust_id", accountName!),
                       ("@character_id", characterId ?? Guid.Empty),
                       ("@char_name", characterName!),
                       ("@Type", action),
                       ("@Modify", changes),
                       ("@Memo", ""),
                       ("@date", DateTime.Now)
                  );
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        #endregion

        #region RustyHearts_Log

        /// <summary>
        /// Logs a sanction action.
        /// </summary>
        /// <param name="characterData">The character data.</param>
        /// <param name="sanctionUid">The sanction UID.</param>
        /// <param name="startTime">The start time of the sanction.</param>
        /// <param name="endTime">The end time of the sanction.</param>
        /// <param name="reason">The reason for the sanction.</param>
        public async Task SanctionLogAsync(CharacterData characterData, Guid sanctionUid, DateTime startTime, DateTime endTime, string reason)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts_Log");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _sqlDatabaseService.ExecuteNonQueryAsync("INSERT INTO Sanction_Log(log_type, sanction_uid, world_id, bcust_id, item_uid, character_id, char_name, item_name, start_time, end_time, personnel, releaser, cause, comment, is_release, reg_date) " +
                                                          "VALUES (@log_type, @sanction_uid, @world_id, @bcust_id, @item_uid, @character_id, @char_name, @item_name, " +
                                                          "@start_time, @end_time, @personnel, @releaser, @cause, @comment, @is_release, @reg_date)",
                      connection,
                      transaction,
                      ("@log_type", 1),
                      ("@sanction_uid", sanctionUid),
                      ("@world_id", 1),
                      ("@bcust_id", characterData.AccountName!),
                      ("@item_uid", "00000000-0000-0000-0000-000000000000"),
                      ("@character_id", characterData.CharacterID),
                      ("@char_name", characterData.CharacterName!),
                      ("@item_name", ""),
                      ("@start_time", startTime),
                      ("@end_time", endTime),
                      ("@personnel", "RHToolkit"),
                      ("@releaser", ""),
                      ("@cause", reason),
                      ("@comment", ""),
                      ("@is_release", 0),
                      ("@reg_date", startTime)
                 );
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Updates a sanction log.
        /// </summary>
        /// <param name="sanctionUid">The sanction UID.</param>
        /// <param name="releaser">The releaser.</param>
        /// <param name="comment">The comment.</param>
        /// <param name="isRelease">Indicates if the sanction is released.</param>
        public async Task UpdateSanctionLogAsync(Guid sanctionUid, string releaser, string comment, int isRelease)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts_Log");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _sqlDatabaseService.ExecuteNonQueryAsync(
                     "UPDATE Sanction_Log SET releaser = @releaser, comment = @comment, is_release = @is_release WHERE sanction_uid = @sanction_uid",
                     connection,
                     transaction,
                      ("@sanction_uid", sanctionUid),
                      ("@releaser", releaser),
                      ("@comment", comment),
                      ("@is_release", isRelease)
                 );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }

        #endregion

        #region RustyHearts_Auth

        /// <summary>
        /// Retrieves account data by account identifier.
        /// </summary>
        /// <param name="accountIdentifier">The account identifier.</param>
        /// <returns>The account data.</returns>
        public async Task<AccountData?> GetAccountDataAsync(string accountIdentifier)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts_Auth");

            string selectAuthQuery = "SELECT WindyCode, AuthID, CashMileage, online FROM AuthTable WHERE [WindyCode] = @accountIdentifier";
            DataTable? authDataTable = await _sqlDatabaseService.ExecuteDataQueryAsync(selectAuthQuery, connection, null, ("@accountIdentifier", accountIdentifier));

            DataTable? accountDataTable = await GetAccountInfoAsync(accountIdentifier);

            if (authDataTable.Rows.Count > 0 && accountDataTable != null && accountDataTable.Rows.Count > 0)
            {
                DataRow authRow = authDataTable.Rows[0];
                DataRow accountRow = accountDataTable.Rows[0];
                long zen = await GetAccountCashAsync(accountIdentifier);

                var accountData = new AccountData
                {
                    AccountID = (int)accountRow["AccountID"],
                    AccountName = (string)accountRow["WindyCode"],
                    Email = (string)accountRow["Email"],
                    CreateTime = (DateTime)accountRow["CreatedAt"],
                    LastLogin = (DateTime)accountRow["LastLogin"],
                    IsLocked = (bool)accountRow["IsLocked"],
                    LastLoginIP = (string)accountRow["LastLoginIP"],
                    AuthID = (Guid)authRow["AuthID"],
                    CashMileage = (int)authRow["CashMileage"],
                    IsConnect = (string)authRow["online"],
                    Zen = zen,
                };

                return accountData;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves the cash mileage of an account.
        /// </summary>
        /// <param name="accountName">The account name.</param>
        /// <returns>The cash mileage.</returns>
        public async Task<int> GetAccountCashMileageAsync(string accountName)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts_Auth");

            object? result = await _sqlDatabaseService.ExecuteScalarAsync(
                "SELECT CashMileage FROM AuthTable WHERE [WindyCode] = @accountName",
                connection,
                ("@accountName", accountName)
            );

            int mileage = result != null ? (int)result : 0;

            return mileage;
        }

        #endregion

        #region RustyHearts_Account

        /// <summary>
        /// Retrieves account information by account name.
        /// </summary>
        /// <param name="accountName">The account name.</param>
        /// <returns>A DataTable containing the account information.</returns>
        public async Task<DataTable?> GetAccountInfoAsync(string accountName)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts_Account");

            string selectAccountQuery = "SELECT AccountID, WindyCode, Email, CreatedAt, LastLogin, IsLocked, LastLoginIP FROM AccountTable WHERE [WindyCode] = @accountIdentifier";

            DataTable? accountDataTable = await _sqlDatabaseService.ExecuteDataQueryAsync(selectAccountQuery, connection, null, ("@accountIdentifier", accountName));

            if (accountDataTable.Rows.Count > 0)
            {
                return accountDataTable;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves the cash balance of an account.
        /// </summary>
        /// <param name="accountName">The account name.</param>
        /// <returns>The cash balance.</returns>
        public async Task<long> GetAccountCashAsync(string accountName)
        {
            using SqlConnection connection = await _sqlDatabaseService.OpenConnectionAsync("RustyHearts_Account");

            object? result = await _sqlDatabaseService.ExecuteScalarAsync(
                "SELECT Zen FROM CashTable WHERE [WindyCode] = @accountName",
                connection,
                ("@accountName", accountName)
            );

            long zen = result != null ? (long)result : 0;

            return zen;
        }

        #endregion
    }
}