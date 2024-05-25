using Microsoft.Data.SqlClient;
using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Utilities;
using System.Data;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Services
{
    public class DatabaseService(ISqlDatabaseService databaseService, IGMDatabaseService gmDatabaseService) : IDatabaseService
    {
        private readonly ISqlDatabaseService _databaseService = databaseService;
        private readonly IGMDatabaseService _gmDatabaseService = gmDatabaseService;

        #region RustyHearts

        #region Character

        #region Write
        public async Task UpdateCharacterDataAsync(Guid characterId, NewCharacterData characterData)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _databaseService.ExecuteNonQueryAsync(
                    "UPDATE CharacterTable SET Level = @level, Experience = @experience, SP = @sp, Total_SP = @total_sp, LobbyID = @lobbyID, gold = @gold, Hearts = @hearts, Block_YN = @block_YN, storage_gold = @storage_gold, storage_count = @storage_count, IsTradeEnable = @isTradeEnable, Permission = @permission, GuildPoint = @guild_point, IsMoveEnable = @isMoveEnable WHERE character_id = @character_id",
                    connection,
                    transaction,
                    ("@character_id", characterId),
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
                throw new Exception($"Error Updating Character Data: {ex.Message}", ex);
            }
        }

        public async Task<int> UpdateCharacterNameAsync(Guid characterId, string characterName)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");

            try
            {
                return (int)await _databaseService.ExecuteProcedureAsync(
                    "up_update_character_name",
                    connection,
                    null,
                    ("@character_id", characterId),
                    ("@character_name", characterName)
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error: {ex.Message}", ex);
            }
        }

        public async Task DeleteCharacterAsync(Guid authId, Guid characterId)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _databaseService.ExecuteProcedureAsync(
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
                throw new Exception($"Error deleting character: {ex.Message}", ex);
            }
        }

        public async Task RestoreCharacterAsync(Guid characterId)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _databaseService.ExecuteNonQueryAsync(
                   "insert into CharacterTable select * FROM CharacterTable_DELETE where character_id = @character_id",
                   connection,
                   transaction,
                   ("@character_id", characterId)
               );
                await _databaseService.ExecuteNonQueryAsync(
                   "delete FROM CharacterTable_DELETE where character_id = @character_id",
                   connection,
                   transaction,
                   ("@character_id", characterId)
               );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error restoring character: {ex.Message}", ex);
            }
        }

        public async Task UpdateCharacterClassAsync(Guid characterId, NewCharacterData characterData)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _databaseService.ExecuteNonQueryAsync(
                    "UPDATE CharacterTable SET Class = @class, Job = @job WHERE character_id = @character_id",
                    connection,
                    transaction,
                    ("@character_id", characterId),
                    ("@class", characterData.Class),
                    ("@job", characterData.Job)
                );

                await _databaseService.ExecuteProcedureAsync(
                     "up_skill_reset",
                     connection,
                transaction,
                ("@character_id", characterId)
                );


                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error updating character class: {ex.Message}", ex);
            }
        }

        #endregion

        #region Read

        public async Task<CharacterData?> GetCharacterDataAsync(string characterIdentifier)
        {
            string selectQuery = "SELECT * FROM CharacterTable WHERE [Name] = @characterIdentifier";
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");

            DataTable dataTable;

            dataTable = await _databaseService.ExecuteDataQueryAsync(selectQuery, connection, null, ("@characterIdentifier", characterIdentifier));

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

        public async Task<List<CharacterData>> GetCharacterDataListAsync(string characterIdentifier, string isConnect = "", bool isDeletedCharacter = false)
        {
            string tableName = isDeletedCharacter ? "CharacterTable_DELETE" : "CharacterTable";

            string selectQuery = $"SELECT * FROM {tableName} WHERE ([bcust_id] = @characterIdentifier OR [name] = @characterIdentifier)";

            if (!string.IsNullOrEmpty(isConnect))
            {
                selectQuery += " AND [IsConnect] = @isConnect";
            }

            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");

            DataTable dataTable = await _databaseService.ExecuteDataQueryAsync(
                selectQuery,
                connection,
                null,
                ("@characterIdentifier", characterIdentifier),
                ("@isConnect", isConnect)
            );

            List<CharacterData> characterDataList = [];

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
                    characterData.GuildName = await GetGuildNameAsync((Guid)row["guildid"]);
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

        public async Task<List<ItemData>> GetItemList(Guid characterId, string tableName)
        {
            string selectQuery = $"SELECT * FROM {tableName} WHERE character_id = @character_id;";
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            DataTable dataTable = await _databaseService.ExecuteDataQueryAsync(selectQuery, connection, null, ("@character_id", characterId));

            List<ItemData> itemList = [];

            foreach (DataRow row in dataTable.Rows)
            {
                ItemData item = new()
                {
                    ItemUid = (Guid)row["item_uid"],
                    CharacterId = (Guid)row["character_id"],
                    AuthId = (Guid)row["auth_id"],
                    PageIndex = (int)row["page_index"],
                    SlotIndex = (int)row["slot_index"],
                    ID = (int)row["code"],
                    Amount = (int)row["use_cnt"],
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
                    LinkId = (Guid)row["link_id"],
                    IsSeizure = (byte)row["is_seizure"],
                    Socket1Color = (byte)row["socket_1_color"],
                    Socket2Color = (byte)row["socket_2_color"],
                    Socket3Color = (byte)row["socket_3_color"],
                    DefermentTime = (int)row["deferment_time"],
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

        public async Task<List<ItemData>> GetAccountItemList(Guid authId)
        {
            string selectQuery = $"SELECT * FROM tbl_Account_Storage WHERE auth_id = @auth_id;";
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            DataTable dataTable = await _databaseService.ExecuteDataQueryAsync(selectQuery, connection, null, ("@auth_id", authId));

            List<ItemData> itemList = [];

            foreach (DataRow row in dataTable.Rows)
            {
                ItemData item = new()
                {
                    ItemUid = (Guid)row["item_uid"],
                    AuthId = (Guid)row["auth_id"],
                    PageIndex = (int)row["page_index"],
                    SlotIndex = (int)row["slot_index"],
                    ID = (int)row["code"],
                    Amount = (int)row["use_cnt"],
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
                    IsSeizure = (byte)row["is_seizure"],
                    Socket1Color = (byte)row["socket_1_color"],
                    Socket2Color = (byte)row["socket_2_color"],
                    Socket3Color = (byte)row["socket_3_color"],
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

        public async Task<bool> GetCharacterOnlineAsync(string characterName)
        {
            string selectQuery = "SELECT IsConnect FROM CharacterTable WHERE name = @characterName";
            var parameters = new (string, object)[] { ("@characterName", characterName) };

            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");

            object? result = await _databaseService.ExecuteScalarAsync(selectQuery, connection, parameters);

            if (result != null && result != DBNull.Value)
            {
                return result.ToString() == "Y";
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> IsCharacterOnlineAsync(string characterName)
        {
            if (await GetCharacterOnlineAsync(characterName))
            {
                RHMessageBox.ShowOKMessage($"The character '{characterName}' is currently online. You can't edit an online character.", "Info");
                return true;
            }

            return false;
        }

        public async Task<string[]> GetAllCharacterNamesAsync(string isConnect = "")
        {
            List<string> characterNames = [];

            string query = "SELECT name FROM CharacterTable WHERE ([Block_YN] = 'N')";

            if (!string.IsNullOrEmpty(isConnect))
            {
                query += " AND [IsConnect] = @isConnect";
            }

            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");

            DataTable dataTable = await _databaseService.ExecuteDataQueryAsync(
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

        public async Task<(Guid? characterId, Guid? authid, string? accountName)> GetCharacterInfoAsync(string characterName)
        {
            string selectQuery = "SELECT character_id, authid, bcust_id FROM CharacterTable WHERE name = @characterName";
            var parameters = new (string, object)[] { ("@characterName", characterName) };

            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");

            DataTable dataTable = await _databaseService.ExecuteDataQueryAsync(selectQuery, connection, null, parameters);

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

        #region Character Title

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

            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            DataTable titleDataTable = await _databaseService.ExecuteDataProcedureAsync("up_read_title_list", connection, null, ("@character_id", characterId));

            if (titleDataTable.Rows.Count > 0)
            {
                foreach (DataRow row in titleDataTable.Rows)
                {
                    Guid titleUid = (Guid)row["ID"];
                    int titleId = (int)row["title_code"];
                    int remainTime = (int)row["remain_time"];
                    int expireTime = (int)row["expire_time"];

                    int titleCategory = _gmDatabaseService.GetTitleCategory(titleId);
                    string formattedtitleCategory = titleCategory == 0 ? "Normal" : "Special";
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

        public async Task<DataRow?> ReadCharacterEquipTitleAsync(Guid characterId)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");

            DataTable dataTable = await _databaseService.ExecuteDataProcedureAsync("up_read_equip_title", connection, null, ("@character_id", characterId));

            return dataTable.Rows.Count > 0 ? dataTable.Rows[0] : null;
        }

        public async Task<bool> CharacterHasTitle(Guid characterId, int titleID)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");

            object? result = await _databaseService.ExecuteScalarAsync(
                "SELECT COUNT(*) FROM CharacterTitle WHERE [character_id] = @character_id AND [title_code] = @title_code",
                connection,
                ("@character_id", characterId),
                ("@title_code", titleID)
            );

            int titleCount = result != null ? (int)result : 0;

            return titleCount > 0;
        }

        public async Task AddCharacterTitleAsync(Guid characterId, int titleId, int remainTime, int expireTime)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _databaseService.ExecuteProcedureAsync(
                     "up_add_character_title",
                     connection,
                     transaction,
                     ("@character_id", characterId),
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
                throw new Exception($"Error adding character title: {ex.Message}", ex);
            }
        }

        public async Task EquipCharacterTitleAsync(Guid characterId, Guid titleId)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _databaseService.ExecuteProcedureAsync(
                     "up_equip_character_title",
                     connection,
                     transaction,
                     ("@character_id", characterId),
                     ("@title_id", titleId)
                 );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error equipping character title: {ex.Message}", ex);
            }
        }

        public async Task UnequipCharacterTitleAsync(Guid titleId)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _databaseService.ExecuteProcedureAsync(
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
                throw new Exception($"Error unequipping character title: {ex.Message}", ex);
            }
        }

        public async Task RemoveCharacterTitleAsync(Guid characterId, Guid titleUid)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _databaseService.ExecuteProcedureAsync(
                     "up_delete_character_title",
                     connection,
                     transaction,
                     ("@character_id", characterId),
                     ("@del_title_id", titleUid)
                 );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error removing character title: {ex.Message}", ex);
            }
        }


        #endregion

        #region Fortune

        public async Task<DataRow?> ReadCharacterFortuneAsync(Guid characterId)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");

            DataTable dataTable = await _databaseService.ExecuteDataProcedureAsync("up_read_fortune", connection, null, ("@character_id", characterId));

            return dataTable.Rows.Count > 0 ? dataTable.Rows[0] : null;
        }

        public async Task UpdateFortuneAsync(Guid characterId, int fortune, int selectedFortuneID1, int selectedFortuneID2, int selectedFortuneID3)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _databaseService.ExecuteProcedureAsync(
                     "up_update_fortune",
                     connection,
                     transaction,
                     ("@character_id", characterId),
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
                throw new Exception($"Error updating fortune: {ex.Message}", ex);
            }
        }

        public async Task RemoveFortuneAsync(Guid characterId, int fortuneState)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _databaseService.ExecuteNonQueryAsync(
                "DELETE FROM FortuneTable WHERE character_id = @character_id",
                connection,
                transaction,
                ("@character_id", characterId)
            );

                await _databaseService.ExecuteProcedureAsync(
                     "up_update_character_fortune",
                     connection,
                     transaction,
                     ("@character_id", characterId),
                     ("@fortune", fortuneState)
                 );

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error removing fortune: {ex.Message}", ex);
            }
        }
        #endregion

        #region Sanction

        public async Task<(Guid SanctionUid, bool IsInsert)> CharacterSanctionAsync(Guid characterId, Guid sanctionUid, int sanctionKind, string releaser, string comment, int sanctionType, int sanctionPeriod, int sanctionCount)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                var result = await _databaseService.ExecuteProcedureAsync(
                "up_char_sanction",
                connection,
                transaction,
                ("@kind", sanctionKind),
                ("@sanction_uid", sanctionUid),
                ("@personnel", "RHToolkit"),
                ("@releaser", releaser),
                ("@comment", comment),
                ("@character_id", characterId),
                ("@sanction_type", sanctionType),
                ("@sanction_count", sanctionCount),
                ("@sanction_period_type", sanctionPeriod)
            );
                transaction.Commit();

                return ((Guid)result, false);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error adding sanction: {ex.Message}", ex);
            }
        }

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

                using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");

                await _databaseService.ExecuteQueryAsync(
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

        public async Task<(DateTime startTime, DateTime? endTime)> GetSanctionTimesAsync(Guid sanctionUid)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            var result = await _databaseService.ExecuteDataQueryAsync(
                "SELECT start_time, end_time FROM Character_Sanction WHERE uid = @uid",
                connection,
                transaction,
                ("@uid", sanctionUid)
            );

            if (result.Rows.Count > 0)
            {
                var startTime = (DateTime)result.Rows[0]["start_time"];
                var endTime = result.Rows[0]["end_time"] != DBNull.Value ? (DateTime?)result.Rows[0]["end_time"] : null;
                return (startTime, endTime);
            }
            else
            {
                return (DateTime.UtcNow, null);
            }
        }

        public async Task<bool> CharacterHasSanctionAsync(Guid characterId)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");

            object? result = await _databaseService.ExecuteScalarAsync(
                "SELECT COUNT(*) FROM Character_Sanction WHERE [character_id] = @character_id AND [is_apply] = 1",
                connection,
                ("@character_id", characterId)
            );

            int sanctionCount = result != null ? (int)result : 0;

            return sanctionCount > 0;
        }

        #endregion

        #region Guild
        public async Task<string?> GetGuildNameAsync(Guid guildId)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");

            string selectQuery = "SELECT name FROM GuildTable WHERE guild_id = @guildId";
            var parameters = new (string, object)[] { ("@guildId", guildId) };

            object? result = await _databaseService.ExecuteScalarAsync(selectQuery, connection, parameters);
            return result != null ? result.ToString() : "No Guild";
        }
        #endregion

        #region Mail
        public async Task InsertMailAsync(Guid? senderAuthId, Guid? senderCharacterId, string mailSender, string recipient, string content, int gold, int returnDay, int reqGold, Guid mailId, int createType)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _databaseService.ExecuteProcedureAsync(
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
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error inserting mail: {ex.Message}", ex);
            }
        }

        public async Task InsertMailItemAsync(ItemData itemData, Guid? recipientAuthId, Guid? recipientCharacterId, Guid mailId, int slotIndex)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _databaseService.ExecuteNonQueryAsync(
                     "INSERT INTO n_mailitem (item_uid, mail_uid, character_id, auth_id, page_index, slot_index, code, use_cnt, remain_time, create_time, update_time, gcode, durability, enhance_level, option_1_code, option_1_value, option_2_code, option_2_value, option_3_code, option_3_value, option_group, ReconNum, ReconState, socket_count, socket_1_code, socket_1_value, socket_2_code, socket_2_value, socket_3_code, socket_3_value, expire_time, lock_pwd, activity_value, link_id, is_seizure, socket_1_color, socket_2_color, socket_3_color, rank, acquireroute, physical, magical, durabilitymax, weight) " +
                     "VALUES (@item_id, @MailId, @character_id, @auth_id, @page_index, @slot_index, @code, @use_cnt, @remain_time, GETDATE(), GETDATE(), @gcode, @durability, @enhance_level, @option_1_code, @option_1_value, @option_2_code, @option_2_value, @option_3_code, @option_3_value, 0, @ReconNum, @ReconState, @socket_count, @socket_1_code, @socket_1_value, @socket_2_code, @socket_2_value, @socket_3_code, @socket_3_value, @expire_time, '', @activity_value, @link_id, 0, @socket_1_color, @socket_2_color, @socket_3_color, @rank, @acquireroute, @physical, @magical, @durabilitymax, @weight)",
                     connection,
                     transaction,
                     ("@character_id", recipientCharacterId ?? Guid.Empty),
                     ("@auth_id", recipientAuthId ?? Guid.Empty),
                     ("@item_id", Guid.NewGuid()),
                     ("@code", itemData.ID),
                     ("@use_cnt", itemData.Amount),
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
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error inserting mail item: {ex.Message}", ex);
            }
        }

        #endregion

        #endregion

        #region RustyHearts_Log
        public async Task GMAuditAsync(string windyCode, Guid? characterId, string characterName, string action, string modify)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("GMRustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                string changes = $"<font color=blue>{action}</font>]<br><font color=red>{modify}<br>{characterName}, GUID:{{{characterId?.ToString().ToUpper()}}}<br></font>";

                await _databaseService.ExecuteNonQueryAsync("INSERT INTO GMAudit(audit_id, AdminID, world_index, bcust_id, character_id, char_name, Type, Modify, Memo, date) " +
                       "VALUES (@audit_id, @AdminID, @world_index, @bcust_id, @character_id, @char_name, @Type, @Modify, @Memo, @date)",
                       connection,
                       transaction,
                       ("@audit_id", Guid.NewGuid()),
                       ("@AdminID", "RHToolkit"),
                       ("@world_index", 1),
                       ("@bcust_id", windyCode),
                       ("@character_id", characterId ?? Guid.Empty),
                       ("@char_name", characterName),
                       ("@Type", action),
                       ("@Modify", changes),
                       ("@Memo", ""),
                       ("@date", DateTime.UtcNow)
                  );
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error: {ex.Message}", ex);
            }
        }

        public async Task SanctionLogAsync(Guid sanctionUid, Guid characterId, string windyCode, string characterName, DateTime startTime, DateTime? endTime, string reason)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts_Log");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _databaseService.ExecuteNonQueryAsync("INSERT INTO Sanction_Log(log_type, sanction_uid, world_id, bcust_id, item_uid, character_id, char_name, item_name, start_time, end_time, personnel, releaser, cause, comment, is_release, reg_date) " +
                                                          "VALUES (@log_type, @sanction_uid, @world_id, @bcust_id, @item_uid, @character_id, @char_name, @item_name, " +
                                                          "@start_time, @end_time, @personnel, @releaser, @cause, @comment, @is_release, @reg_date)",
                      connection,
                      transaction,
                      ("@log_type", 1),
                      ("@sanction_uid", sanctionUid),
                      ("@world_id", 1),
                      ("@bcust_id", windyCode),
                      ("@item_uid", "00000000-0000-0000-0000-000000000000"),
                      ("@character_id", characterId),
                      ("@char_name", characterName),
                      ("@item_name", ""),
                      ("@start_time", startTime),
                      ("@end_time", DateTime.UtcNow),
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
                throw new Exception($"Error Sanction Log: {ex.Message} {ex.StackTrace}", ex);
            }
        }

        public async Task UpdateSanctionLogAsync(Guid sanctionUid, string releaser, string comment, int isRelease)
        {
            using SqlConnection connection = await _databaseService.OpenConnectionAsync("RustyHearts_Log");
            using var transaction = connection.BeginTransaction();

            try
            {
                await _databaseService.ExecuteNonQueryAsync(
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
                throw new Exception($"Error: {ex.Message}", ex);
            }
        }
        #endregion

        #region RustyHeats_Auth

        #endregion

        #region RustyHeats_Account

        #endregion
    }

}
