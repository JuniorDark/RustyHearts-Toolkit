using Microsoft.Data.SqlClient;
using RHToolkit.Models;
using System.Data;

namespace RHToolkit.Services
{
    public interface IDatabaseService
    {
        Task AddCharacterTitleAsync(CharacterData characterData, int titleId, int remainTime, int expireTime);
        Task AddCouponAsync(string couponCode, DateTime validDate, ItemData itemData);
        Task<bool> CharacterHasSanctionAsync(Guid characterId);
        Task<bool> CharacterHasTitle(Guid characterId, int titleID);
        Task CharacterSanctionAsync(CharacterData characterData, EnumService.SanctionOperationType operationType, Guid sanctionUid, int sanctionKind, string reasonDetails, string releaser, string comment, int sanctionType, int sanctionPeriod, int sanctionCount);
        Task<bool> CouponExists(string couponCode);
        Task DeleteCharacterAsync(Guid authId, Guid characterId);
        Task DeleteCharacterTitleAsync(CharacterData characterData, Guid titleUid);
        Task DeleteCouponAsync(int couponNumber);
        Task DeleteInventoryItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData, string tableName);
        Task EquipCharacterTitleAsync(CharacterData characterData, Guid titleId);
        Task<long> GetAccountCashAsync(string accountName);
        Task<int> GetAccountCashMileageAsync(string accountName);
        Task<AccountData?> GetAccountDataAsync(string accountIdentifier);
        Task<DataTable?> GetAccountInfoAsync(string accountName);
        Task<string[]> GetAllCharacterNamesAsync(string isConnect = "");
        Task<CharacterData?> GetCharacterDataAsync(string characterIdentifier);
        Task<List<CharacterData>> GetCharacterDataListAsync(string characterIdentifier, string isConnect = "", bool isDeletedCharacter = false);
        Task<(Guid? characterId, Guid? authid, string? accountName)> GetCharacterInfoAsync(string characterName);
        Task<bool> GetCharacterOnlineAsync(string characterName);
        Task<string?> GetGuildNameAsync(Guid guildId);
        Task<List<ItemData>> GetItemList(Guid characterId, string tableName);
        Task<(DateTime startTime, DateTime endTime)> GetSanctionTimesAsync(Guid sanctionUid);
        Task<DataRow?> GetUniAccountInfoAsync(Guid authId);
        Task GMAuditAsync(CharacterData characterData, string action, string auditMessage);
        Task GMAuditMailAsync(string accountName, Guid? characterId, string characterName, string action, string auditMessage);
        Task InsertInventoryDeleteItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData);
        Task InsertInventoryItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData, string tableName);
        Task InsertMailAsync(SqlConnection connection, SqlTransaction transaction, Guid? senderAuthId, Guid? senderCharacterId, string mailSender, string recipient, string content, int gold, int returnDay, int reqGold, Guid mailId, int createType);
        Task InsertMailItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData, Guid? recipientAuthId, Guid? recipientCharacterId, Guid mailId, int slotIndex);
        Task InsertPetInventoryItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData, Guid petId);
        Task<bool> IsCharacterOnlineAsync(string characterName);
        Task<DataRow?> ReadCharacterEquipTitleAsync(Guid characterId);
        Task<DataRow?> ReadCharacterFortuneAsync(Guid characterId);
        Task<DataTable> ReadCharacterSanctionListAsync(Guid characterId);
        Task<DataTable?> ReadCharacterTitleListAsync(Guid characterId);
        Task<DataTable?> ReadCouponListAsync();
        Task RemoveFortuneAsync(CharacterData characterData, int fortuneState, int fortuneID1, int fortuneID2, int fortuneID3);
        Task RestoreCharacterAsync(Guid characterId);
        Task SanctionLogAsync(CharacterData characterData, Guid sanctionUid, DateTime startTime, DateTime endTime, string reason);
        Task SaveInventoryItem(CharacterData characterData, List<ItemData>? itemDataList, List<ItemData>? deletedItemDataList, string tableName);
        Task<(List<string> successfulRecipients, List<string> failedRecipients)> SendMailAsync(string sender, string? message, int gold, int itemCharge, int returnDays, string[] recipients, List<ItemData> itemDataList);
        Task UnequipCharacterTitleAsync(CharacterData characterData, Guid titleId);
        Task UpdateCharacterClassAsync(CharacterData characterData, int newCharacterClass);
        Task UpdateCharacterDataAsync(NewCharacterData characterData);
        Task UpdateCharacterJobAsync(CharacterData characterData, int newCharacterJob);
        Task<int> UpdateCharacterNameAsync(Guid characterId, string characterName);
        Task UpdateFortuneAsync(CharacterData characterData, int fortune, int selectedFortuneID1, int selectedFortuneID2, int selectedFortuneID3);
        Task UpdateInventoryItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData, string tableName);
        Task UpdatePetInventoryItemAsync(SqlConnection connection, SqlTransaction transaction, ItemData itemData);
        Task UpdateSanctionLogAsync(Guid sanctionUid, string releaser, string comment, int isRelease);
    }
}