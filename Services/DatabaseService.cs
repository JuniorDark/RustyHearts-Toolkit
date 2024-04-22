using RHToolkit.Models;
using System.Data;
using System.Windows;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Services
{
    public class DatabaseService(ISqlDatabaseService databaseService) : IDatabaseService
    {
        private readonly ISqlDatabaseService _databaseService = databaseService;

        #region RustyHearts

        #region Character
        public CharacterData? GetCharacterData(string characterName)
        {
            string selectQuery = "SELECT * FROM CharacterTable WHERE [Name] = @name";
            using var connection = _databaseService.OpenConnection("RustyHearts");
            DataTable dataTable = _databaseService.ExecuteDataQuery(selectQuery, connection, null, ("@name", characterName));

            if (dataTable.Rows.Count > 0)
            {
                DataRow row = dataTable.Rows[0];
                var characterData = new CharacterData
                {
                    CharacterID = (Guid)row["character_id"],
                    WindyCode = (string)row["bcust_id"],
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
                    characterData.GuildName = GetGuildName((Guid)row["guildid"]);
                }
                else
                {
                    characterData.GuildName = "No Guild";
                }

                return characterData;
            }
            else
            {
                return null;
            }
        }

        public void UpdateCharacterData(Guid characterId, NewCharacterData characterData)
        {
            using var connection = _databaseService.OpenConnection("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                _databaseService.ExecuteNonQuery(
                    "UPDATE CharacterTable SET Class = @class, Job = @job, Level = @level, Experience = @experience, SP = @sp, Total_SP = @total_sp, Fatigue = @fatigue, LobbyID = @lobbyID, gold = @gold, Hearts = @hearts, Block_YN = @block_YN, storage_gold = @storage_gold, storage_count = @storage_count, IsTradeEnable = @isTradeEnable, Permission = @permission, GuildPoint = @guild_point, IsMoveEnable = @isMoveEnable WHERE character_id = @character_id",
                    connection,
                    transaction,
                    ("@character_id", characterId),
                    ("@class", characterData.Class),
                    ("@job", characterData.Job),
                    ("@level", characterData.Level),
                    ("@experience", characterData.Experience),
                    ("@sp", characterData.SP),
                    ("@total_sp", characterData.TotalSP),
                    ("@fatigue", characterData.Fatigue),
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

        public int UpdateCharacterName(Guid characterId, string characterName)
        {
            using var connection = _databaseService.OpenConnection("RustyHearts");

            try
            {
                return (int)_databaseService.ExecuteProcedure(
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

        #region Read
        public bool IsCharacterOnline(string characterName)
        {
            string selectQuery = "SELECT IsConnect FROM CharacterTable WHERE name = @characterName";
            var parameters = new (string, object)[] { ("@characterName", characterName) };

            using var connection = _databaseService.OpenConnection("RustyHearts");

            object result = _databaseService.ExecuteScalar(selectQuery, connection, parameters);

            if (result != null && result != DBNull.Value)
            {
                return result.ToString() == "Y";
            }
            else
            {
                return false;
            }
        }

        public string[] GetAllCharacterNames()
        {
            List<string> characterNames = [];

            string query = "SELECT name FROM CharacterTable";

            using var connection = _databaseService.OpenConnection("RustyHearts");

            DataTable dataTable = _databaseService.ExecuteDataQuery(query, connection);

            foreach (DataRow row in dataTable.Rows)
            {
                string characterName = row["name"]?.ToString() ?? string.Empty;
                characterNames.Add(characterName);
            }

            return [.. characterNames];
        }

        public string[] GetAllOnlineCharacterNames()
        {
            List<string> characterNames = [];

            string query = "SELECT name FROM CharacterTable WHERE IsConnect = 'Y'";

            using var connection = _databaseService.OpenConnection("RustyHearts");

            DataTable dataTable = _databaseService.ExecuteDataQuery(query, connection);

            foreach (DataRow row in dataTable.Rows)
            {
                string characterName = row["name"]?.ToString() ?? string.Empty;
                characterNames.Add(characterName);
            }

            return [.. characterNames];
        }

        public (Guid? characterId, Guid? authid, string? windyCode) GetCharacterInfo(string characterName)
        {
            string selectQuery = "SELECT character_id, authid, bcust_id FROM CharacterTable WHERE name = @characterName";
            var parameters = new (string, object)[] { ("@characterName", characterName) };

            using var connection = _databaseService.OpenConnection("RustyHearts");

            DataTable dataTable = _databaseService.ExecuteDataQuery(selectQuery, connection, null, parameters);

            if (dataTable.Rows.Count > 0)
            {
                DataRow row = dataTable.Rows[0];
                return ((Guid)row["character_id"], (Guid)row["authid"], row["bcust_id"].ToString());
            }
            else
            {
                return (null, null, null);
            }
        }
        #endregion

        #endregion

        #region Fortune

        public DataRow? ReadCharacterFortune(Guid characterId)
        {
            using var connection = _databaseService.OpenConnection("RustyHearts");

            DataTable dataTable = _databaseService.ExecuteDataProcedure("up_read_fortune", connection, null, ("@character_id", characterId));

            return dataTable.Rows.Count > 0 ? dataTable.Rows[0] : null;
        }

        public void UpdateFortune(Guid characterId, int fortune, int selectedFortuneID1, int selectedFortuneID2, int selectedFortuneID3)
        {
            using var connection = _databaseService.OpenConnection("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                _databaseService.ExecuteProcedure(
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

        public void RemoveFortune(Guid characterId, int fortuneState)
        {
            using var connection = _databaseService.OpenConnection("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                _databaseService.ExecuteNonQuery(
               "DELETE FROM FortuneTable WHERE character_id = @character_id",
               connection,
               transaction,
               ("@character_id", characterId)
           );

                _databaseService.ExecuteProcedure(
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

        public DataTable ReadCharacterSanctionList(Guid characterId)
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

                using var connection = _databaseService.OpenConnection("RustyHearts");

                _databaseService.ExecuteQuery(
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
                MessageBox.Show($"Error reading sanction: {ex.Message}", "Error");
            }

            return dataTable;
        }

        public (DateTime startTime, DateTime? endTime) GetSanctionTimes(Guid sanctionUid)
        {
            using var connection = _databaseService.OpenConnection("RustyHearts");
            using var transaction = connection.BeginTransaction();

            var result = _databaseService.ExecuteDataQuery(
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
                return (DateTime.MinValue, null);
            }
        }

        public bool CharacterHasSanction(Guid characterId)
        {
            using var connection = _databaseService.OpenConnection("RustyHearts");

            int sanctionCount = (int)_databaseService.ExecuteScalar(
                "SELECT COUNT(*) FROM Character_Sanction WHERE [character_id] = @character_id AND [is_apply] = 1",
                connection,
                ("@character_id", characterId)
            );

            return sanctionCount > 0;
        }

        public (Guid SanctionUid, bool IsInsert) CharacterSanction(Guid characterId, Guid sanctionUid, int sanctionKind, string releaser, string comment, int sanctionType, int sanctionPeriod, int sanctionCount)
        {
            using var connection = _databaseService.OpenConnection("RustyHearts");
            using var transaction = connection.BeginTransaction();

            var result = _databaseService.ExecuteProcedure(
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

            return ((Guid)result, false);
        }

        #endregion

        #region Guild
        public string? GetGuildName(Guid guildId)
        {
            using var connection = _databaseService.OpenConnection("RustyHearts");

            string selectQuery = "SELECT name FROM GuildTable WHERE guild_id = @guildId";
            var parameters = new (string, object)[] { ("@guildId", guildId) };

            object result = _databaseService.ExecuteScalar(selectQuery, connection, parameters);
            return result != null ? result.ToString() : "No Guild";
        }
        #endregion

        #region Mail
        public void InsertMail(Guid? senderAuthId, Guid? senderCharacterId, string mailSender, string recipient, string content, int gold, int returnDay, int reqGold, Guid mailId, int createType)
        {
            using var connection = _databaseService.OpenConnection("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                _databaseService.ExecuteProcedure(
                    "up_insert_mail",
                    connection,
                    transaction,
                    ("@sender_auth_id", senderAuthId),
                    ("@sender_character_id", senderCharacterId),
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

        public void InsertMailItem(ItemData itemData, Guid? recipientAuthId, Guid? recipientCharacterId, Guid mailId, int slotIndex)
        {
            using var connection = _databaseService.OpenConnection("RustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                _databaseService.ExecuteNonQuery(
                    "INSERT INTO n_mailitem (item_uid, mail_uid, character_id, auth_id, page_index, slot_index, code, use_cnt, remain_time, create_time, update_time, gcode, durability, enhance_level, option_1_code, option_1_value, option_2_code, option_2_value, option_3_code, option_3_value, option_group, ReconNum, ReconState, socket_count, socket_1_code, socket_1_value, socket_2_code, socket_2_value, socket_3_code, socket_3_value, expire_time, lock_pwd, activity_value, link_id, is_seizure, socket_1_color, socket_2_color, socket_3_color, rank, acquireroute, physical, magical, durabilitymax, weight) " +
                    "VALUES (@item_id, @MailId, @character_id, @auth_id, @page_index, @slot_index, @code, @use_cnt, @remain_time, GETDATE(), GETDATE(), @gcode, @durability, @enhance_level, @option_1_code, @option_1_value, @option_2_code, @option_2_value, @option_3_code, @option_3_value, 0, @ReconNum, @ReconState, @socket_count, @socket_1_code, @socket_1_value, @socket_2_code, @socket_2_value, @socket_3_code, @socket_3_value, @expire_time, '', @activity_value, @link_id, 0, @socket_1_color, @socket_2_color, @socket_3_color, @rank, @acquireroute, @physical, @magical, @durabilitymax, @weight)",
                    connection,
                    transaction,
                    ("@character_id", recipientCharacterId),
                    ("@auth_id", recipientAuthId),
                    ("@item_id", Guid.NewGuid()),
                    ("@code", itemData.ID),
                    ("@use_cnt", itemData.Amount),
                    ("@remain_time", 0),
                    ("@gcode", 0),
                    ("@page_index", 61),
                    ("@slot_index", slotIndex),
                    ("@durability", itemData.Durability),
                    ("@enhance_level", itemData.EnchantLevel),
                    ("@option_1_code", itemData.RandomOption01),
                    ("@option_1_value", itemData.RandomOption01Value),
                    ("@option_2_code", itemData.RandomOption02),
                    ("@option_2_value", itemData.RandomOption02Value),
                    ("@option_3_code", itemData.RandomOption03),
                    ("@option_3_value", itemData.RandomOption03Value),
                    ("@ReconNum", itemData.Reconstruction),
                    ("@ReconState", itemData.ReconstructionMax),
                    ("@socket_count", itemData.SocketCount),
                    ("@socket_1_code", itemData.SocketOption01),
                    ("@socket_1_value", itemData.SocketOption01Value),
                    ("@socket_2_code", itemData.SocketOption02),
                    ("@socket_2_value", itemData.SocketOption02Value),
                    ("@socket_3_code", itemData.SocketOption03),
                    ("@socket_3_value", itemData.SocketOption03Value),
                    ("@socket_1_color", itemData.Socket01Color),
                    ("@socket_2_color", itemData.Socket02Color),
                    ("@socket_3_color", itemData.Socket03Color),
                    ("@expire_time", 0),
                    ("@MailId", mailId),
                    ("@activity_value", itemData.AugmentStone),
                    ("@link_id", "00000000-0000-0000-0000-000000000000"),
                    ("@rank", itemData.Rank),
                    ("@acquireroute", 0),
                    ("@physical", 0),
                    ("@magical", 0),
                    ("@durabilitymax", itemData.MaxDurability),
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
        public void GMAudit(string windyCode, Guid? characterId, string characterName, string action, string modify)
        {
            using var connection = _databaseService.OpenConnection("GMRustyHearts");
            using var transaction = connection.BeginTransaction();

            try
            {
                string changes = $"<font color=blue>{action}</font>]<br><font color=red>{modify}<br>{characterName}, GUID:{{{characterId.ToString().ToUpper()}}}<br></font>";

                _databaseService.ExecuteNonQuery("INSERT INTO GMAudit(audit_id, AdminID, world_index, bcust_id, character_id, char_name, Type, Modify, Memo, date) " +
                     "VALUES (@audit_id, @AdminID, @world_index, @bcust_id, @character_id, @char_name, @Type, @Modify, @Memo, @date)",
                     connection,
                     transaction,
                     ("@audit_id", Guid.NewGuid()),
                     ("@AdminID", "RHToolkit"),
                     ("@world_index", 1),
                     ("@bcust_id", windyCode),
                     ("@character_id", characterId),
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

        public void SanctionLog(Guid sanctionUid, Guid characterId, string windyCode, string characterName, DateTime startTime, DateTime? endTime, string reason)
        {
            using var connection = _databaseService.OpenConnection("RustyHearts_Log");
            using var transaction = connection.BeginTransaction();

            try
            {

                _databaseService.ExecuteNonQuery("INSERT INTO Sanction_Log(log_type, sanction_uid, world_id, bcust_id, item_uid, character_id, char_name, item_name, start_time, end_time, personnel, releaser, cause, comment, is_release, reg_date) " +
                                                         "VALUES (@log_type, @sanction_uid, @world_id, @bcust_id, @item_uid, @character_id, @char_name, @item_name, " +
                                                         "FORMAT(@start_time, 'M/d/yyyy h:mm:ss tt'), FORMAT(@end_time, 'M/d/yyyy h:mm:ss tt'), @personnel, @releaser, @cause, @comment, @is_release, @reg_date)",
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
                throw new Exception($"Error: {ex.Message}", ex);
            }
        }

        public void UpdateSanctionLog(Guid sanctionUid, string releaser, string comment, int isRelease)
        {
            using var connection = _databaseService.OpenConnection("RustyHearts_Log");
            using var transaction = connection.BeginTransaction();

            try
            {
                _databaseService.ExecuteNonQuery(
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
