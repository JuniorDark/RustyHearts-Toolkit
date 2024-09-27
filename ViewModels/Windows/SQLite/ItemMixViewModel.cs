using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using System.ComponentModel;

namespace RHToolkit.ViewModels.Windows
{
    public partial class ItemMixViewModel : ObservableObject, IRecipient<ItemMixMessage>
    {
        private readonly IGMDatabaseService _gmDatabaseService;
        private readonly System.Timers.Timer _itemMixFilterUpdateTimer;

        public ItemMixViewModel(IGMDatabaseService gmDatabaseService, ItemDataManager itemDataManager)
        {
            _gmDatabaseService = gmDatabaseService;
            _itemDataManager = itemDataManager;
            _itemMixFilterUpdateTimer = new()
            {
                Interval = 400,
                AutoReset = false
            };
            _itemMixFilterUpdateTimer.Elapsed += ItemMixFilterUpdateTimerElapsed;
            PopulateItemMixItems();
            _itemMixDataView = new CollectionViewSource { Source = ItemMixItems }.View;
            _itemMixDataView.Filter = FilterItems;

            WeakReferenceMessenger.Default.Register(this);
        }

        #region Commands

        [RelayCommand]
        private void AddFilterItem(string parameter)
        {
            try
            {
                SearchText = parameter;
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region ItemMixMessage

        public void Receive(ItemMixMessage message)
        {
            var group = message.Value;

            SelectedGroup = group;
        }

        #endregion

        #region SelectedItem
        private void UpdateItemMixMaterial(ItemMixData? selectedItem)
        {
            ItemMixMaterialsItems ??= [];

            if (selectedItem != null)
            {
                ItemMixMaterialsItems = [];

                SelectedItemDataViewModel = ItemDataManager.GetItemDataViewModel(selectedItem.ID, 0, 1);

                for (int i = 0; i < 5; i++)
                {
                    var itemCodeProperty = typeof(ItemMixData).GetProperty($"ItemCode0{i}");
                    var itemCountProperty = typeof(ItemMixData).GetProperty($"ItemCount0{i}");

                    int itemCode = (int)(itemCodeProperty?.GetValue(selectedItem) ?? 0);
                    int itemCount = (int)(itemCountProperty?.GetValue(selectedItem) ?? 0);

                    var itemMaterial = ItemDataManager.GetItemDataViewModel(itemCode, i, itemCount);

                    if (i < ItemMixMaterialsItems.Count)
                    {
                        ItemMixMaterialsItems[i] = itemMaterial!;
                    }
                    else
                    {
                        ItemMixMaterialsItems.Add(itemMaterial!);
                    }
                }

                IsSelectedItemVisible = Visibility.Visible;
            }
            else
            {
                IsSelectedItemVisible = Visibility.Hidden;
            }
        }

        #endregion

        #region CollectionView Filter
        [ObservableProperty]
        private ICollectionView _itemMixDataView;

        private bool FilterItems(object obj)
        {
            if (obj is ItemMixData item)
            {
                // Search filtering
                if (!string.IsNullOrEmpty(SearchText))
                {
                    string searchText = SearchText.ToLower();

                    // Check if any of the fields (ID, ItemName, ItemCode00 to ItemCode04) contain the search text
                    if (item.ID.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                        (!string.IsNullOrEmpty(item.ItemName) && item.ItemName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)) ||
                        item.ItemCode00.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                        item.ItemCode01.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                        item.ItemCode02.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                        item.ItemCode03.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                        item.ItemCode04.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }

                    return false;
                }

                if (FilterGroups != null && FilterGroups.Count > 0)
                {
                    if (item.MixGroup == null || !FilterGroups.Contains(item.MixGroup))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }


        private void RefreshView()
        {
            Application.Current.Dispatcher.Invoke(ItemMixDataView.Refresh);
        }

        [ObservableProperty]
        private string? _searchText;
        partial void OnSearchTextChanged(string? value)
        {
            _itemMixFilterUpdateTimer.Stop();
            _itemMixFilterUpdateTimer.Start();
        }

        private void ItemMixFilterUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            RefreshView();
        }

        #endregion

        #region Lists
        [ObservableProperty]
        private List<NameID>? _itemMixGroupItems;

        private void PopulateItemMixItems()
        {
            ItemMixItems = _gmDatabaseService.GetItemMixList();
            SelectedItem = ItemMixItems.FirstOrDefault();

            ItemMixGroupItems =
            [
              .. _gmDatabaseService.GetItemMixGroupItems(),
              .. _gmDatabaseService.GetCostumeMixGroupItems(),
            ];
        }
        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"Item Craft";

        [ObservableProperty]
        private string? _openMessage = "Open a file";

        [ObservableProperty]
        private ItemDataManager _itemDataManager;

        [ObservableProperty]
        private ObservableCollection<ItemMixData> _itemMixItems = [];

        [ObservableProperty]
        private ObservableCollection<ItemDataViewModel> _itemMixMaterialsItems = [];

        [ObservableProperty]
        private List<string>? _filterGroups;

        [ObservableProperty]
        private string? _selectedGroup;
        partial void OnSelectedGroupChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                FilterGroups = value.Split(',').Select(g => g.Trim()).ToList();
            }
            else
            {
                FilterGroups = null;
            }
            RefreshView();
            SelectedItem = ItemMixItems.FirstOrDefault(FilterItems);
        }

        [ObservableProperty]
        private ItemDataViewModel? _selectedItemDataViewModel;

        [ObservableProperty]
        private Visibility _isSelectedItemVisible = Visibility.Hidden;

        [ObservableProperty]
        private ItemMixData? _selectedItem;
        partial void OnSelectedItemChanged(ItemMixData? value)
        {
            UpdateItemMixMaterial(value);
        }

        #endregion

    }
}
