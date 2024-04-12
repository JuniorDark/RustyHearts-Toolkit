using RHGMTool.Services;
using static RHGMTool.Models.EnumService;

namespace RHGMTool.Models
{
    public class ItemDataManager
    {
        private static readonly Lazy<ItemDataManager> _instance = new(() => new ItemDataManager());

        public static ItemDataManager Instance => _instance.Value;

        public List<ItemData>? CachedItemDataList { get; private set; }
        public List<NameID>? CachedOptionItems { get; private set; }

        private readonly SqLiteDatabaseService _databaseService;
        private readonly GMDatabaseService _gmDatabaseService;

        private ItemDataManager()
        {
            CachedItemDataList = null;
            _databaseService = new SqLiteDatabaseService();
            _gmDatabaseService = new GMDatabaseService(_databaseService);
            InitializeCachedLists();
        }

        public void InitializeCachedLists()
        {
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
            CachedOptionItems = new List<NameID>(_gmDatabaseService.GetOptionItems());
        }
    }

}
