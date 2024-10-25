using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using RHToolkit.ViewModels.Windows.Tools.VM;
using System.ComponentModel;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Windows
{
    public partial class DropGroupListViewModel : ObservableObject, IRecipient<DropGroupMessage>
    {
        private readonly IGMDatabaseService _gmDatabaseService;
        private readonly System.Timers.Timer _dropGroupItemFilterUpdateTimer;

        public DropGroupListViewModel(IGMDatabaseService gmDatabaseService, ItemDataManager itemDataManager)
        {
            _gmDatabaseService = gmDatabaseService;
            _itemDataManager = itemDataManager;
            _dropGroupItemFilterUpdateTimer = new()
            {
                Interval = 400,
                AutoReset = false
            };
            _dropGroupItemFilterUpdateTimer.Elapsed += DropGroupItemFilterUpdateTimerElapsed;
            PopulateListItems();
            _dropGroupItemView = new CollectionViewSource { Source = CurrentDropGroupItems }.View;
            _dropGroupItemView.Filter = FilterItems;

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

        #region Message

        public void Receive(DropGroupMessage message)
        {
            var itemId = message.Value;
            var dropGroupType = message.DropGroupType;

            if (dropGroupType != DropGroupType)
            {
                DropGroupItems?.Clear();

                var selectedDropGroupType = DropGroupsItems?.FirstOrDefault(item => item.ID == (int)dropGroupType);

                if (selectedDropGroupType != null)
                {
                    SelectedDropGroupType = selectedDropGroupType;
                }
            }

            if (CurrentDropGroupItems != null)
            {
                var item = CurrentDropGroupItems.FirstOrDefault(item => item.ID == itemId);

                SelectedItem = item;
            }
        }

        #endregion

        #region SelectedItem
        private void UpdateDropGroupListItemSelectedItem(DropGroupList? selectedItem)
        {
            if (selectedItem != null)
            {
                DropGroupItems ??= [];

                if (DropGroupType == ItemDropGroupType.RareCardDropGroupList)
                {
                    var cardIds = new[]
                    {
                        selectedItem.BronzeCardID,
                        selectedItem.SilverCardID,
                        selectedItem.GoldCardID
                    };

                    for (int i = 0; i < cardIds.Length; i++)
                    {
                        int cardId = cardIds[i];

                        ItemDataViewModel? itemDataViewModel = null;

                        if (i < DropGroupItems.Count)
                        {
                            var existingItem = DropGroupItems[i];
                            if (existingItem.DropItemCode != cardId)
                            {
                                itemDataViewModel = ItemDataManager.GetItemDataViewModel(cardId, i, 1);
                                existingItem.ItemDataViewModel = itemDataViewModel;
                            }

                            existingItem.DropItemCode = cardId;
                        }
                        else
                        {
                            itemDataViewModel = ItemDataManager.GetItemDataViewModel(cardId, i, 1);

                            var newItem = new ItemDropGroup
                            {
                                DropItemCode = cardId,
                                ItemDataViewModel = itemDataViewModel
                            };

                            Application.Current.Dispatcher.Invoke(() => DropGroupItems.Add(newItem));
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < DropItemCount; i++)
                    {
                        var property = typeof(DropGroupList).GetProperty($"DropItemCode{i + 1:D2}");

                        if (property != null)
                        {
                            var value = property.GetValue(selectedItem);
                            if (value is int dropItem)
                            {
                                ItemDataViewModel? itemDataViewModel = null;

                                if (i < DropGroupItems.Count)
                                {
                                    var existingItem = DropGroupItems[i];
                                    if (existingItem.DropItemCode != dropItem)
                                    {
                                        itemDataViewModel = ItemDataManager.GetItemDataViewModel(dropItem, i, 1);
                                        existingItem.ItemDataViewModel = itemDataViewModel;
                                    }

                                    existingItem.DropItemCode = dropItem;
                                }
                                else
                                {
                                    itemDataViewModel = ItemDataManager.GetItemDataViewModel(dropItem, i, 1);

                                    var newItem = new ItemDropGroup
                                    {
                                        DropItemCode = dropItem,
                                        ItemDataViewModel = itemDataViewModel
                                    };

                                    Application.Current.Dispatcher.Invoke(() => DropGroupItems.Add(newItem));
                                }
                            }
                        }
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
        private ICollectionView _dropGroupItemView;

        private bool FilterItems(object obj)
        {
            if (obj is DropGroupList item)
            {
                // Search filtering
                if (!string.IsNullOrEmpty(SearchText))
                {
                    string searchText = SearchText.ToLower();
                    if (item.ID.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }

                    for (int i = 1; i <= DropItemCount; i++)
                    {
                        var property = typeof(DropGroupList).GetProperty($"DropItemCode{i:D2}");

                        if (property != null)
                        {
                            var value = property.GetValue(item);
                            if (value is int dropGroupItemValue &&
                                dropGroupItemValue.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }

                return true;
            }

            return false;
        }

        private void RefreshView()
        {
            Application.Current.Dispatcher.Invoke(DropGroupItemView.Refresh);
        }

        [ObservableProperty]
        private string? _searchText;
        partial void OnSearchTextChanged(string? value)
        {
            _dropGroupItemFilterUpdateTimer.Stop();
            _dropGroupItemFilterUpdateTimer.Start();
        }

        private void DropGroupItemFilterUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            RefreshView();
        }

        #endregion

        #region Lists
        [ObservableProperty]
        private List<DropGroupList>? _currentDropGroupItems;

        private void PopulateDropGroupItems(ItemDropGroupType dropGroupType)
        {
            DropGroupType = dropGroupType;
            DropItemCount = DropGroupType switch
            {
                ItemDropGroupType.ChampionItemItemDropGroupList or ItemDropGroupType.InstanceItemDropGroupList or ItemDropGroupType.QuestItemDropGroupList or ItemDropGroupType.WorldInstanceItemDropGroupList => 30,
                ItemDropGroupType.ItemDropGroupListF or ItemDropGroupType.ItemDropGroupList or ItemDropGroupType.RareCardRewardItemList => 40,
                ItemDropGroupType.EventWorldItemDropGroupList or ItemDropGroupType.WorldItemDropGroupList or ItemDropGroupType.WorldItemDropGroupListF => 60,
                ItemDropGroupType.RareCardDropGroupList => 3,
                _ => 0,
            };

            CurrentDropGroupItems = dropGroupType switch
            {
                ItemDropGroupType.ChampionItemItemDropGroupList => ChampionItemDropGroupItems,
                ItemDropGroupType.InstanceItemDropGroupList => InstanceItemDropGroupItems,
                ItemDropGroupType.QuestItemDropGroupList => QuestItemDropGroupItems,
                ItemDropGroupType.WorldInstanceItemDropGroupList => WorldInstanceItemDropGroupItems,
                ItemDropGroupType.ItemDropGroupListF => ItemDropGroupFItems,
                ItemDropGroupType.ItemDropGroupList => ItemDropGroupItems,
                ItemDropGroupType.EventWorldItemDropGroupList => EventWorldItemDropGroupItems,
                ItemDropGroupType.WorldItemDropGroupList => WorldItemDropGroupItems,
                ItemDropGroupType.WorldItemDropGroupListF => WorldItemDropGroupItems,
                ItemDropGroupType.RareCardDropGroupList => RareCardDropGroupItems,
                _ => null,
            };

            DropGroupItemView = CollectionViewSource.GetDefaultView(CurrentDropGroupItems);
            DropGroupItemView.Filter = FilterItems;
            OnPropertyChanged(nameof(CurrentDropGroupItems));
        }

        [ObservableProperty]
        private List<DropGroupList>? _itemDropGroupItems;

        [ObservableProperty]
        private List<DropGroupList>? _itemDropGroupFItems;

        [ObservableProperty]
        private List<DropGroupList>? _questItemDropGroupItems;

        [ObservableProperty]
        private List<DropGroupList>? _championItemDropGroupItems;

        [ObservableProperty]
        private List<DropGroupList>? _instanceItemDropGroupItems;

        [ObservableProperty]
        private List<DropGroupList>? _worldInstanceItemDropGroupItems;

        [ObservableProperty]
        private List<DropGroupList>? _rareCardDropGroupItems;

        [ObservableProperty]
        private List<DropGroupList>? _eventWorldItemDropGroupItems;

        [ObservableProperty]
        private List<DropGroupList>? _worldItemDropGroupItems;

        [ObservableProperty]
        private List<DropGroupList>? _worldItemDropGroupFItems;

        [ObservableProperty]
        private List<NameID>? _dropGroupsItems;

        private void PopulateListItems()
        {
            DropGroupsItems =
                [
                    new NameID { ID = 1, Name = "Item Drop Group List F" },
                    new NameID { ID = 2, Name = "Item Drop Group List" },
                    new NameID { ID = 3, Name = "Champion Item Drop Group List" },
                    new NameID { ID = 4, Name = "Event World Item Drop Group List " },
                    new NameID { ID = 5, Name = "Instance Item Drop Group List" },
                    new NameID { ID = 6, Name = "Quest Item Drop Group List" },
                    new NameID { ID = 7, Name = "World Instance Item Drop Group List" },
                    new NameID { ID = 8, Name = "World Item Drop Group List" },
                    new NameID { ID = 9, Name = "World Item Drop Group List F" },
                    new NameID { ID = 11, Name = "Rare Card Drop Group List" },
                ];

            ItemDropGroupItems = _gmDatabaseService.GetDropGroupListItems("itemdropgrouplist", 40);
            ItemDropGroupFItems = _gmDatabaseService.GetDropGroupListItems("itemdropgrouplist_f", 40);
            QuestItemDropGroupItems = _gmDatabaseService.GetDropGroupListItems("questitemdropgrouplist", 30);
            ChampionItemDropGroupItems = _gmDatabaseService.GetDropGroupListItems("championitemdropgrouplist", 30);
            InstanceItemDropGroupItems = _gmDatabaseService.GetDropGroupListItems("instanceitemdropgrouplist", 30);
            QuestItemDropGroupItems = _gmDatabaseService.GetDropGroupListItems("questitemdropgrouplist", 30);
            WorldInstanceItemDropGroupItems = _gmDatabaseService.GetDropGroupListItems("worldinstanceitemdropgrouplist", 30);
            EventWorldItemDropGroupItems = _gmDatabaseService.GetDropGroupListItems("eventworlditemdropgrouplist", 60);
            WorldItemDropGroupItems = _gmDatabaseService.GetDropGroupListItems("worlditemdropgrouplist", 60);
            WorldItemDropGroupFItems = _gmDatabaseService.GetDropGroupListItems("worlditemdropgrouplist_fatigue", 60);
            RareCardDropGroupItems = _gmDatabaseService.GetRareCardDropGroupListItems();

            SelectedDropGroupType = DropGroupsItems.FirstOrDefault()!;
        }
        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"Drop Group List";

        [ObservableProperty]
        private ItemDataManager _itemDataManager;

        [ObservableProperty]
        private ItemDropGroupType _dropGroupType;

        [ObservableProperty]
        private int _dropItemCount;

        [ObservableProperty]
        private NameID? _selectedDropGroupType;
        partial void OnSelectedDropGroupTypeChanged(NameID? value)
        {
            if (value != null)
            {
                PopulateDropGroupItems((ItemDropGroupType)value.ID);
            }
        }

        [ObservableProperty]
        private ObservableCollection<ItemDropGroup> _dropGroupItems = [];

        [ObservableProperty]
        private Visibility _isSelectedItemVisible = Visibility.Hidden;

        [ObservableProperty]
        private DropGroupList? _selectedItem;
        partial void OnSelectedItemChanged(DropGroupList? value)
        {
            UpdateDropGroupListItemSelectedItem(value);
        }

        #endregion

    }
}
