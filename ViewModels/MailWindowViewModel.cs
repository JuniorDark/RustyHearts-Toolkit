using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Views;

namespace RHToolkit.ViewModels
{
    public partial class MailWindowViewModel : ObservableObject, IRecipient<ItemDataMessage>
    {
        public MailWindowViewModel()
        {
            WeakReferenceMessenger.Default.Register(this);
        }

        private ItemWindow? itemWindow;

        [RelayCommand]
        private void AddItem(string parameter)
        {
            if (int.TryParse(parameter, out int slotIndex))
            {
                ItemData? itemData = Item?.FirstOrDefault(m => m.SlotIndex == slotIndex);

                itemData ??= new ItemData
                {
                    SlotIndex = slotIndex
                };

                if (itemWindow != null)
                {
                    WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, ViewModelType.ItemWindowViewModel));
                }
                else
                {
                    itemWindow = new ItemWindow();
                    itemWindow.Closed += (sender, e) => itemWindow = null;
                    itemWindow.Show();
                    WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, ViewModelType.ItemWindowViewModel));
                }
            }
        }


        [RelayCommand]
        private void RemoveItem(string parameter)
        {
            if (int.TryParse(parameter, out int slotIndex))
            {
                if (Item != null)
                {
                    var removedItemIndex = Item.FindIndex(i => i.SlotIndex == slotIndex);
                    if (removedItemIndex != -1)
                    {
                        // Remove the ItemData with the specified SlotIndex
                        Item.RemoveAt(removedItemIndex);

                        // Update properties to default values for the removed SlotIndex
                        ResetItemProperties(slotIndex);
                    }
                }
            }
        }

        private void ResetItemProperties(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0:
                    ItemIcon1 = null;
                    ItemIconBranch1 = 0;
                    ItemName1 = "Click to add a item";
                    ItemAmount1 = 0;
                    break;
                case 1:
                    ItemIcon2 = null;
                    ItemIconBranch2 = 0;
                    ItemName2 = "Click to add a item";
                    ItemAmount2 = 0;
                    break;
                case 2:
                    ItemIcon3 = null;
                    ItemIconBranch3 = 0;
                    ItemName3 = "Click to add a item";
                    ItemAmount3 = 0;
                    break;
                default:
                    break;
            }
        }

        public void Receive(ItemDataMessage message)
        {
            if (message.Recipient == ViewModelType.MailWindowViewModel)
            {
                var itemData = message.Value;

                Item ??= [];

                var existingItemIndex = Item.FindIndex(m => m.SlotIndex == itemData.SlotIndex);
                if (existingItemIndex != -1)
                {
                    // Remove the existing ItemData with the same SlotIndex
                    Item.RemoveAt(existingItemIndex);
                }

                // Add the new ItemData to the Item list
                Item.Add(itemData);

                UpdateItemProperties(itemData);

            }
        }

        [ObservableProperty]
        private List<ItemData>? _item;
        partial void OnItemChanged(List<ItemData>? value)
        {
            //UpdateItemData(value);
        }

        private void UpdateItemProperties(ItemData itemData)
        {
            switch (itemData.SlotIndex)
            {
                case 0:
                    ItemIcon1 = itemData.IconName;
                    ItemIconBranch1 = itemData.Branch;
                    ItemName1 = itemData.Name;
                    ItemAmount1 = itemData.Amount;
                    break;
                case 1:
                    ItemIcon2 = itemData.IconName;
                    ItemIconBranch2 = itemData.Branch;
                    ItemName2 = itemData.Name;
                    ItemAmount2 = itemData.Amount;
                    break;
                case 2:
                    ItemIcon3 = itemData.IconName;
                    ItemIconBranch3 = itemData.Branch;
                    ItemName3 = itemData.Name;
                    ItemAmount3 = itemData.Amount;
                    break;
                default:
                    break;
            }
        }

        private void UpdateItemData(List<ItemData>? itemDatas)
        {
            if (itemDatas != null)
            {
                ItemIcon1 = itemDatas.FirstOrDefault(data => data.SlotIndex == 0)?.IconName;
                ItemIcon2 = itemDatas.FirstOrDefault(data => data.SlotIndex == 1)?.IconName;
                ItemIcon3 = itemDatas.FirstOrDefault(data => data.SlotIndex == 2)?.IconName;
                ItemIconBranch1 = itemDatas.FirstOrDefault(data => data.SlotIndex == 0)?.Branch;
                ItemIconBranch2 = itemDatas.FirstOrDefault(data => data.SlotIndex == 1)?.Branch;
                ItemIconBranch3 = itemDatas.FirstOrDefault(data => data.SlotIndex == 2)?.Branch;
                ItemName1 = itemDatas.FirstOrDefault(data => data.SlotIndex == 0)?.Name;
                ItemName2 = itemDatas.FirstOrDefault(data => data.SlotIndex == 1)?.Name;
                ItemName3 = itemDatas.FirstOrDefault(data => data.SlotIndex == 2)?.Name;
                ItemAmount1 = itemDatas.FirstOrDefault(data => data.SlotIndex == 0)?.Amount;
                ItemAmount2 = itemDatas.FirstOrDefault(data => data.SlotIndex == 1)?.Amount;
                ItemAmount3 = itemDatas.FirstOrDefault(data => data.SlotIndex == 2)?.Amount;
            }
        }


        [ObservableProperty]
        private string? _recipient;

        [ObservableProperty]
        private string? _sender;

        [ObservableProperty]
        private string? _mailContent;

        [ObservableProperty]
        private int _attachGold;

        [ObservableProperty]
        private int _returnDays;

        [ObservableProperty]
        private int _itemCharge;

        [ObservableProperty]
        private bool _sendToAll;

        [ObservableProperty]
        private string? _itemName1 = "Click to add a item";

        [ObservableProperty]
        private string? _itemName2 = "Click to add a item";

        [ObservableProperty]
        private string? _itemName3 = "Click to add a item";

        [ObservableProperty]
        private int? _itemAmount1;

        [ObservableProperty]
        private int? _itemAmount2;

        [ObservableProperty]
        private int? _itemAmount3;

        [ObservableProperty]
        private string? _itemIcon1;

        [ObservableProperty]
        private string? _itemIcon2;

        [ObservableProperty]
        private string? _itemIcon3;

        [ObservableProperty]
        private int? _itemIconBranch1;

        [ObservableProperty]
        private int? _itemIconBranch2;

        [ObservableProperty]
        private int? _itemIconBranch3;

    }
}
