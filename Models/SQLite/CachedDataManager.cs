using RHToolkit.Services;

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
                CachedItemDataList = _gmDatabaseService.GetItemDataLists();
                // Initialize CachedOptionItems
                CachedOptionItems = _gmDatabaseService.GetOptionItems();
            }
        }
    }

}
