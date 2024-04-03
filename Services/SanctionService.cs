using System.Data;
using System.Data.SqlClient;
using System.Windows;
using static RHGMTool.Models.EnumService;

namespace RHGMTool.Services
{
    public class SanctionService(ISqlDatabaseService databaseService) : ISanctionService
    {
        private readonly ISqlDatabaseService _databaseService = databaseService;

        #region Sanction
        public DataTable ReadCharacterSanctionList(SqlConnection connection, Guid characterId)
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

                using SqlCommand sanctionCommand = new(selectQuery, connection);
                sanctionCommand.Parameters.AddWithValue("@character_id", characterId);

                using SqlDataReader sanctionReader = sanctionCommand.ExecuteReader();

                while (sanctionReader.Read())
                {
                    Guid sanctionUid = sanctionReader.GetGuid(0);
                    byte sanctionType = sanctionReader.GetByte(2);
                    byte sanctionCount = sanctionReader.GetByte(3);
                    byte sanctionPeriod = sanctionReader.GetByte(4);

                    DateTime startDate = sanctionReader.GetDateTime(6);
                    DateTime? releaseDate = !sanctionReader.IsDBNull(7) ? sanctionReader.GetDateTime(7) : null;
                    string addedBy = sanctionReader.GetString(8);
                    string removedBy = sanctionReader.GetString(9);
                    string comment = sanctionReader.GetString(10);

                    byte isApply = sanctionReader.GetByte(11);

                    string sanctionTypeName = GetEnumDescription((SanctionType)sanctionType);
                    string sanctionCountName = GetEnumDescription((SanctionCount)sanctionCount);
                    string sanctionPeriodName = GetEnumDescription((SanctionPeriod)sanctionPeriod);
                    string reason = $"{sanctionTypeName}|{sanctionCountName}|{sanctionPeriodName}";

                    dataTable.Rows.Add(sanctionUid, startDate.ToString(), releaseDate?.ToString(), reason, addedBy, removedBy, comment, isApply);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading sanction: {ex.Message}", "Error");
            }

            return dataTable;
        }

        public Guid GetSanctionUid(SqlConnection connection, SqlTransaction transaction, Guid characterId)
        {
            using SqlCommand getSanctionUidCommand = new("SELECT TOP 1 uid FROM Character_Sanction WHERE character_id = @character_id ORDER BY reg_date DESC", connection, transaction);
            getSanctionUidCommand.Parameters.AddWithValue("@character_id", characterId);
            object result = getSanctionUidCommand.ExecuteScalar();
            return result != null && result != DBNull.Value ? (Guid)result : Guid.Empty;
        }

        public void GetSanctionTime(SqlConnection connection, SqlTransaction transaction, Guid sanctionUid, out DateTime? startTime, out DateTime? endTime)
        {
            startTime = null;
            endTime = null;
            DateTime maxDate = DateTime.MaxValue;

            using SqlCommand getTimeCommand = new("SELECT start_time, end_time FROM Character_Sanction WHERE uid = @sanction_uid", connection, transaction);
            getTimeCommand.Parameters.AddWithValue("@sanction_uid", sanctionUid);

            using SqlDataReader reader = getTimeCommand.ExecuteReader();
            if (reader.Read())
            {
                startTime = reader.IsDBNull(0) ? null : reader.GetDateTime(0);
                endTime = reader.IsDBNull(1) ? maxDate : reader.GetDateTime(1);
            }
        }

        public bool CharacterHasSanction(Guid characterId)
        {
            bool hasSanction = false;

            try
            {
                using var connection = SqlDatabaseConnection.OpenSqlConnection("RustyHearts");
                string query = "SELECT COUNT(*) FROM Character_Sanction WHERE [character_id] = @character_id AND [is_apply] = 1";

                using SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@character_id", characterId);

                int sanctionCount = (int)command.ExecuteScalar();

                if (sanctionCount > 0)
                {
                    hasSanction = true;
                }
            }
            catch (Exception)
            {
                throw;
            }

            return hasSanction;
        }
        #endregion
    }
}
