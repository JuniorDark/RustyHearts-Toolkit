﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Newtonsoft.Json;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Views;
using System.Data;
using System.IO;
using System.Windows;

namespace RHToolkit.ViewModels
{
    public partial class MailWindowViewModel : ObservableObject, IRecipient<ItemDataMessage>
    {
        private readonly List<ItemData>? _cachedItemDataList = [];

        public MailWindowViewModel()
        {
            if (ItemDataManager.Instance.CachedItemDataList == null)
            {
                ItemDataManager.Instance.InitializeCachedLists();
            }

            _cachedItemDataList = ItemDataManager.Instance.CachedItemDataList;


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
                    var viewModel = App.Services.BuildServiceProvider().GetRequiredService<ItemWindowViewModel>();
                    itemWindow = new ItemWindow(viewModel);
                    itemWindow.Closed += (sender, e) => itemWindow = null;
                    itemWindow.Show();
                    WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, ViewModelType.ItemWindowViewModel));
                }
            }
        }


        [RelayCommand]
        private void SaveTemplate()
        {
            try
            {
                MailTemplateData templateData = new()
                {
                    MailTemplate = true,
                    Sender = Sender,
                    Recipient = Recipient,
                    SendToAll = SendToAll,
                    MailContent = MailContent,
                    AttachGold = AttachGold,
                    ItemCharge = ItemCharge,
                    ReturnDays = ReturnDays,
                    ItemIDs = Item.Select(item => item.ID).ToList(),
                    ItemAmounts = Item.Select(item => item.Amount).ToList(),
                    Durabilities = Item.Select(item => item.Durability).ToList(),
                    EnchantLevels = Item.Select(item => item.EnchantLevel).ToList(),
                    Ranks = Item.Select(item => item.Rank).ToList(),
                    ReconNums = Item.Select(item => item.Reconstruction).ToList(),
                    ReconStates = Item.Select(item => item.ReconstructionMax).ToList(),
                    Options1 = Item.Select(item => item.RandomOption01).ToList(),
                    Options2 = Item.Select(item => item.RandomOption02).ToList(),
                    Options3 = Item.Select(item => item.RandomOption03).ToList(),
                    OptionValues1 = Item.Select(item => item.RandomOption01Value).ToList(),
                    OptionValues2 = Item.Select(item => item.RandomOption02Value).ToList(),
                    OptionValues3 = Item.Select(item => item.RandomOption03Value).ToList(),
                    SocketCounts = Item.Select(item => item.SocketCount).ToList(),
                    SocketColors1 = Item.Select(item => item.Socket01Color).ToList(),
                    SocketColors2 = Item.Select(item => item.Socket02Color).ToList(),
                    SocketColors3 = Item.Select(item => item.Socket03Color).ToList(),
                    SocketOptions1 = Item.Select(item => item.SocketOption01).ToList(),
                    SocketOptions2 = Item.Select(item => item.SocketOption02).ToList(),
                    SocketOptions3 = Item.Select(item => item.SocketOption03).ToList(),
                    SocketOptionValues1 = Item.Select(item => item.SocketOption01Value).ToList(),
                    SocketOptionValues2 = Item.Select(item => item.SocketOption02Value).ToList(),
                    SocketOptionValues3 = Item.Select(item => item.SocketOption03Value).ToList(),
                    DurabilityMaxValues = Item.Select(item => item.MaxDurability).ToList(),
                    WeightValues = Item.Select(item => item.Weight).ToList(),
                };

                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    FilterIndex = 1
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (StreamWriter file = File.CreateText(saveFileDialog.FileName))
                    {
                        JsonSerializer serializer = new()
                        {
                            Formatting = Formatting.Indented
                        };
                        serializer.Serialize(file, templateData);
                    }

                    MessageBox.Show("Mail template saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task LoadTemplate()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = await File.ReadAllTextAsync(openFileDialog.FileName);

                    IsButtonEnabled = false;

                    await Task.Run(() =>
                    {
                        LoadTemplate(json);
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsButtonEnabled = true;
                }
            }
        }

        private void LoadTemplate(string json)
        {
            try
            {
                if (json.Contains("\"MailTemplate\": true"))
                {
                    MailTemplateData? templateData = JsonConvert.DeserializeObject<MailTemplateData>(json);

                    if (templateData != null)
                    {
                        List<int> invalidItemIDs = GetInvalidItemIDs(templateData.ItemIDs);

                        if (invalidItemIDs.Count > 0)
                        {
                            string invalidItemIDsString = string.Join(", ", invalidItemIDs);
                            MessageBox.Show($"Template have invalid ItemIDs.\nInvalid ItemIDs in the template: {invalidItemIDsString}", "Error loading template", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        ClearTemplate();

                        Sender = templateData.Sender;
                        Recipient = templateData.Recipient;
                        SendToAll = templateData.SendToAll;
                        MailContent = templateData.MailContent;
                        AttachGold = templateData.AttachGold;
                        ItemCharge = templateData.ItemCharge;
                        ReturnDays = templateData.ReturnDays;

                        if (templateData != null && templateData.ItemIDs != null)
                        {
                            for (int i = 0; i < templateData.ItemIDs?.Count; i++)
                            {
                                // Find the corresponding ItemData object in the _cachedItemDataList
                                ItemData? cachedItem = _cachedItemDataList?.FirstOrDefault(item => item.ID == templateData.ItemIDs[i]);

                                ItemData itemData = new()
                                {
                                    SlotIndex = i,
                                    ID = templateData.ItemIDs[i],
                                    Name = cachedItem?.Name ?? "",
                                    IconName = cachedItem?.IconName ?? "",
                                    Branch = cachedItem?.Branch ?? 0,
                                    Amount = templateData.ItemAmounts?[i] ?? 0,
                                    Durability = templateData.Durabilities?[i] ?? 0,
                                    EnchantLevel = templateData.EnchantLevels?[i] ?? 0,
                                    Rank = templateData.Ranks?[i] ?? 0,
                                    Reconstruction = templateData.ReconNums?[i] ?? 0,
                                    ReconstructionMax = templateData.ReconStates?[i] ?? 0,
                                    RandomOption01 = templateData.Options1?[i] ?? 0,
                                    RandomOption02 = templateData.Options2?[i] ?? 0,
                                    RandomOption03 = templateData.Options3?[i] ?? 0,
                                    RandomOption01Value = templateData.OptionValues1?[i] ?? 0,
                                    RandomOption02Value = templateData.OptionValues2?[i] ?? 0,
                                    RandomOption03Value = templateData.OptionValues3?[i] ?? 0,
                                    SocketCount = templateData.SocketCounts?[i] ?? 0,
                                    Socket01Color = templateData.SocketColors1?[i] ?? 0,
                                    Socket02Color = templateData.SocketColors2?[i] ?? 0,
                                    Socket03Color = templateData.SocketColors3?[i] ?? 0,
                                    SocketOption01 = templateData.SocketOptions1?[i] ?? 0,
                                    SocketOption02 = templateData.SocketOptions2?[i] ?? 0,
                                    SocketOption03 = templateData.SocketOptions3?[i] ?? 0,
                                    SocketOption01Value = templateData.SocketOptionValues1?[i] ?? 0,
                                    SocketOption02Value = templateData.SocketOptionValues2?[i] ?? 0,
                                    SocketOption03Value = templateData.SocketOptionValues3?[i] ?? 0,
                                    MaxDurability = templateData.DurabilityMaxValues?[i] ?? 0,
                                    Weight = templateData.WeightValues?[i] ?? 0,
                                };

                                Item ??= [];
                                Item.Add(itemData);
                                UpdateItemProperties(itemData);
                            }
                        }

                    }
                    else
                    {
                        MessageBox.Show("Failed to load JSON template or JSON is empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Invalid template file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading json template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<int> GetInvalidItemIDs(List<int>? itemIDs)
        {
            List<int> invalidItemIDs = [];

            if (itemIDs == null || itemIDs.Count == 0)
            {
                // ItemIDs is null or empty, so no validation needed
                return invalidItemIDs;
            }

            // Validate each ItemID against the cached data table
            foreach (int itemID in itemIDs)
            {
                // Check if there is any item in the cached list with the current item ID
                bool found = false;
                foreach (ItemData item in _cachedItemDataList)
                {
                    if (item.ID == itemID)
                    {
                        found = true;
                        break;
                    }
                }

                // If no matching row is found, the ItemID is invalid
                if (!found)
                {
                    invalidItemIDs.Add(itemID);
                }
            }

            return invalidItemIDs;
        }

        [RelayCommand]
        private void ClearTemplate()
        {
            Recipient = default;
            Sender = "GM";
            MailContent = "GameMaster InsertItem";
            SendToAll = false;
            AttachGold = 0;
            ReturnDays = 7;
            ItemCharge = 0;

            Item?.Clear();

            ResetItemProperties(0);
            ResetItemProperties(1);
            ResetItemProperties(2);
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

        [ObservableProperty]
        private bool _isButtonEnabled = true;

        [ObservableProperty]
        private string? _recipient;

        [ObservableProperty]
        private string? _sender = "GM";

        [ObservableProperty]
        private string? _mailContent = "GameMaster InsertItem";

        [ObservableProperty]
        private int _attachGold;

        [ObservableProperty]
        private int _returnDays = 7;

        [ObservableProperty]
        private int _itemCharge;

        [ObservableProperty]
        private bool _sendToAll = false;

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
