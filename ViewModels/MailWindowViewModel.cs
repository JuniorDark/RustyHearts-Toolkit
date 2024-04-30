using Microsoft.Win32;
using Newtonsoft.Json;
using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Services;
using RHToolkit.Views;
using System.Data;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace RHToolkit.ViewModels
{
    public partial class MailWindowViewModel : ObservableObject, IRecipient<ItemDataMessage>
    {
        private readonly WindowsProviderService _windowsProviderService;
        private readonly List<ItemData>? _cachedItemDataList = [];
        private readonly IDatabaseService _databaseService;
        private readonly ISnackbarService _snackbarService;

        public MailWindowViewModel(WindowsProviderService windowsProviderService, IDatabaseService databaseService, ISnackbarService snackbarService)
        {
            _windowsProviderService = windowsProviderService;
            _databaseService = databaseService;
            _snackbarService = snackbarService;

            if (ItemDataManager.Instance.CachedItemDataList == null)
            {
                ItemDataManager.Instance.InitializeCachedLists();
            }

            _cachedItemDataList = ItemDataManager.Instance.CachedItemDataList;


            WeakReferenceMessenger.Default.Register(this);
        }

        #region Add Item

        private ItemWindow? _itemWindowInstance;

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

                if (_itemWindowInstance == null)
                {
                    _windowsProviderService.Show<ItemWindow>();
                    _itemWindowInstance = Application.Current.Windows.OfType<ItemWindow>().FirstOrDefault();
                    _itemWindowInstance.Closed += (sender, args) => _itemWindowInstance = null;
                }

                WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, ViewModelType.ItemWindowViewModel));
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
        #endregion

        #region Remove Item
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
                    ItemIDs = Item.Select(item => item.ID).ToList(),
                    ItemAmounts = Item.Select(item => item.Amount).ToList(),
                    Durabilities = Item.Select(item => item.MaxDurability).ToList(),
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

                    System.Windows.MessageBox.Show("Mail template saved successfully.", "Success", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving template: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
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
                    System.Windows.MessageBox.Show($"Error loading template: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
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
                            System.Windows.MessageBox.Show($"Template have invalid ItemIDs.\nInvalid ItemIDs in the template: {invalidItemIDsString}", "Error loading template", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
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
                        System.Windows.MessageBox.Show("Failed to load JSON template or JSON is empty.", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Invalid template file.", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading json template: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
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
        #endregion

        #region Send Mail

        private static readonly char[] separator = [','];

        [RelayCommand]
        private void SendMail()
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
                _snackbarService.Show(
                "Empty recipient.",
                "Enter a Recipient.",
                ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.MailWarning24),
                TimeSpan.FromSeconds(5)
                );
                return;
            }
            if (string.IsNullOrEmpty(Sender))
            {
                _snackbarService.Show(
                "Empty sender.",
                "Enter a Sender.",
                ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.MailWarning24),
                TimeSpan.FromSeconds(5)
                );
                return;
            }

            if (sendToAllCharacters)
            {

                recipients = _databaseService.GetAllCharacterNames();
            }
            else
            {
                // Split recipients by comma and trim any extra spaces
                recipients = Recipient.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(r => r.Trim())
                                        .ToArray();

                // Validate if any recipient is empty or contains non-letter characters
                if (recipients.Any(string.IsNullOrEmpty) || recipients.Any(r => !r.All(char.IsLetter)))
                {
                    _snackbarService.Show(
                    "Invalid recipient.",
                    "Recipient names must contain only letters and cannot be empty",
                    ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.MailWarning24),
                    TimeSpan.FromSeconds(5)
                    );
                    return;
                }

                // Check for duplicate recipients
                HashSet<string> uniqueRecipients = new(StringComparer.OrdinalIgnoreCase);
                foreach (var recipient in recipients)
                {
                    if (!uniqueRecipients.Add(recipient))
                    {
                        _snackbarService.Show(
                        "Duplicate recipient.",
                        $"Duplicate recipient found: {recipient}",
                        ControlAppearance.Caution,
                        new SymbolIcon(SymbolRegular.MailWarning24),
                        TimeSpan.FromSeconds(5)
                        );
                        return;
                    }
                }
            }

            string confirmationMessage = sendToAllCharacters
                ? "Send this mail to all characters?"
                : $"Send this mail to the following recipients?\n{string.Join(", ", recipients)}";

            if (ConfirmMessage($"{confirmationMessage}"))
            {
                string recipient = string.Empty;

                try
                {
                    foreach (var currentRecipient in recipients)
                    {
                        Guid mailId = Guid.NewGuid();

                        recipient = currentRecipient;

                        (Guid? senderCharacterId, Guid? senderAuthId, string? senderWindyCode) = _databaseService.GetCharacterInfo(mailSender);
                        (Guid? recipientCharacterId, Guid? recipientAuthId, string? recipientWindyCode) = _databaseService.GetCharacterInfo(recipient);

                        if (senderCharacterId == null && createType == 5)
                        {
                            _snackbarService.Show(
                           "Invalid sender.",
                           $"The sender ({mailSender}) does not exist.\nThe sender name must be a valid character name for billing mail.",
                           ControlAppearance.Danger,
                           new SymbolIcon(SymbolRegular.MailWarning24),
                           TimeSpan.FromSeconds(5)
                           );
                            return;
                        }

                        if (senderCharacterId == recipientCharacterId)
                        {
                            _snackbarService.Show(
                           "Failed to send Mail.",
                           $"The sender and recipient cannot be the same.",
                           ControlAppearance.Danger,
                           new SymbolIcon(SymbolRegular.MailWarning24),
                           TimeSpan.FromSeconds(5)
                           );
                            return;
                        }

                        if (recipientCharacterId == null || recipientAuthId == null)
                        {
                            _snackbarService.Show(
                           "Failed to send Mail.",
                           $"The recipient ({recipient}) does not exist.",
                           ControlAppearance.Danger,
                           new SymbolIcon(SymbolRegular.MailWarning24),
                           TimeSpan.FromSeconds(5)
                           );
                            continue; // Skip to the next recipient
                        }

                        if (senderCharacterId == null)
                        {
                            senderCharacterId = Guid.Parse("00000000-0000-0000-0000-000000000000");
                        }

                        string Modify = "";

                        if (gold > 0)
                        {
                            Modify += $"[<font color=blue>Attach Gold - {gold}</font>]<br></font>";
                        }

                        if (Item != null)
                        {
                            // Iterate over ItemDataList and insert item data for each ItemData
                            foreach (ItemData itemData in Item)
                            {
                                try
                                {
                                    if (itemData.ID != 0)
                                    {
                                        Modify += $"[<font color=blue>Attach Item - {itemData.ID} ({itemData.Amount})</font>]<br></font>";

                                        _databaseService.InsertMailItem(itemData, recipientAuthId, recipientCharacterId, mailId, itemData.SlotIndex);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _snackbarService.Show(
                                   "Attaching item.",
                                   $"Error attaching item to mail: {ex.Message}",
                                   ControlAppearance.Danger,
                                   new SymbolIcon(SymbolRegular.MailWarning24),
                                   TimeSpan.FromSeconds(5)
                                   );
                                    return;
                                }
                            }
                        }

                        _databaseService.InsertMail(recipientAuthId, senderCharacterId, mailSender, recipient, content, gold, returnDay, reqGold, mailId, createType);

                        _databaseService.GMAudit(recipientWindyCode, recipientCharacterId, recipient, "Send Mail", $"<font color=blue>Send Mail</font>]<br><font color=red>Sender: RHToolkit: {senderCharacterId}, Recipient: {recipient}, GUID:{{{recipientCharacterId}}}<br></font>" + Modify);
                    }

                    if (sendToAllCharacters)
                    {
                        _snackbarService.Show(
                                  "Success.",
                                  $"The mail has been sent successfully to all characters. Please re-login the character/change game server to view the mail.",
                                  ControlAppearance.Success,
                                  new SymbolIcon(SymbolRegular.MailCheckmark24),
                                  TimeSpan.FromSeconds(5)
                                  );
                    }
                    else
                    { 
                        _snackbarService.Show(
                                  "Success.",
                                  $"The mail has been sent successfully. Please re-login the character/change game server to view the mail.",
                                  ControlAppearance.Success,
                                  new SymbolIcon(SymbolRegular.MailCheckmark24),
                                  TimeSpan.FromSeconds(5)
                                  );
                    }
                }
                catch (Exception ex)
                {
                    _snackbarService.Show(
                                   "Error",
                                   $"Error sending mail: {ex.Message}",
                                   ControlAppearance.Danger,
                                   new SymbolIcon(SymbolRegular.MailWarning24),
                                   TimeSpan.FromSeconds(5)
                                   );
                }
            }
        }

        public static bool ConfirmMessage(string message)
        {
            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(message, "Confirmation", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == System.Windows.MessageBoxResult.Yes;
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
        private string? _mailContent = "GameMaster InsertItem";

        [ObservableProperty]
        private int _attachGold;

        [ObservableProperty]
        private int _returnDays = 7;

        [ObservableProperty]
        private int _itemCharge;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRecipientEabled))]
        private bool _sendToAll = false;
        partial void OnSendToAllChanged(bool value)
        {
            if (value == true)
            {
                IsRecipientEabled = false;
            }
            else
            {
                IsRecipientEabled = true;
            }
        }

        [ObservableProperty]
        private bool _isRecipientEabled = true;

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

        #endregion

    }
}
