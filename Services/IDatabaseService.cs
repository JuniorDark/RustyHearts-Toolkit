using RHToolkit.Models;
using System.Data;

namespace RHToolkit.Services
{
    public interface IDatabaseService
    {
        bool CharacterHasSanction(Guid characterId);
        (Guid SanctionUid, bool IsInsert) CharacterSanction(Guid characterId, Guid sanctionUid, int sanctionKind, string releaser, string comment, int sanctionType, int sanctionPeriod, int sanctionCount);
        string[] GetAllCharacterNames();
        string[] GetAllOnlineCharacterNames();
        CharacterData? GetCharacterData(string characterName);
        (Guid? characterId, Guid? authid, string? windyCode) GetCharacterInfo(string characterName);
        string? GetGuildName(Guid guildId);
        (DateTime startTime, DateTime? endTime) GetSanctionTimes(Guid sanctionUid);
        void GMAudit(string windyCode, Guid? characterId, string characterName, string action, string modify);
        void InsertMail(Guid? senderAuthId, Guid? senderCharacterId, string mailSender, string recipient, string content, int gold, int returnDay, int reqGold, Guid mailId, int createType);
        void InsertMailItem(ItemData itemData, Guid? recipientAuthId, Guid? recipientCharacterId, Guid mailId, int slotIndex);
        bool IsCharacterOnline(string characterName);
        DataRow? ReadCharacterFortune(Guid characterId);
        DataTable ReadCharacterSanctionList(Guid characterId);
        void RemoveFortune(Guid characterId, int fortuneState);
        void SanctionLog(Guid sanctionUid, Guid characterId, string windyCode, string characterName, DateTime startTime, DateTime? endTime, string reason);
        void UpdateCharacterData(Guid characterId, NewCharacterData characterData);
        int UpdateCharacterName(Guid characterId, string characterName);
        void UpdateFortune(Guid characterId, int fortune, int selectedFortuneID1, int selectedFortuneID2, int selectedFortuneID3);
        void UpdateSanctionLog(Guid sanctionUid, string releaser, string comment, int isRelease);
    }
}