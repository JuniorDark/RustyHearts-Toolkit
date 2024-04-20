using RHToolkit.Models;
using System.Data;

namespace RHToolkit.Services
{
    public interface IDatabaseService
    {
        bool CharacterHasSanction(IDbConnection connection, Guid characterId);
        (Guid SanctionUid, bool IsInsert) CharacterSanction(IDbConnection connection, IDbTransaction transaction, Guid characterId, Guid sanctionUid, int sanctionKind, string releaser, string comment, int sanctionType, int sanctionPeriod, int sanctionCount);
        string[] GetAllCharacterNames(IDbConnection connection);
        string[] GetAllOnlineCharacterNames(IDbConnection connection);
        CharacterData? GetCharacterData(IDbConnection connection, string characterName);
        (Guid? characterId, Guid? authid, string? windyCode) GetCharacterInfo(IDbConnection connection, string characterName);
        string? GetGuildName(IDbConnection connection, Guid guildId);
        (DateTime startTime, DateTime? endTime) GetSanctionTimes(IDbConnection connection, IDbTransaction transaction, Guid sanctionUid);
        void GMAudit(IDbConnection connection, IDbTransaction transaction, string windyCode, Guid characterId, string characterName, string action, string modify);
        bool IsCharacterOnline(IDbConnection connection, string characterName);
        DataRow? ReadCharacterFortune(IDbConnection connection, Guid characterId);
        DataTable ReadCharacterSanctionList(IDbConnection connection, Guid characterId);
        void RemoveFortune(IDbConnection connection, IDbTransaction transaction, Guid characterId, int fortuneState);
        void SanctionLog(IDbConnection connection, IDbTransaction transaction, Guid sanctionUid, Guid characterId, string windyCode, string characterName, DateTime startTime, DateTime? endTime, string reason);
        void UpdateCharacterData(IDbConnection connection, IDbTransaction transaction, Guid characterId, NewCharacterData characterData);
        int UpdateCharacterName(IDbConnection connection, IDbTransaction transaction, Guid characterId, string characterName);
        void UpdateFortune(IDbConnection connection, IDbTransaction transaction, Guid characterId, int fortune, int selectedFortuneID1, int selectedFortuneID2, int selectedFortuneID3);
        void UpdateSanctionLog(IDbConnection connection, IDbTransaction transaction, Guid sanctionUid, string releaser, string comment, int isRelease);
    }
}