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
        public CharacterData? GetCharacterData(IDbConnection connection, string characterName)
        {
            string selectQuery = "SELECT * FROM CharacterTable WHERE [Name] = @name";
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
                    characterData.GuildName = GetGuildName(connection, (Guid)row["guildid"]);
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

        public void UpdateCharacterData(IDbConnection connection, IDbTransaction transaction, Guid characterId, NewCharacterData characterData)
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
        }

        public int UpdateCharacterName(IDbConnection connection, IDbTransaction transaction, Guid characterId, string characterName)
        {
            return (int)_databaseService.ExecuteProcedure(
                    "up_update_character_name",
                    connection,
                    transaction,
                    ("@character_id", characterId),
                    ("@character_name", characterName)
                );
        }

        #region Read
        public bool IsCharacterOnline(IDbConnection connection, string characterName)
        {
            string selectQuery = "SELECT IsConnect FROM CharacterTable WHERE name = @characterName";
            var parameters = new (string, object)[] { ("@characterName", characterName) };

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

        public string[] GetAllCharacterNames(IDbConnection connection)
        {
            List<string> characterNames = [];

            string query = "SELECT name FROM CharacterTable";

            DataTable dataTable = _databaseService.ExecuteDataQuery(query, connection);

            foreach (DataRow row in dataTable.Rows)
            {
                string characterName = row["name"]?.ToString() ?? string.Empty;
                characterNames.Add(characterName);
            }

            return [.. characterNames];
        }

        public string[] GetAllOnlineCharacterNames(IDbConnection connection)
        {
            List<string> characterNames = [];

            string query = "SELECT name FROM CharacterTable WHERE IsConnect = 'Y'";

            DataTable dataTable = _databaseService.ExecuteDataQuery(query, connection);

            foreach (DataRow row in dataTable.Rows)
            {
                string characterName = row["name"]?.ToString() ?? string.Empty;
                characterNames.Add(characterName);
            }

            return [.. characterNames];
        }

        public (Guid? characterId, Guid? authid, string? windyCode) GetCharacterInfo(IDbConnection connection, string characterName)
        {
            string selectQuery = "SELECT character_id, authid, bcust_id FROM CharacterTable WHERE name = @characterName";
            var parameters = new (string, object)[] { ("@characterName", characterName) };

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

        public DataRow? ReadCharacterFortune(IDbConnection connection, Guid characterId)
        {
            DataTable dataTable = _databaseService.ExecuteDataProcedure("up_read_fortune", connection, null, ("@character_id", characterId));

            return dataTable.Rows.Count > 0 ? dataTable.Rows[0] : null;
        }

        public void UpdateFortune(IDbConnection connection, IDbTransaction transaction, Guid characterId, int fortune, int selectedFortuneID1, int selectedFortuneID2, int selectedFortuneID3)
        {
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
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating fortune: {ex.Message}", ex);
            }
        }

        public void RemoveFortune(IDbConnection connection, IDbTransaction transaction, Guid characterId, int fortuneState)
        {
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
            }
            catch (Exception ex)
            {
                throw new Exception($"Error removing fortune: {ex.Message}", ex);
            }
        }
        #endregion

        #region Sanction

        public DataTable ReadCharacterSanctionList(IDbConnection connection, Guid characterId)
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

        public (DateTime startTime, DateTime? endTime) GetSanctionTimes(IDbConnection connection, IDbTransaction transaction, Guid sanctionUid)
        {
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

        public bool CharacterHasSanction(IDbConnection connection, Guid characterId)
        {
            int sanctionCount = (int)_databaseService.ExecuteScalar(
                "SELECT COUNT(*) FROM Character_Sanction WHERE [character_id] = @character_id AND [is_apply] = 1",
                connection,
                ("@character_id", characterId)
            );

            return sanctionCount > 0;
        }

        public (Guid SanctionUid, bool IsInsert) CharacterSanction(IDbConnection connection, IDbTransaction transaction, Guid characterId, Guid sanctionUid, int sanctionKind, string releaser, string comment, int sanctionType, int sanctionPeriod, int sanctionCount)
        {
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
        public string? GetGuildName(IDbConnection connection, Guid guildId)
        {
            string selectQuery = "SELECT name FROM GuildTable WHERE guild_id = @guildId";
            var parameters = new (string, object)[] { ("@guildId", guildId) };

            object result = _databaseService.ExecuteScalar(selectQuery, connection, parameters);
            return result != null ? result.ToString() : "No Guild";
        }
        #endregion

        #endregion

        #region RustyHearts_Log
        public void GMAudit(IDbConnection connection, IDbTransaction transaction, string windyCode, Guid characterId, string characterName, string action, string modify)
        {
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                connection.ChangeDatabase("GMRustyHearts");

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
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void SanctionLog(IDbConnection connection, IDbTransaction transaction, Guid sanctionUid, Guid characterId, string windyCode, string characterName, DateTime startTime, DateTime? endTime, string reason)
        {
            try
            {
                connection.ChangeDatabase("RustyHearts_Log");

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
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void UpdateSanctionLog(IDbConnection connection, IDbTransaction transaction, Guid sanctionUid, string releaser, string comment, int isRelease)
        {
            connection.ChangeDatabase("RustyHearts_Log");

            _databaseService.ExecuteNonQuery(
                    "UPDATE Sanction_Log SET releaser = @releaser, comment = @comment, is_release = @is_release WHERE sanction_uid = @sanction_uid",
                    connection,
                    transaction,
                     ("@sanction_uid", sanctionUid),
                     ("@releaser", releaser),
                     ("@comment", comment),
                     ("@is_release", isRelease)
                );
        }
        #endregion

        #region RustyHeats_Auth

        #endregion

        #region RustyHeats_Account

        #endregion
    }

}
