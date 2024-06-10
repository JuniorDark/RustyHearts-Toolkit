using Microsoft.Data.SqlClient;
using RHToolkit.Models;
using System.Data;

namespace RHToolkit.Services
{
    public interface IDatabaseService
    {
        Task AddCharacterTitleAsync(CharacterInfo characterInfo, int titleId, int remainTime, int expireTime);
        Task AddCouponAsync(string couponCode, ItemData itemData);
        Task<bool> CharacterHasSanctionAsync(Guid characterId);
        Task<bool> CharacterHasTitle(Guid characterId, int titleID);
        Task CharacterSanctionAsync(CharacterInfo characterInfo, EnumService.SanctionOperationType operationType, Guid sanctionUid, int sanctionKind, string reasonDetails, string releaser, string comment, int sanctionType, int sanctionPeriod, int sanctionCount);
        Task<bool> CouponExists(string couponCode);
        Task DeleteCharacterAsync(Guid authId, Guid characterId);
        Task DeleteCharacterTitleAsync(CharacterInfo characterInfo, Guid titleUid);
        Task DeleteCouponAsync(int couponNumber);
        Task EquipCharacterTitleAsync(CharacterInfo characterInfo, Guid titleId);
        Task<List<ItemData>> GetAccountItemList(Guid authId);
        Task<string[]> GetAllCharacterNamesAsync(string isConnect = "");
        Task<CharacterData?> GetCharacterDataAsync(string characterIdentifier);
        Task<List<CharacterData>> GetCharacterDataListAsync(string characterIdentifier, string isConnect = "", bool isDeletedCharacter = false);
        Task<(Guid? characterId, Guid? authid, string? accountName)> GetCharacterInfoAsync(string characterName);
        Task<bool> GetCharacterOnlineAsync(string characterName);
        Task<string?> GetGuildNameAsync(Guid guildId);
        Task<List<ItemData>> GetItemList(Guid characterId, string tableName);
        Task<(DateTime startTime, DateTime endTime)> GetSanctionTimesAsync(Guid sanctionUid);
        Task GMAuditAsync(string accountName, Guid? characterId, string characterName, string action, string auditMessage);
        Task InsertMailAsync(SqlConnection connection, SqlTransaction transaction, Guid? senderAuthId, Guid? senderCharacterId, string mailSender, string recipient, string content, int gold, int returnDay, int reqGold, Guid mailId, int createType);
        Task InsertMailItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData, Guid? recipientAuthId, Guid? recipientCharacterId, Guid mailId, int slotIndex);
        Task<bool> IsCharacterOnlineAsync(string characterName);
        Task<DataRow?> ReadCharacterEquipTitleAsync(Guid characterId);
        Task<DataRow?> ReadCharacterFortuneAsync(Guid characterId);
        Task<DataTable> ReadCharacterSanctionListAsync(Guid characterId);
        Task<DataTable?> ReadCharacterTitleListAsync(Guid characterId);
        Task<DataTable?> ReadCouponListAsync();
        Task RemoveFortuneAsync(CharacterInfo characterInfo, int fortuneState, int fortuneID1, int fortuneID2, int fortuneID3);
        Task RestoreCharacterAsync(Guid characterId);
        Task SanctionLogAsync(CharacterInfo characterInfo, Guid sanctionUid, DateTime startTime, DateTime endTime, string reason);
        Task<(List<string> successfulRecipients, List<string> failedRecipients)> SendMailAsync(string sender, string? message, int gold, int itemCharge, int returnDays, string[] recipients, List<ItemData> itemDataList);
        Task UnequipCharacterTitleAsync(CharacterInfo characterInfo, Guid titleId);
        Task UpdateCharacterClassAsync(Guid characterId, string accountName, string characterName, int currentCharacterClass, int newCharacterClass);
        Task UpdateCharacterDataAsync(NewCharacterData characterData, string action, string auditMessage);
        Task UpdateCharacterJobAsync(Guid characterId, string accountName, string characterName, int currentCharacterJob, int newCharacterJob);
        Task<int> UpdateCharacterNameAsync(Guid characterId, string characterName);
        Task UpdateFortuneAsync(CharacterInfo characterInfo, int fortune, int selectedFortuneID1, int selectedFortuneID2, int selectedFortuneID3);
        Task UpdateSanctionLogAsync(Guid sanctionUid, string releaser, string comment, int isRelease);
    }
}