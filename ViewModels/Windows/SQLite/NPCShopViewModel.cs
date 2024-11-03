using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Services;
using RHToolkit.ViewModels.Windows.Tools.VM;

namespace RHToolkit.ViewModels.Windows
{
    public partial class NPCShopViewModel : ObservableObject, IRecipient<NpcShopMessage>
    {
        private readonly IGMDatabaseService _gmDatabaseService;

        public NPCShopViewModel(IGMDatabaseService gmDatabaseService, ItemDataManager itemDataManager)
        {
            _gmDatabaseService = gmDatabaseService;
            _itemDataManager = itemDataManager;
            PopulateListItems();

            WeakReferenceMessenger.Default.Register(this);
        }

        #region NpcShopMessage
        public void Receive(NpcShopMessage message)
        {
            var shopID = message.Value;

            if (message.TabName != null)
            {
                var tabName = _gmDatabaseService.GetString(message.TabName.ID);
                TabName = tabName;
            }

            ShopID = shopID.ID;
            UpdateSelectedItem(shopID);
        }

        private void UpdateSelectedItem(NameID shopID)
        {
            switch (shopID.Type)
            {
                case "NpcShop":
                    UpdateNpcShop(shopID.ID);
                    break;
                case "TradeShop":
                    UpdateTradeShop(shopID.ID);
                    break;
            }
        }

        #region NpcShop

        private void UpdateNpcShop(int shopID)
        {
            NpcShopItems ??= [];

            var items = _gmDatabaseService.GetNpcShopItems(shopID);

            for (int i = 0; i < items.Count; i++)
            {
                var itemCode = items[i];
                var itemDataViewModel = ItemDataManager.GetItemDataViewModel(itemCode, i, 1);

                if (i < NpcShopItems.Count)
                {
                    var existingItem = NpcShopItems[i];

                    existingItem.ItemCode = itemCode;
                    existingItem.ItemDataViewModel = itemDataViewModel;
                }
                else
                {
                    var item = new NPCShopItem
                    {
                        ItemCode = itemCode,
                        ItemDataViewModel = itemDataViewModel
                    };

                    NpcShopItems.Add(item);
                }
            }
        }

        #endregion

        #region TradeShop

        private void UpdateTradeShop(int shopID)
        {
            NpcShopItems?.Clear();

            NpcShopItems = [];

            var items = _gmDatabaseService.GetTradeShopItems(shopID);

            for (int i = 0; i < items.Count; i++)
            {
                var item = new NPCShopItem
                {
                    ItemCode = items[i],
                    ItemDataViewModel = ItemDataManager.GetItemDataViewModel(items[i], i, 1)
                };

                NpcShopItems.Add(item);
            }
        }

        #endregion

        #endregion

        #region Comboboxes

        [ObservableProperty]
        private List<NameID>? _npcShopsItems;

        private void PopulateListItems()
        {
            NpcShopsItems = _gmDatabaseService.GetNpcShopsItems();
        }

        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = Resources.NPCShop;

        [ObservableProperty]
        private string? _openMessage = Resources.OpenFile;

        [ObservableProperty]
        private string? _tabName = "Tab";

        [ObservableProperty]
        private ItemDataManager _itemDataManager;

        #region SelectedItem

        [ObservableProperty]
        private ObservableCollection<NPCShopItem> _npcShopItems = [];

        [ObservableProperty]
        private NameID? _selectedShop;
        partial void OnSelectedShopChanged(NameID? value)
        {
            if (value != null)
            {
                UpdateSelectedItem(value);
            }
        }

        [ObservableProperty]
        private int _shopID;

        #endregion

        #endregion

    }
}
