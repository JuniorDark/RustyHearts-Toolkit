using RHToolkit.Services;

namespace RHToolkit.Models.SQLite
{
    /// <summary>
    /// Manages cached data for items, options, and skills.
    /// </summary>
    public class CachedDataManager
    {
        private readonly IGMDatabaseService _gmDatabaseService;
        public List<ItemData>? CachedItemDataList { get; private set; }
        public List<NameID>? CachedOptionItems { get; private set; }
        public List<SkillData>? CachedSkillDataList { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedDataManager"/> class.
        /// </summary>
        /// <param name="gmDatabaseService">The database service to use for fetching data.</param>
        public CachedDataManager(IGMDatabaseService gmDatabaseService)
        {
            _gmDatabaseService = gmDatabaseService;

            CachedItemDataList = null;
            CachedOptionItems = null;
            CachedSkillDataList = null;

            InitializeCachedLists();
        }

        /// <summary>
        /// Initializes the cached lists by fetching data from the database.
        /// </summary>
        public void InitializeCachedLists()
        {
            string dbFilePath = SqLiteDatabaseService.GetDatabaseFilePath();

            if (File.Exists(dbFilePath))
            {
                SqLiteDatabaseService.DbFilePath = dbFilePath;

                CachedItemDataList?.Clear();
                CachedOptionItems?.Clear();
                CachedSkillDataList?.Clear();

                // Initialize CachedItemDataList
                CachedItemDataList = _gmDatabaseService.GetItemDataLists();
                // Initialize CachedSkillItems
                var cachedSkillDataList = _gmDatabaseService.GetSkillDataLists();
                CachedSkillDataList = cachedSkillDataList;
                // Initialize CachedOptionItems
                CachedOptionItems = _gmDatabaseService.GetOptionItems();
            }
        }
    }
}