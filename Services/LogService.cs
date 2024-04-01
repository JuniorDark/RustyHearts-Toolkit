using System.Data;

namespace RHGMTool.Services
{
    public class LogService(ISqlDatabaseService databaseService) : ILogService
    {
        private readonly ISqlDatabaseService _databaseService = databaseService;

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
                     ("@AdminID", "RHGMTool"),
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
                     ("@personnel", "RHGMTool"),
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

    }

}
