using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using System.Data;

namespace RHToolkit.ViewModels.Pages
{
    public partial class CouponViewModel : ObservableValidator, IRecipient<ItemDataMessage>
    {
        private readonly WindowsProviderService _windowsProviderService;
        private readonly IDatabaseService _databaseService;
        private readonly ISqLiteDatabaseService _sqLiteDatabaseService;
        private readonly Guid _token;

        public CouponViewModel(WindowsProviderService windowsProviderService, IDatabaseService databaseService, ISqLiteDatabaseService sqLiteDatabaseService)
        {
            _token = Guid.NewGuid();
            _windowsProviderService = windowsProviderService;
            _databaseService = databaseService;
            _sqLiteDatabaseService = sqLiteDatabaseService;
            WeakReferenceMessenger.Default.Register(this);
        }

        #region Read Coupon

        [RelayCommand]
        private async Task ReadCouponList()
        {

            if (!SqlCredentialValidator.ValidateCredentials())
            {
                return;
            }

            try
            {
                CouponList = null;
                CouponList = await _databaseService.ReadCouponListAsync();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}", "Error");
            }
        }
        #endregion

        #region Add Coupon

        [RelayCommand(CanExecute = nameof(CanExecuteAddCouponCommand))]
        private async Task AddCoupon()
        {
            if (ItemData == null || CouponCode == null) return;

            if (!ValidateCouponCode(CouponCode))
            {
                RHMessageBoxHelper.ShowOKMessage("The coupon code is invalid. It must have 20 characters and contain only letters and numbers.", "Invalid Coupon");
                return;
            }

            try
            {
                string couponCode = CouponCode.Replace("-", "");

                if (RHMessageBoxHelper.ConfirmMessage($"Add the coupon '{couponCode}' valid until '{ValidDate}'?"))
                {
                    if (await _databaseService.CouponExists(couponCode))
                    {
                        RHMessageBoxHelper.ShowOKMessage($"The coupon '{couponCode}' already exists.", "Duplicate Coupon");
                        return;
                    }

                    await _databaseService.AddCouponAsync(couponCode, ValidDate, ItemData);

                    RHMessageBoxHelper.ShowOKMessage("Coupon added successfully!", "Success");

                    await ReadCouponList();
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}");
            }
        }

        private static bool ValidateCouponCode(string couponCode)
        {
            // Remove dashes for validation
            string code = couponCode.Replace("-", "");

            // Check length
            if (code.Length != 20)
            {
                return false;
            }

            // Check if all characters are valid
            foreach (char c in code)
            {
                if (!Characters.Contains(c))
                {
                    return false;
                }
            }

            return true;
        }


        private bool CanExecuteAddCouponCommand()
        {
            return ItemData != null && !string.IsNullOrWhiteSpace(CouponCode);
        }

        #endregion

        #region Generate Coupon

        [RelayCommand]
        private void GenerateCoupon()
        {
            try
            {
                CouponCode = GenerateCouponCode();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}");
            }
        }

        private static readonly Random random = new();
        private const string Characters = "ABCDEFGHIJKLMNPQRSTUVWXYZ0123456789";

        public static string GenerateCouponCode()
        {
            StringBuilder coupon = new();
            for (int i = 0; i < 20; i++)
            {
                if (i > 0 && i % 5 == 0)
                {
                    coupon.Append('-');
                }
                coupon.Append(Characters[random.Next(0, Characters.Length)]);
            }
            return coupon.ToString();
        }
        #endregion

        #region Delete Coupon

        [RelayCommand(CanExecute = nameof(CanExecuteDeleteCouponCommand))]
        private async Task DeleteCoupon()
        {
            if (CouponList == null) return;

            try
            {

                if (SelectedCoupon != null)
                {
                    if (RHMessageBoxHelper.ConfirmMessage($"Delete the coupon?"))
                    {
                        int couponNumber = (int)SelectedCoupon["no"];

                        await _databaseService.DeleteCouponAsync(couponNumber);

                        RHMessageBoxHelper.ShowOKMessage($"Coupon deleted successfully!", "Success");

                        SelectedCoupon = null;

                        await ReadCouponList();
                    }
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error: {ex.Message}");
            }
        }

        private bool CanExecuteDeleteCouponCommand()
        {
            return CouponList != null && SelectedCoupon != null;
        }
        #endregion

        #region Add Item

        private readonly Dictionary<Guid, ItemWindow> _itemWindows = [];

        [RelayCommand]
        private void AddItem(string parameter)
        {
            if (int.TryParse(parameter, out int slotIndex))
            {
                if (!_sqLiteDatabaseService.ValidateDatabase())
                {
                    return;
                }

                ItemData? itemData = ItemData;

                itemData ??= new ItemData
                {
                    SlotIndex = slotIndex
                };

                var token = _token;

                if (_itemWindows.TryGetValue(token, out ItemWindow? existingWindow))
                {
                    if (existingWindow.WindowState == WindowState.Minimized)
                    {
                        existingWindow.WindowState = WindowState.Normal;
                    }

                    existingWindow.Focus();
                }
                else
                {
                    var itemWindow = _windowsProviderService.ShowInstance<ItemWindow>(true);
                    if (itemWindow != null)
                    {
                        itemWindow.Closed += (s, e) => _itemWindows.Remove(token);
                        _itemWindows[token] = itemWindow;
                    }
                }

                WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "ItemWindowViewModel", "Coupon", token));
            }
        }

        public void Receive(ItemDataMessage message)
        {
            if (message.Recipient == "CouponViewModel" && message.Token == _token)
            {
                var itemData = message.Value;

                ItemData = itemData;

                SetItemProperties(itemData);
            }
        }

        private void SetItemProperties(ItemData itemData)
        {
            string iconNameProperty = $"ItemIcon";
            string iconBranchProperty = $"ItemIconBranch";
            string nameProperty = $"ItemName";
            string itemAmountProperty = $"ItemAmount";

            GetType().GetProperty(iconNameProperty)?.SetValue(this, itemData.IconName);
            GetType().GetProperty(iconBranchProperty)?.SetValue(this, itemData.Branch);
            GetType().GetProperty(nameProperty)?.SetValue(this, itemData.Name);
            GetType().GetProperty(itemAmountProperty)?.SetValue(this, itemData.Amount);
        }
        #endregion

        #region Properties
        [ObservableProperty]
        private DataTable? _couponList;
        partial void OnCouponListChanged(DataTable? value)
        {
            DeleteCouponCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private DataRowView? _selectedCoupon;
        partial void OnSelectedCouponChanged(DataRowView? value)
        {
            DeleteCouponCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private ItemData? _itemData;
        partial void OnItemDataChanged(ItemData? value)
        {
            AddCouponCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private string? _couponCode;
        partial void OnCouponCodeChanged(string? value)
        {
            AddCouponCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private DateTime _validDate = DateTime.Today;

        [ObservableProperty]
        private string? _itemName = "Select a Item";

        public string ItemNameColor => FrameService.GetBranchColor(ItemIconBranch);

        [ObservableProperty]
        private int _itemAmount;

        [ObservableProperty]
        private string? _itemIcon;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ItemNameColor))]
        private int _itemIconBranch;

        #endregion
    }
}
