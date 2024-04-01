using RHGMTool.Models;
using System.Data;

namespace RHGMTool.Services
{
    public interface ICharacterService
    {
        CharacterData? GetCharacterData(IDbConnection connection, string characterName);
        bool IsCharacterOnline(IDbConnection connection, string characterName);
        string[] GetAllCharacterNames(IDbConnection connection);
        string[] GetAllOnlineCharacterNames(IDbConnection connection);
        public (Guid? characterId, Guid? authid, string? windyCode) GetCharacterInfo(IDbConnection connection, string characterName);
        string? GetGuildName(IDbConnection connection, Guid guildId);
        void UpdateCharacterData(IDbConnection connection, IDbTransaction transaction, Guid characterId, NewCharacterData characterData);
        int UpdateCharacterName(IDbConnection connection, IDbTransaction transaction, Guid characterId, string characterName);

        DataRow? ReadCharacterFortune(IDbConnection connection, Guid characterId);
        void UpdateFortune(IDbConnection connection, IDbTransaction transaction, Guid characterId, int fortune, int selectedFortuneID1, int selectedFortuneID2, int selectedFortuneID3);
        void RemoveFortune(IDbConnection connection, IDbTransaction transaction, Guid characterId, int fortuneState);

        DataTable ReadCharacterSanctionList(IDbConnection connection, Guid characterId);
        (DateTime startTime, DateTime? endTime) GetSanctionTimes(IDbConnection connection, IDbTransaction transaction, Guid sanctionUid);
        bool CharacterHasSanction(IDbConnection connection, Guid characterId);
        (Guid SanctionUid, bool IsInsert) CharacterSanction(IDbConnection connection, IDbTransaction transaction, Guid characterId, Guid sanctionUid, int sanctionKind, string releaser, string comment, int sanctionType, int sanctionPeriod, int sanctionCount);
    }
}
