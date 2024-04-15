using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RHGMTool.Messages;
using RHGMTool.Models;
using RHGMTool.Views;

namespace RHGMTool.ViewModels
{
    public partial class MailWindowViewModel : ObservableObject, IRecipient<MailItemData>
    {
        public MailWindowViewModel()
        {
            WeakReferenceMessenger.Default.Register<MailItemData>(this);
        }

        [RelayCommand]
        private void OpenItemWindow(string parameter)
        {
            if (int.TryParse(parameter, out int slotIndex))
            {
                // Create a MailData object to pass to the ItemWindow
                MailData? mailData = Mail?.FirstOrDefault(m => m.SlotIndex == slotIndex);

                // If the MailData for the slot index is not found, create a new MailData with only the SlotIndex
                mailData ??= new MailData
                {
                    SlotIndex = slotIndex
                };

                // Open the ItemWindow
                ItemWindow itemWindow = new();
                // Send the MailData to the ItemWindow
                WeakReferenceMessenger.Default.Send(new MailItemData(mailData, ViewModelType.ItemViewModel));
                itemWindow.ShowDialog();
            }
        }

        public void Receive(MailItemData message)
        {
            if (message.Recipient == ViewModelType.MailWindowViewModel)
            {
                var mailData = message.Value;

                Mail ??= [];

                var existingMailIndex = Mail.FindIndex(m => m.SlotIndex == mailData.SlotIndex);
                if (existingMailIndex != -1)
                {
                    // Remove the existing MailData with the same SlotIndex
                    Mail.RemoveAt(existingMailIndex);
                }

                // Add the new MailData to the Mail list
                Mail.Add(mailData);

                UpdateMailData();
            }
        }

        [ObservableProperty]
        private List<MailData>? _mail;
        partial void OnMailChanged(List<MailData>? value)
        {
            UpdateMailData();
        }

        private void UpdateMailData()
        {
            if (Mail != null)
            {
                ItemIcon1 = Mail.FirstOrDefault(data => data.SlotIndex == 0)?.IconName;
                ItemIcon2 = Mail.FirstOrDefault(data => data.SlotIndex == 1)?.IconName;
                ItemIcon3 = Mail.FirstOrDefault(data => data.SlotIndex == 2)?.IconName;
                ItemIconBranch1 = Mail.FirstOrDefault(data => data.SlotIndex == 0)?.ItemBranch;
                ItemIconBranch2 = Mail.FirstOrDefault(data => data.SlotIndex == 1)?.ItemBranch;
                ItemIconBranch3 = Mail.FirstOrDefault(data => data.SlotIndex == 2)?.ItemBranch;
                ItemName1 = Mail.FirstOrDefault(data => data.SlotIndex == 0)?.ItemName;
                ItemName2 = Mail.FirstOrDefault(data => data.SlotIndex == 1)?.ItemName;
                ItemName3 = Mail.FirstOrDefault(data => data.SlotIndex == 2)?.ItemName;
                ItemAmount1 = Mail.FirstOrDefault(data => data.SlotIndex == 0)?.Amount;
                ItemAmount2 = Mail.FirstOrDefault(data => data.SlotIndex == 1)?.Amount;
                ItemAmount3 = Mail.FirstOrDefault(data => data.SlotIndex == 2)?.Amount;
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
        private string? _itemName1;

        [ObservableProperty]
        private string? _itemName2;

        [ObservableProperty]
        private string? _itemName3;

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
