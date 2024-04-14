using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RHGMTool.Models;
using RHGMTool.Views;

namespace RHGMTool.ViewModels
{
    public partial class MailWindowViewModel : ObservableRecipient
    {
        public MailWindowViewModel()
        {
            WeakReferenceMessenger.Default.Register<MailData>(this, OnMailDataReceived);
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
                WeakReferenceMessenger.Default.Send(mailData);
                itemWindow.ShowDialog();
            }
        }

        private void OnMailDataReceived(object recipient, object? message)
        {
            if (message is MailData mailData)
            {
                // Add or update the MailData in the Mail list
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

                OnPropertyChanged(nameof(ItemIcon1));
                OnPropertyChanged(nameof(ItemIcon2));
                OnPropertyChanged(nameof(ItemIcon3));
                OnPropertyChanged(nameof(ItemIconBranch1));
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
        private string? _itemIcon1;

        [ObservableProperty]
        private string? _itemIcon2;

        [ObservableProperty]
        private string? _itemIcon3;

        [ObservableProperty]
        private int? _itemIconBranch1;

        [ObservableProperty]
        private int _itemIconBranch2;

        [ObservableProperty]
        private int _itemIconBranch3;

    }
}
