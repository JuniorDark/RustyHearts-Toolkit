using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models.Database
{
    public class ItemDataManager
    {
        private readonly IGMDatabaseService _gmDatabaseService;
        public List<ItemData>? CachedItemDataList { get; private set; }
        public List<NameID>? CachedOptionItems { get; private set; }

        public ItemDataManager(IGMDatabaseService gmDatabaseService)
        {
            _gmDatabaseService = gmDatabaseService;

            CachedItemDataList = null;
            CachedOptionItems = null;

            InitializeCachedLists();
        }

        public static bool GetDatabaseFilePath()
        {
            string resourcesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
            string dbFilePath = Path.Combine(resourcesFolder, "gmdb.db");

            if (File.Exists(dbFilePath))
            {
                return true;
            }

            RHMessageBox.ShowOKMessage($"Database file {dbFilePath} not found.", "Database Not Found");
            return false;
        }

        public void InitializeCachedLists()
        {
            if (GetDatabaseFilePath())
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

}
