using System.Data;

namespace RHGMTool.Services
{
    public interface ILogService
    {
        void GMAudit(IDbConnection connection, IDbTransaction transaction, string windyCode, Guid characterId, string characterName, string action, string modify);
        void SanctionLog(IDbConnection connection, IDbTransaction transaction, Guid sanctionUid, Guid characterId, string windyCode, string characterName, DateTime startTime, DateTime? endTime, string reason);
        void UpdateSanctionLog(IDbConnection connection, IDbTransaction transaction, Guid sanctionUid, string releaser, string comment, int isRelease);
    }
}
