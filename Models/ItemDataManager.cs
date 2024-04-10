using RHGMTool.Services;
using static RHGMTool.Models.EnumService;

namespace RHGMTool.Models
{
    public class ItemDataManager
    {
        public List<ItemData>? CachedItemDataList { get; private set; }

        private readonly SqLiteDatabaseService _databaseService;
        private readonly GMDatabaseService _gmDatabaseService;

        public ItemDataManager()
        {
            CachedItemDataList = null;
            _databaseService = new SqLiteDatabaseService();
            _gmDatabaseService = new GMDatabaseService(_databaseService);
            InitializeItemDataList();
        }

        public void InitializeItemDataList()
        {
            // Initialize CachedItemDataList
            CachedItemDataList ??=
            [
                // Fetch data for each item type and merge into CachedItemDataList
                .. _gmDatabaseService.GetItemDataList(ItemType.Item, "itemlist"),
                    .. _gmDatabaseService.GetItemDataList(ItemType.Costume, "itemlist_costume"),
                    .. _gmDatabaseService.GetItemDataList(ItemType.Armor, "itemlist_armor"),
                    .. _gmDatabaseService.GetItemDataList(ItemType.Weapon, "itemlist_weapon"),
                ];
        }

    }
}
