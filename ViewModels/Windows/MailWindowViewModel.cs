using Microsoft.Win32;
using Newtonsoft.Json;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.SQLite;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using System.Data;
using System.Windows.Controls;

namespace RHToolkit.ViewModels.Windows;

public partial class MailWindowViewModel : ObservableValidator, IRecipient<ItemDataMessage>
{
    private readonly WindowsProviderService _windowsProviderService;
    private readonly MailManager _mailManager;
    private readonly CachedDataManager _cachedDataManager;

    public MailWindowViewModel(WindowsProviderService windowsProviderService, MailManager mailManager, CachedDataManager cachedDataManager)
    {
        _windowsProviderService = windowsProviderService;
        _mailManager = mailManager;
        _cachedDataManager = cachedDataManager;
        WeakReferenceMessenger.Default.Register(this);
    }

    #region Add Item

    private ItemWindow? _itemWindowInstance;

    [RelayCommand]
    private void AddItem(string parameter)
    {
        if (int.TryParse(parameter, out int slotIndex))
        {
            ItemData? itemData = ItemDataList?.FirstOrDefault(m => m.SlotIndex == slotIndex);

            itemData ??= new ItemData
            {
                SlotIndex = slotIndex
            };

            if (_itemWindowInstance == null)
            {
                _windowsProviderService.Show<ItemWindow>();
                _itemWindowInstance = Application.Current.Windows.OfType<ItemWindow>().FirstOrDefault();

                if (_itemWindowInstance != null)
                {
                    _itemWindowInstance.Closed += (sender, args) => _itemWindowInstance = null;

                    _itemWindowInstance.ContentRendered += (sender, args) =>
                    {
                        WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "ItemWindowViewModel", "Mail"));
                    };
                }
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "ItemWindowViewModel", "Mail"));
            }

            _itemWindowInstance?.Focus();
        }
    }

    public void Receive(ItemDataMessage message)
    {
        if (message.Recipient == "MailWindowViewModel")
        {
            var itemData = message.Value;

            ItemDataList ??= [];

            var existingItemIndex = ItemDataList.FindIndex(m => m.SlotIndex == itemData.SlotIndex);
            if (existingItemIndex != -1)
            {
                // Remove the existing ItemData with the same SlotIndex
                ItemDataList.RemoveAt(existingItemIndex);
            }

            // Add the new ItemData to the Item list
            ItemDataList.Add(itemData);

            SetItemProperties(itemData);

        }
    }

    [ObservableProperty]
    private List<ItemData>? _itemDataList;

    private void SetItemProperties(ItemData itemData)
    {
        string iconNameProperty = $"ItemIcon{itemData.SlotIndex}";
        string iconBranchProperty = $"ItemIconBranch{itemData.SlotIndex}";
        string nameProperty = $"ItemName{itemData.SlotIndex}";
        string itemAmountProperty = $"ItemAmount{itemData.SlotIndex}";

        GetType().GetProperty(iconNameProperty)?.SetValue(this, itemData.IconName);
        GetType().GetProperty(iconBranchProperty)?.SetValue(this, itemData.Branch);
        GetType().GetProperty(nameProperty)?.SetValue(this, itemData.Name);
        GetType().GetProperty(itemAmountProperty)?.SetValue(this, itemData.Amount);
    }
    #endregion

    #region Remove Item
    [RelayCommand]
    private void RemoveItem(string parameter)
    {
        if (int.TryParse(parameter, out int slotIndex))
        {
            if (ItemDataList != null)
            {
                var removedItemIndex = ItemDataList.FindIndex(i => i.SlotIndex == slotIndex);
                if (removedItemIndex != -1)
                {
                    // Remove the ItemData with the specified SlotIndex
                    ItemDataList.RemoveAt(removedItemIndex);

                    // Update properties to default values for the removed SlotIndex
                    ResetItemProperties(slotIndex);
                }
            }
        }
    }

    private void ResetItemProperties(int slotIndex)
    {
        string iconNameProperty = $"ItemIcon{slotIndex}";
        string iconBranchProperty = $"ItemIconBranch{slotIndex}";
        string nameProperty = $"ItemName{slotIndex}";
        string amountProperty = $"ItemAmount{slotIndex}";

        GetType().GetProperty(iconNameProperty)?.SetValue(this, null);
        GetType().GetProperty(iconBranchProperty)?.SetValue(this, 0);
        GetType().GetProperty(nameProperty)?.SetValue(this, Resources.AddItemDesc);
        GetType().GetProperty(amountProperty)?.SetValue(this, 0);
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
                MailContent = MailContent,
                AttachGold = AttachGold,
                ItemCharge = ItemCharge,
                ReturnDays = ReturnDays,
                ItemIDs = ItemDataList?.Select(item => item.ID).ToList(),
                ItemAmounts = ItemDataList?.Select(item => item.Amount).ToList(),
                Durabilities = ItemDataList?.Select(item => item.DurabilityMax).ToList(),
                EnchantLevels = ItemDataList?.Select(item => item.EnhanceLevel).ToList(),
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
                RHMessageBox.ShowOKMessage(Resources.SaveTemplateSucess, Resources.Success);
            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"{Resources.SaveTemplateError}: {ex.Message}", Resources.Error);
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
                RHMessageBox.ShowOKMessage($"{Resources.LoadTemplateError}: {ex.Message}", Resources.LoadTemplateError);
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
                        if (GetInvalidItemID(itemID))
                        {
                            invalidItemIDs.Add(itemID);
                        }
                    }
                }

                if (invalidItemIDs.Count > 0)
                {
                    string invalidItemIDsString = string.Join(", ", invalidItemIDs);
                    RHMessageBox.ShowOKMessage($"{Resources.TemplateInvalidId}: {invalidItemIDsString}", Resources.LoadTemplateError);
                    return;
                }

                ClearMailData();

                Sender = templateData.Sender;
                Recipient = templateData.Recipient;
                SelectAllCheckBoxChecked = templateData.SendToAll;
                MailContent = templateData.MailContent;
                AttachGold = templateData.AttachGold;
                ItemCharge = templateData.ItemCharge;
                ReturnDays = templateData.ReturnDays;

                if (templateData.ItemIDs != null)
                {
                    for (int i = 0; i < templateData.ItemIDs.Count; i++)
                    {
                        // Find the corresponding ItemData in the CachedItemDataList
                        ItemData? cachedItem = _cachedDataManager.CachedItemDataList?.FirstOrDefault(item => item.ID == templateData.ItemIDs[i]);

                        ItemData itemData = new()
                        {
                            SlotIndex = i,
                            ID = templateData.ItemIDs[i],
                            Name = cachedItem?.Name ?? "",
                            IconName = cachedItem?.IconName ?? "",
                            Branch = cachedItem?.Branch ?? 0,
                            Amount = templateData.ItemAmounts?[i] ?? 0,
                            Durability = templateData.Durabilities?[i] ?? 0,
                            EnhanceLevel = templateData.EnchantLevels?[i] ?? 0,
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

                        ItemDataList ??= [];
                        ItemDataList.Add(itemData);
                        SetItemProperties(itemData);
                    }
                }
            }
            else
            {
                RHMessageBox.ShowOKMessage(Resources.LoadTemplateJsonError, Resources.LoadTemplateError);
            }
        }
        else
        {
            RHMessageBox.ShowOKMessage(Resources.InvalidTemplate, Resources.LoadTemplateError);
        }
    }

    private bool GetInvalidItemID(int itemID)
    {
        return _cachedDataManager.CachedItemDataList == null || !_cachedDataManager.CachedItemDataList.Any(item => item.ID == itemID);
    }

    [RelayCommand]
    private void ClearMailData()
    {
        Recipient = default;
        Sender = "GM";
        MailContent = Resources.GameMasterInsertItem;
        SelectAllCheckBoxChecked = false;
        AttachGold = 0;
        ReturnDays = 7;
        ItemCharge = 0;

        ItemDataList?.Clear();

        ResetItemProperties(0);
        ResetItemProperties(1);
        ResetItemProperties(2);
    }
    #endregion

    #region Send Mail

    [RelayCommand]
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
            RHMessageBox.ShowOKMessage(Resources.EmptyRecipientDesc, Resources.EmptyRecipient);
            return;
        }

        if (string.IsNullOrWhiteSpace(mailSender))
        {
            RHMessageBox.ShowOKMessage(Resources.EmptySenderDesc, Resources.EmptySender);
            return;
        }

        string[] recipients = await _mailManager.GetRecipientsAsync(mailRecipient, sendToAllCharacters, sendToAllOnline, sendToAllOffline);
        if (recipients == null || recipients.Length == 0)
        {
            return;
        }

        string confirmationMessage = MailManager.GetConfirmationMessage(sendToAllCharacters, sendToAllOnline, sendToAllOffline, recipients);

        if (RHMessageBox.ConfirmMessage(confirmationMessage))
        {
            try
            {
                IsButtonEnabled = false;

                (List<string> successfulRecipients, List<string> failedRecipients) = await _mailManager.SendMailAsync(mailSender, message, gold, reqGold, returnDay, recipients, ItemDataList!);

                string successMessage = MailManager.GetSendMessage(sendToAllCharacters, sendToAllOnline, sendToAllOffline, successfulRecipients, failedRecipients);
                RHMessageBox.ShowOKMessage(successMessage, Resources.SendMail);
            }
            catch (Exception ex)
            {
                RHMessageBox.ShowOKMessage($"{Resources.SendMailError}: {ex.Message}", Resources.Error);
            }
            finally
            {
                IsButtonEnabled = true;
            }
        }
    }

    #endregion

    #region Properties

    [ObservableProperty]
    private bool _isButtonEnabled = true;

    [ObservableProperty]
    private string? _recipient;

    [ObservableProperty]
    private string? _sender = "GM";

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

    [ObservableProperty]
    private string? _itemName0 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName1 = Resources.AddItemDesc;

    [ObservableProperty]
    private string? _itemName2 = Resources.AddItemDesc;

    [ObservableProperty]
    private int? _itemAmount0;

    [ObservableProperty]
    private int? _itemAmount1;

    [ObservableProperty]
    private int? _itemAmount2;

    [ObservableProperty]
    private string? _itemIcon0;

    [ObservableProperty]
    private string? _itemIcon1;

    [ObservableProperty]
    private string? _itemIcon2;

    [ObservableProperty]
    private int? _itemIconBranch0;

    [ObservableProperty]
    private int? _itemIconBranch1;

    [ObservableProperty]
    private int? _itemIconBranch2;

    #endregion

}
