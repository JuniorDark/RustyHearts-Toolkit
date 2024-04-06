using RHGMTool.Services;
using static RHGMTool.Models.EnumService;

namespace RHGMTool.Models
{
    public class ItemDataManager
    {
        public List<ItemData>? CachedItemDataList { get; private set; }

        private readonly SqLiteDatabaseService _databaseService;
        private readonly GMDbService _gMDbService;

        public ItemDataManager()
        {
            CachedItemDataList = null;
            _databaseService = new SqLiteDatabaseService();
            _gMDbService = new GMDbService(_databaseService);
            InitializeItemDataList();
        }

        public void InitializeItemDataList()
        {
            // Initialize CachedItemDataList
            CachedItemDataList ??=
            [
                // Fetch data for each item type and merge into CachedItemDataList
                .. _gMDbService.GetItemDataList(ItemType.Item, "itemlist"),
                    .. _gMDbService.GetItemDataList(ItemType.Costume, "itemlist_costume"),
                    .. _gMDbService.GetItemDataList(ItemType.Armor, "itemlist_armor"),
                    .. _gMDbService.GetItemDataList(ItemType.Weapon, "itemlist_weapon"),
                ];
        }

    }
}
