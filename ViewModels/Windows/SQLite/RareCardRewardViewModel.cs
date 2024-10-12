using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using RHToolkit.ViewModels.Windows.Tools.VM;
using System.ComponentModel;

namespace RHToolkit.ViewModels.Windows
{
    public partial class RareCardRewardViewModel : ObservableObject, IRecipient<IDMessage>
    {
        private readonly IGMDatabaseService _gmDatabaseService;
        private readonly System.Timers.Timer _rareCardItemFilterUpdateTimer;

        public RareCardRewardViewModel(IGMDatabaseService gmDatabaseService, ItemDataManager itemDataManager)
        {
            _gmDatabaseService = gmDatabaseService;
            _itemDataManager = itemDataManager;
            _rareCardItemFilterUpdateTimer = new()
            {
                Interval = 400,
                AutoReset = false
            };
            _rareCardItemFilterUpdateTimer.Elapsed += RareCardItemFilterUpdateTimerElapsed;
            PopulateRareCardRewardItems();
            _rareCardRewardView = new CollectionViewSource { Source = RareCardRewardItems }.View;
            _rareCardRewardView.Filter = FilterItems;

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

        public void Receive(IDMessage message)
        {
            var itemId = message.Value;

            if (RareCardRewardItems != null)
            {
                var item = RareCardRewardItems.FirstOrDefault(item => item.ID == itemId);

                SelectedItem = item;
            }
        }

        #endregion

        #region SelectedItem
        private void UpdateRareCardRewardItemSelectedItem(RareCardReward? selectedItem)
        {
            if (selectedItem != null)
            {
                RareCardItems ??= [];

                for (int i = 0; i < 40; i++)
                {
                    var property = typeof(RareCardReward).GetProperty($"RewardItem{i + 1:D2}");

                    if (property != null)
                    {
                        var value = property.GetValue(selectedItem);
                        if (value is int rewardItem) 
                        {
                            ItemDataViewModel? itemDataViewModel = null;

                            if (i < RareCardItems.Count)
                            {
                                var existingItem = RareCardItems[i];
                                if (existingItem.DropItemCode != rewardItem)
                                {
                                    itemDataViewModel = ItemDataManager.GetItemDataViewModel(rewardItem, i, 1);
                                    existingItem.ItemDataViewModel = itemDataViewModel;
                                }

                                existingItem.DropItemCode = rewardItem;
                            }
                            else
                            {
                                itemDataViewModel = ItemDataManager.GetItemDataViewModel(rewardItem, i, 1);

                                var newItem = new ItemDropGroup
                                {
                                    DropItemCode = rewardItem,
                                    ItemDataViewModel = itemDataViewModel
                                };

                                Application.Current.Dispatcher.Invoke(() => RareCardItems.Add(newItem));
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
        private ICollectionView _rareCardRewardView;

        private bool FilterItems(object obj)
        {
            if (obj is RareCardReward item)
            {
                // Search filtering
                if (!string.IsNullOrEmpty(SearchText))
                {
                    string searchText = SearchText.ToLower();
                    if (item.ID.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }

                    for (int i = 1; i <= 40; i++)
                    {
                        var property = typeof(RareCardReward).GetProperty($"RewardItem{i:D2}");

                        if (property != null)
                        {
                            var value = property.GetValue(item);
                            if (value is int rewardItemValue &&
                                rewardItemValue.ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
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
            Application.Current.Dispatcher.Invoke(RareCardRewardView.Refresh);
        }

        [ObservableProperty]
        private string? _searchText;
        partial void OnSearchTextChanged(string? value)
        {
            _rareCardItemFilterUpdateTimer.Stop();
            _rareCardItemFilterUpdateTimer.Start();
        }

        private void RareCardItemFilterUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            RefreshView();
        }

        #endregion

        #region Lists
        [ObservableProperty]
        private List<RareCardReward>? _rareCardRewardItems;

        private void PopulateRareCardRewardItems()
        {
            RareCardRewardItems = _gmDatabaseService.GetRareCardRewardItems();
            SelectedItem = RareCardRewardItems.FirstOrDefault();
        }
        #endregion

        #region Properties
        [ObservableProperty]
        private string _title = $"Rare Card Reward";

        [ObservableProperty]
        private ItemDataManager _itemDataManager;

        [ObservableProperty]
        private ObservableCollection<ItemDropGroup> _rareCardItems = [];

        [ObservableProperty]
        private Visibility _isSelectedItemVisible = Visibility.Hidden;

        [ObservableProperty]
        private RareCardReward? _selectedItem;
        partial void OnSelectedItemChanged(RareCardReward? value)
        {
            UpdateRareCardRewardItemSelectedItem(value);
        }

        #endregion

    }
}
