using RHToolkit.Services;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models.SQLite
{
    public class CachedDataManager
    {
        private readonly IGMDatabaseService _gmDatabaseService;
        public List<ItemData>? CachedItemDataList { get; private set; }
        public List<NameID>? CachedOptionItems { get; private set; }

        public CachedDataManager(IGMDatabaseService gmDatabaseService)
        {
            _gmDatabaseService = gmDatabaseService;

            CachedItemDataList = null;
            CachedOptionItems = null;

            InitializeCachedLists();
        }

        public void InitializeCachedLists()
        {
            string dbFilePath = SqLiteDatabaseService.GetDatabaseFilePath();

            if (File.Exists(dbFilePath))
            {
                SqLiteDatabaseService.DbFilePath = dbFilePath;

                CachedItemDataList?.Clear();
                CachedOptionItems?.Clear();

                // Initialize CachedItemDataList
                CachedItemDataList =
                    [
                        // Fetch data for each item type and merge into CachedItemDataList
                        .. _gmDatabaseService.GetItemDataList(ItemType.Item, "itemlist"),
                .. _gmDatabaseService.GetItemDataList(ItemType.Costume, "itemlist_costume"),
                .. _gmDatabaseService.GetItemDataList(ItemType.Armor, "itemlist_armor"),
                .. _gmDatabaseService.GetItemDataList(ItemType.Weapon, "itemlist_weapon"),
            ];

                // Initialize CachedOptionItems
                CachedOptionItems = _gmDatabaseService.GetOptionItems();
            }
        }
    }

}
