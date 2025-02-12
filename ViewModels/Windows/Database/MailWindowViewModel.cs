﻿using Microsoft.Win32;
using Newtonsoft.Json;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.ViewModels.Windows.Database.VM;
using System.Data;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Windows;

public partial class MailWindowViewModel : ObservableValidator, IRecipient<ItemDataMessage>
{
    private readonly IWindowsService _windowsService;
    private readonly IDatabaseService _databaseService;
    private readonly MailDataManager _mailDataManager;
    private readonly ItemDataManager _itemDataManager;
    private readonly Guid _token;

    public MailWindowViewModel(IWindowsService windowsService, IDatabaseService databaseService, MailDataManager mailDataManager, ItemDataManager itemDataManager)
    {
        _token = Guid.NewGuid();
        _windowsService = windowsService;
        _databaseService = databaseService;
        _mailDataManager = mailDataManager;
        _itemDataManager = itemDataManager;

        InitializeMailItemDataViewModels();

        WeakReferenceMessenger.Default.Register(this);
    }

    #region Add Item

    [RelayCommand]
    private void AddItem(string parameter)
    {
        try
        {
            if (int.TryParse(parameter, out int slotIndex))
            {
                ItemData? itemData = ItemDataList?.FirstOrDefault(i => i.SlotIndex == slotIndex) ?? new ItemData { SlotIndex = slotIndex};

                _windowsService.OpenItemWindow(_token, "Mail", itemData);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
        
    }

    public void Receive(ItemDataMessage message)
    {
        if (message.Recipient == "MailWindow" && message.Token == _token)
        {
            var itemData = message.Value;

            CreateItem(itemData);
        }
    }

    private void CreateItem(ItemData itemData)
    {
        ItemDataList ??= [];
        RemoveItem(itemData.SlotIndex);
        ItemDataList.Add(itemData);
        SetItemDataViewModel(itemData);
    }

    private void RemoveItem(int slotIndex)
    {
        if (ItemDataList != null)
        {
            // Find the existing item in ItemDataList
            var removedItem = ItemDataList.FirstOrDefault(item => item.SlotIndex == slotIndex);

            if (removedItem != null)
            {
                ItemDataList?.Remove(removedItem);
                OnPropertyChanged(nameof(ItemDataList));
                RemoveItemDataViewModel(removedItem);
            }
        }
    }

    private void SetItemDataViewModel(ItemData itemData)
    {
        if (MailItemDataViewModels != null)
        {
            var itemDataViewModel = _itemDataManager.GetItemData(itemData);
            MailItemDataViewModels[itemDataViewModel.SlotIndex].ItemDataViewModel = itemDataViewModel;
            OnPropertyChanged(nameof(MailItemDataViewModels));
        }
    }

    private void RemoveItemDataViewModel(ItemData removedItem)
    {
        if (MailItemDataViewModels != null)
        {
            MailItemDataViewModels[removedItem.SlotIndex].ItemDataViewModel = null;
            OnPropertyChanged(nameof(MailItemDataViewModels));
        }
    }

    private void InitializeMailItemDataViewModels()
    {
        MailItemDataViewModels = ItemDataManager.InitializeCollection(3, 61);
    }

    #endregion

    #region Remove Item

    [RelayCommand]
    private void RemoveItem(string parameter)
    {
        if (int.TryParse(parameter, out int slotIndex))
        {
            RemoveItem(slotIndex);
        }
    }
    #endregion

    #region Template

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
                SendToAll = SelectAllCheckBoxChecked,
                SendToAllOnline = OnlineCheckBoxChecked,
                SendToAllOffline = OfflineCheckBoxChecked,
                MailContent = MailContent,
                AttachGold = AttachGold,
                ItemCharge = ItemCharge,
                ReturnDays = ReturnDays,
                ItemIDs = ItemDataList?.Select(item => item.ItemId).ToList(),
                ItemAmounts = ItemDataList?.Select(item => item.ItemAmount).ToList(),
                Durabilities = ItemDataList?.Select(item => item.DurabilityMax).ToList(),
                EnchantLevels = ItemDataList?.Select(item => item.EnhanceLevel).ToList(),
                AugmentStoneValues = ItemDataList?.Select(item => item.AugmentStone).ToList(),
                Ranks = ItemDataList?.Select(item => item.Rank).ToList(),
                ReconNums = ItemDataList?.Select(item => item.Reconstruction).ToList(),
                ReconStates = ItemDataList?.Select(item => item.ReconstructionMax).ToList(),
                Options1 = ItemDataList?.Select(item => item.Option1Code).ToList(),
                Options2 = ItemDataList?.Select(item => item.Option2Code).ToList(),
                Options3 = ItemDataList?.Select(item => item.Option3Code).ToList(),
                OptionValues1 = ItemDataList?.Select(item => item.Option1Value).ToList(),
                OptionValues2 = ItemDataList?.Select(item => item.Option2Value).ToList(),
                OptionValues3 = ItemDataList?.Select(item => item.Option3Value).ToList(),
                SocketCounts = ItemDataList?.Select(item => item.SocketCount).ToList(),
                SocketColors1 = ItemDataList?.Select(item => item.Socket1Color).ToList(),
                SocketColors2 = ItemDataList?.Select(item => item.Socket2Color).ToList(),
                SocketColors3 = ItemDataList?.Select(item => item.Socket3Color).ToList(),
                SocketOptions1 = ItemDataList?.Select(item => item.Socket1Code).ToList(),
                SocketOptions2 = ItemDataList?.Select(item => item.Socket2Code).ToList(),
                SocketOptions3 = ItemDataList?.Select(item => item.Socket3Code).ToList(),
                SocketOptionValues1 = ItemDataList?.Select(item => item.Socket1Value).ToList(),
                SocketOptionValues2 = ItemDataList?.Select(item => item.Socket2Value).ToList(),
                SocketOptionValues3 = ItemDataList?.Select(item => item.Socket3Value).ToList(),
                DurabilityMaxValues = ItemDataList?.Select(item => item.DurabilityMax).ToList(),
                WeightValues = ItemDataList?.Select(item => item.Weight).ToList(),
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
                RHMessageBoxHelper.ShowOKMessage(Resources.SaveTemplateSucess, Resources.Success);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.SaveTemplateError}: {ex.Message}", Resources.Error);
        }
    }

    [RelayCommand]
    private async Task LoadTemplate()
    {
        OpenFileDialog openFileDialog = new()
        {
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                string template = await File.ReadAllTextAsync(openFileDialog.FileName);

                IsButtonEnabled = false;

                LoadTemplate(template);
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.LoadTemplateError}: {ex.Message}", Resources.LoadTemplateError);
            }
            finally
            {
                IsButtonEnabled = true;
            }
        }
    }

    private void LoadTemplate(string template)
    {
        if (template.Contains("\"MailTemplate\": true"))
        {
            MailTemplateData? templateData = JsonConvert.DeserializeObject<MailTemplateData>(template);

            if (templateData != null)
            {
                List<int> invalidItemIDs = [];

                if (templateData.ItemIDs != null)
                {
                    foreach (int itemID in templateData.ItemIDs)
                    {
                        if (_itemDataManager.IsInvalidItemID(itemID))
                        {
                            invalidItemIDs.Add(itemID);
                        }
                    }
                }

                if (invalidItemIDs.Count > 0)
                {
                    string invalidItemIDsString = string.Join(", ", invalidItemIDs);
                    RHMessageBoxHelper.ShowOKMessage($"{Resources.TemplateInvalidId}: {invalidItemIDsString}", Resources.LoadTemplateError);
                    return;
                }

                ClearMailData();

                Sender = templateData.Sender;
                Recipient = templateData.Recipient;
                SelectAllCheckBoxChecked = templateData.SendToAll;
                OnlineCheckBoxChecked = templateData.SendToAllOnline;
                OfflineCheckBoxChecked = templateData.SendToAllOffline;
                MailContent = templateData.MailContent;
                AttachGold = templateData.AttachGold;
                ItemCharge = templateData.ItemCharge;
                ReturnDays = templateData.ReturnDays;

                if (templateData.ItemIDs != null)
                {
                    for (int i = 0; i < templateData.ItemIDs.Count; i++)
                    {
                        ItemData itemData = new()
                        {
                            SlotIndex = i,
                            ItemId = templateData.ItemIDs[i],
                            ItemAmount = templateData.ItemAmounts?[i] ?? 0,
                            Durability = templateData.Durabilities?[i] ?? 0,
                            EnhanceLevel = templateData.EnchantLevels?[i] ?? 0,
                            AugmentStone = templateData.AugmentStoneValues?[i] ?? 0,
                            Rank = templateData.Ranks?[i] ?? 0,
                            Reconstruction = templateData.ReconNums?[i] ?? 0,
                            ReconstructionMax = templateData.ReconStates?[i] ?? 0,
                            Option1Code = templateData.Options1?[i] ?? 0,
                            Option2Code = templateData.Options2?[i] ?? 0,
                            Option3Code = templateData.Options3?[i] ?? 0,
                            Option1Value = templateData.OptionValues1?[i] ?? 0,
                            Option2Value = templateData.OptionValues2?[i] ?? 0,
                            Option3Value = templateData.OptionValues3?[i] ?? 0,
                            SocketCount = templateData.SocketCounts?[i] ?? 0,
                            Socket1Color = templateData.SocketColors1?[i] ?? 0,
                            Socket2Color = templateData.SocketColors2?[i] ?? 0,
                            Socket3Color = templateData.SocketColors3?[i] ?? 0,
                            Socket1Code = templateData.SocketOptions1?[i] ?? 0,
                            Socket2Code = templateData.SocketOptions2?[i] ?? 0,
                            Socket3Code = templateData.SocketOptions3?[i] ?? 0,
                            Socket1Value = templateData.SocketOptionValues1?[i] ?? 0,
                            Socket2Value = templateData.SocketOptionValues2?[i] ?? 0,
                            Socket3Value = templateData.SocketOptionValues3?[i] ?? 0,
                            DurabilityMax = templateData.DurabilityMaxValues?[i] ?? 0,
                            Weight = templateData.WeightValues?[i] ?? 0,
                        };

                        CreateItem(itemData);
                    }
                }
            }
            else
            {
                RHMessageBoxHelper.ShowOKMessage(Resources.LoadTemplateJsonError, Resources.LoadTemplateError);
            }
        }
        else
        {
            RHMessageBoxHelper.ShowOKMessage(Resources.InvalidTemplate, Resources.LoadTemplateError);
        }
    }

    [RelayCommand]
    private void ClearMailData()
    {
        Recipient = default;
        Sender = "GM";
        MailContent = Resources.GameMasterInsertItem;
        SelectAllCheckBoxChecked = false;
        OnlineCheckBoxChecked = false;
        OfflineCheckBoxChecked = false;
        AttachGold = 0;
        ReturnDays = 7;
        ItemCharge = 0;

        ItemDataList?.Clear();
        MailItemDataViewModels?.Clear();
        InitializeMailItemDataViewModels();
    }
    #endregion

    #region Send Mail

    [RelayCommand(CanExecute = nameof(CanExecuteCommand))]
    private async Task SendMailAsync()
    {
        string? mailRecipient = Recipient;
        string? mailSender = Sender;
        string? message = MailContent;
        int gold = AttachGold;
        int reqGold = ItemCharge;
        int returnDay = ReturnDays;
        bool sendToAllCharacters = SelectAllCheckBoxChecked;
        bool sendToAllOnline = OnlineCheckBoxChecked && !OfflineCheckBoxChecked;
        bool sendToAllOffline = OfflineCheckBoxChecked && !OnlineCheckBoxChecked;

        if (string.IsNullOrWhiteSpace(mailRecipient) && !sendToAllCharacters && !sendToAllOnline && !sendToAllOffline)
        {
            RHMessageBoxHelper.ShowOKMessage(Resources.EmptyRecipientDesc, Resources.EmptyRecipient);
            return;
        }

        if (string.IsNullOrWhiteSpace(mailSender))
        {
            RHMessageBoxHelper.ShowOKMessage(Resources.EmptySenderDesc, Resources.EmptySender);
            return;
        }

        try
        {
            string[] recipients = await _mailDataManager.GetRecipientsAsync(mailRecipient, sendToAllCharacters, sendToAllOnline, sendToAllOffline);
            if (recipients == null || recipients.Length == 0)
            {
                return;
            }

            string confirmationMessage = MailDataManager.GetConfirmationMessage(sendToAllCharacters, sendToAllOnline, sendToAllOffline, recipients);

            if (RHMessageBoxHelper.ConfirmMessage(confirmationMessage))
            {
                IsButtonEnabled = false;

                (List<string> successfulRecipients, List<string> failedRecipients) = await _databaseService.SendMailAsync(mailSender, message, gold, reqGold, returnDay, recipients, ItemDataList!);

                string successMessage = MailDataManager.GetSendMessage(sendToAllCharacters, sendToAllOnline, sendToAllOffline, successfulRecipients, failedRecipients);
                RHMessageBoxHelper.ShowOKMessage(successMessage, Resources.SendMail);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.SendMailError}: {ex.Message}", Resources.Error);
        }
        finally
        {
            IsButtonEnabled = true;
        }
        
    }

    private bool CanExecuteCommand()
    {
        return !string.IsNullOrWhiteSpace(Recipient) && !string.IsNullOrWhiteSpace(Sender);
    }
    #endregion

    #region Properties
    [ObservableProperty]
    private string _title = Resources.SendMail;

    [ObservableProperty]
    private List<ItemData>? _itemDataList;

    [ObservableProperty]
    private ObservableCollection<InventoryItem>? _mailItemDataViewModels;

    [ObservableProperty]
    private bool _isButtonEnabled = true;

    [ObservableProperty]
    private string? _recipient;
    partial void OnRecipientChanged(string? value)
    {
       SendMailCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    private string? _sender = "GM";
    partial void OnSenderChanged(string? value)
    {
        SendMailCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    private string? _mailContent = Resources.GameMasterInsertItem;

    [ObservableProperty]
    private int _attachGold;

    [ObservableProperty]
    private int _returnDays = 7;

    [ObservableProperty]
    private int _itemCharge;

    [ObservableProperty]
    private bool _isRecipientEnabled = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRecipientEnabled))]
    private bool _selectAllCheckBoxChecked = false;
    partial void OnSelectAllCheckBoxCheckedChanged(bool value)
    {
        IsRecipientEnabled = value == true ? false : true;
    }

    [ObservableProperty]
    private bool _onlineCheckBoxChecked = false;
    partial void OnOnlineCheckBoxCheckedChanged(bool value)
    {
        IsRecipientEnabled = value == true ? false : true;
    }

    [ObservableProperty]
    private bool _offlineCheckBoxChecked = false;
    partial void OnOfflineCheckBoxCheckedChanged(bool value)
    {
        IsRecipientEnabled = value == true ? false : true;
    }

    [RelayCommand]
    private void OnSelectAllChecked(object sender)
    {
        if (sender is not CheckBox checkBox)
        {
            return;
        }

        checkBox.IsChecked ??=
            !OnlineCheckBoxChecked || !OfflineCheckBoxChecked;

        if (checkBox.IsChecked == true)
        {
            OnlineCheckBoxChecked = true;
            OfflineCheckBoxChecked = true;
        }
        else if (checkBox.IsChecked == false)
        {
            OnlineCheckBoxChecked = false;
            OfflineCheckBoxChecked = false;
        }
    }

    [RelayCommand]
    private void OnSingleChecked(string option)
    {
        bool allChecked = OnlineCheckBoxChecked && OfflineCheckBoxChecked;
        bool allUnchecked =
            !OnlineCheckBoxChecked && !OfflineCheckBoxChecked;

        SelectAllCheckBoxChecked = allChecked
        || (allUnchecked
        && false);
    }

    #endregion

}
