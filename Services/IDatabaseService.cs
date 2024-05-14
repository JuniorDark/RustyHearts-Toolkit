using RHToolkit.Models;
using System.Data;

namespace RHToolkit.Services
{
    public interface IDatabaseService
    {
        Task AddCharacterTitleAsync(Guid characterId, int titleId, int remainTime, int expireTime);
        Task<bool> CharacterHasSanctionAsync(Guid characterId);
        Task<bool> CharacterHasTitle(Guid characterId, int titleID);
        Task<(Guid SanctionUid, bool IsInsert)> CharacterSanctionAsync(Guid characterId, Guid sanctionUid, int sanctionKind, string releaser, string comment, int sanctionType, int sanctionPeriod, int sanctionCount);
        Task EquipCharacterTitleAsync(Guid characterId, Guid titleId);
        Task<List<ItemData>> GetAccountItemList(Guid authId);
        Task<string[]> GetAllCharacterNamesAsync();
        Task<string[]> GetAllOnlineCharacterNamesAsync();
        Task<CharacterData?> GetCharacterDataAsync(string characterName);
        Task<(Guid characterId, Guid authid, string? windyCode)> GetCharacterInfoAsync(string characterName);
        Task<string?> GetGuildNameAsync(Guid guildId);
        Task<List<ItemData>> GetItemList(Guid characterId, string tableName);
        Task<(DateTime startTime, DateTime? endTime)> GetSanctionTimesAsync(Guid sanctionUid);
        Task GMAuditAsync(string windyCode, Guid characterId, string characterName, string action, string modify);
        Task InsertMailAsync(Guid senderAuthId, Guid senderCharacterId, string mailSender, string recipient, string content, int gold, int returnDay, int reqGold, Guid mailId, int createType);
        Task InsertMailItemAsync(ItemData itemData, Guid recipientAuthId, Guid recipientCharacterId, Guid mailId, int slotIndex);
        Task<bool> IsCharacterOnlineAsync(string characterName);
        Task<DataRow?> ReadCharacterEquipTitleAsync(Guid characterId);
        Task<DataRow?> ReadCharacterFortuneAsync(Guid characterId);
        Task<DataTable> ReadCharacterSanctionListAsync(Guid characterId);
        Task<DataTable?> ReadCharacterTitleListAsync(Guid characterId);
        Task RemoveCharacterTitleAsync(Guid characterId, Guid titleUid);
        Task RemoveFortuneAsync(Guid characterId, int fortuneState);
        Task SanctionLogAsync(Guid sanctionUid, Guid characterId, string windyCode, string characterName, DateTime startTime, DateTime? endTime, string reason);
        Task UnequipCharacterTitleAsync(Guid titleId);
        Task UpdateCharacterDataAsync(Guid characterId, NewCharacterData characterData);
        Task<int> UpdateCharacterNameAsync(Guid characterId, string characterName);
        Task UpdateFortuneAsync(Guid characterId, int fortune, int selectedFortuneID1, int selectedFortuneID2, int selectedFortuneID3);
        Task UpdateSanctionLogAsync(Guid sanctionUid, string releaser, string comment, int isRelease);
    }
}