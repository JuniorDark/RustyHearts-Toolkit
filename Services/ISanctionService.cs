using System.Data;
using System.Data.SqlClient;

namespace RHGMTool.Services
{
    public interface ISanctionService
    {
        DataTable ReadCharacterSanctionList(SqlConnection connection, Guid characterId);
        Guid GetSanctionUid(SqlConnection connection, SqlTransaction transaction, Guid characterId);
        void GetSanctionTime(SqlConnection connection, SqlTransaction transaction, Guid sanctionUid, out DateTime? startTime, out DateTime? endTime);
        bool CharacterHasSanction(Guid characterId);
    }
}
