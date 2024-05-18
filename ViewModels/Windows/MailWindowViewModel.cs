using Microsoft.Win32;
using Newtonsoft.Json;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using System.Data;

namespace RHToolkit.ViewModels.Windows;

public partial class MailWindowViewModel : ObservableObject, IRecipient<ItemDataMessage>
{
    private readonly WindowsProviderService _windowsProviderService;
    private readonly IDatabaseService _databaseService;

    public MailWindowViewModel(WindowsProviderService windowsProviderService, IDatabaseService databaseService)
    {
        _windowsProviderService = windowsProviderService;
        _databaseService = databaseService;

        if (ItemDataManager.Instance.CachedItemDataList == null)
        {
            ItemDataManager.Instance.InitializeCachedLists();
        }

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
                WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData,"ItemWindowViewModel", "Mail"));
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
                SendToAll = SendToAll,
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
                RHMessageBox.ShowOKMessage($"{Resources.LoadTemplateError}: {ex.Message}", Resources.Error);
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
                        RHMessageBox.ShowOKMessage($"{Resources.TemplateInvalidId}: {invalidItemIDsString}", Resources.LoadTemplateError);
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
                            ItemData? cachedItem = ItemDataManager.Instance.CachedItemDataList?.FirstOrDefault(item => item.ID == templateData.ItemIDs[i]);

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
                    RHMessageBox.ShowOKMessage(Resources.LoadTemplateJsonError, Resources.Error);
                }
            }
            else
            {
                RHMessageBox.ShowOKMessage(Resources.InvalidTemplate, Resources.Error);
            }
        }
        catch (Exception ex)
        {
            RHMessageBox.ShowOKMessage($"{Resources.LoadTemplateError}: {ex.Message}", Resources.Error);
        }
    }

    private static List<int> GetInvalidItemIDs(List<int>? itemIDs)
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

            if (ItemDataManager.Instance.CachedItemDataList != null)
            {
                foreach (ItemData item in ItemDataManager.Instance.CachedItemDataList)
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

        }

        return invalidItemIDs;
    }

    [RelayCommand]
    private void ClearTemplate()
    {
        Recipient = default;
        Sender = "GM";
        MailContent = Resources.GameMasterInsertItem;
        SendToAll = false;
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

    private static readonly char[] separator = [','];

    [RelayCommand]
    private async Task SendMailAsync()
    {
        string? mailSender = Sender;
        string? content = MailContent;
        if (content != null)
        {
            content = content.Replace("'", "''");
        }
        content += $"<br><br><right>{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

        int gold = AttachGold;
        int reqGold = ItemCharge;
        int returnDay = ReturnDays;
        int createType = (reqGold != 0) ? 5 : 0;
        List<string> failedRecipients = [];
        bool sendToAllCharacters = SendToAll;
        string[] recipients;

        if (string.IsNullOrEmpty(Recipient) && !sendToAllCharacters)
        {
            RHMessageBox.ShowOKMessage(Resources.EmptyRecipientDesc, Resources.EmptyRecipient);
            return;
        }
        if (string.IsNullOrEmpty(mailSender))
        {
            RHMessageBox.ShowOKMessage(Resources.EmptySenderDesc, Resources.EmptySender);
            return;
        }

        if (sendToAllCharacters)
        {
            recipients = await _databaseService.GetAllCharacterNamesAsync();
        }
        else
        {
            // Split recipients by comma and trim any extra spaces
            recipients = Recipient!.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(r => r.Trim())
                                    .ToArray();

            // Validate if any recipient is empty or contains non-letter characters
            if (recipients.Any(string.IsNullOrEmpty) || recipients.Any(r => !r.All(char.IsLetter)))
            {
                RHMessageBox.ShowOKMessage(Resources.InvalidRecipientDesc, Resources.InvalidRecipient);
                return;
            }

            // Check for duplicate recipients
            HashSet<string> uniqueRecipients = new(StringComparer.OrdinalIgnoreCase);
            foreach (var recipient in recipients)
            {
                if (!uniqueRecipients.Add(recipient))
                {
                    RHMessageBox.ShowOKMessage($"{Resources.DuplicateRecipientDesc}: {recipient}", Resources.DuplicateRecipient);
                    return;
                }
            }
        }

        string confirmationMessage = sendToAllCharacters
            ? Resources.SendMailMessageAll
            : $"{Resources.SendMailMessage}\n{string.Join(", ", recipients)}";

        if (RHMessageBox.ConfirmMessage($"{confirmationMessage}"))
        {
            try
            {
                IsButtonEnabled = false;

                foreach (var currentRecipient in recipients)
                {
                    Guid mailId = Guid.NewGuid();
                    string recipient = currentRecipient;

                    (Guid senderCharacterId, Guid senderAuthId, string senderWindyCode) = await _databaseService.GetCharacterInfoAsync(mailSender);
                    (Guid recipientCharacterId, Guid recipientAuthId, string recipientWindyCode) = await _databaseService.GetCharacterInfoAsync(recipient);

                    if (senderCharacterId == Guid.Empty && createType == 5)
                    {
                        string invalidSenderMessage = string.Format(Resources.InvalidSenderDesc, mailSender);
                        RHMessageBox.ShowOKMessage(invalidSenderMessage, Resources.InvalidSender);
                        return;
                    }

                    if (senderCharacterId == recipientCharacterId)
                    {
                        RHMessageBox.ShowOKMessage(Resources.SendMailSameName, Resources.FailedSendMail);
                        return;
                    }

                    if (recipientCharacterId == Guid.Empty || recipientAuthId == Guid.Empty)
                    {
                        string invalidRecipientMessage = string.Format(Resources.NonExistentRecipient, recipient);
                        RHMessageBox.ShowOKMessage(invalidRecipientMessage, Resources.FailedSendMail);
                        continue;
                    }

                    if (senderCharacterId == Guid.Empty)
                    {
                        senderCharacterId = Guid.Parse("00000000-0000-0000-0000-000000000000");
                    }

                    string modify = "";

                    if (gold > 0)
                    {
                        modify += $"[<font color=blue>{Resources.GMAuditAttachGold} - {gold}</font>]<br></font>";
                    }

                    if (ItemDataList != null)
                    {
                        // Iterate over ItemDataList and insert item data for each ItemData
                        foreach (ItemData itemData in ItemDataList)
                        {
                            try
                            {
                                if (itemData.ID != 0)
                                {
                                    modify += $"[<font color=blue>{Resources.GMAuditAttachItem} - {itemData.ID} ({itemData.Amount})</font>]<br></font>";

                                    await _databaseService.InsertMailItemAsync(itemData, recipientAuthId, recipientCharacterId, mailId, itemData.SlotIndex);
                                }
                            }
                            catch (Exception ex)
                            {
                                RHMessageBox.ShowOKMessage($"{Resources.AttachItemErrorDesc}: {ex.Message}", Resources.AttachItemErrorDesc);
                                return;
                            }
                        }
                    }

                    await _databaseService.InsertMailAsync(recipientAuthId, senderCharacterId, mailSender!, recipient, content, gold, returnDay, reqGold, mailId, createType);

                   await _databaseService.GMAuditAsync(recipientWindyCode!, recipientCharacterId, recipient, Resources.SendMail, $"<font color=blue>{Resources.SendMail}</font>]<br><font color=red>{Resources.Sender}: RHToolkit: {senderCharacterId}, {Resources.Recipient}: {recipient}, GUID:{{{recipientCharacterId}}}<br></font>" + modify);
                }

                if (sendToAllCharacters)
                {
                    RHMessageBox.ShowOKMessage(Resources.SendMailMessageAllSuccess, Resources.Success);
                }
                else
                {
                    RHMessageBox.ShowOKMessage(Resources.SendMailMessageSuccess, Resources.Success);
                }
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
    [NotifyPropertyChangedFor(nameof(IsRecipientEnabled))]
    private bool _sendToAll = false;
    partial void OnSendToAllChanged(bool value)
    {
        if (value == true)
        {
            IsRecipientEnabled = false;
        }
        else
        {
            IsRecipientEnabled = true;
        }
    }

    [ObservableProperty]
    private bool _isRecipientEnabled = true;

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
