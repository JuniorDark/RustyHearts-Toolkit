﻿using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.ViewModels.Controls;
using System.Data;

namespace RHToolkit.ViewModels.Pages
{
    public partial class CouponViewModel : ObservableValidator, IRecipient<ItemDataMessage>
    {
        private readonly IWindowsService _windowsService;
        private readonly IDatabaseService _databaseService;
        private readonly ISqLiteDatabaseService _sqLiteDatabaseService;
        private readonly ItemDataManager _itemDataManager;
        private readonly Guid _token;

        public CouponViewModel(IWindowsService windowsService, IDatabaseService databaseService, ISqLiteDatabaseService sqLiteDatabaseService, ItemDataManager itemDataManager)
        {
            _token = Guid.NewGuid();
            _windowsService = windowsService;
            _databaseService = databaseService;
            _sqLiteDatabaseService = sqLiteDatabaseService;
            _itemDataManager = itemDataManager;
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
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
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
                RHMessageBoxHelper.ShowOKMessage(Resources.CouponInvalidMessage, Resources.Error);
                return;
            }

            try
            {
                string couponCode = CouponCode.Replace("-", "");

                if (RHMessageBoxHelper.ConfirmMessage(Resources.CouponAddMessage))
                {
                    if (await _databaseService.CouponExists(couponCode))
                    {
                        RHMessageBoxHelper.ShowOKMessage(Resources.CouponDuplicateMessage, Resources.Error);
                        return;
                    }

                    await _databaseService.AddCouponAsync(couponCode, ValidDate, ItemData);

                    RHMessageBoxHelper.ShowOKMessage(Resources.CouponAddSuccessMessage, Resources.Success);

                    await ReadCouponList();
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error} : {ex.Message}");
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
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error} : {ex.Message}");
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
                    int couponNumber = (int)SelectedCoupon["no"];

                    if (RHMessageBoxHelper.ConfirmMessage(string.Format(Resources.CouponDeleteMessage, couponNumber)))
                    {
                        await _databaseService.DeleteCouponAsync(couponNumber);

                        RHMessageBoxHelper.ShowOKMessage(Resources.CouponDeleteSuccessMessage, Resources.Success);

                        SelectedCoupon = null;

                        await ReadCouponList();
                    }
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error} : {ex.Message}");
            }
        }

        private bool CanExecuteDeleteCouponCommand()
        {
            return CouponList != null && SelectedCoupon != null;
        }
        #endregion

        #region Add Item

        [RelayCommand]
        private void AddItem()
        {
            try
            {
                var itemData = new ItemData
                {
                    IsNewItem = true
                };

                var token = _token;

                _windowsService.OpenItemWindow(token, "CouponItem", itemData);
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        public void Receive(ItemDataMessage message)
        {
            if (message.Recipient == "CouponWindow" && message.Token == _token)
            {
                var itemData = message.Value;
                ItemData = itemData;
                var itemDataViewModel= _itemDataManager.GetItemData(itemData);

                ItemDataViewModel = itemDataViewModel;
            }
        }

        #endregion

        #region Remove Item

        [RelayCommand]
        private void RemoveItem(string parameter)
        {
            ItemData = null;
            ItemDataViewModel = null;
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
        [NotifyPropertyChangedFor(nameof(ItemName))]
        private ItemDataViewModel? _itemDataViewModel;
        partial void OnItemDataViewModelChanged(ItemDataViewModel? value)
        {
            ItemName = value != null ? value.ItemName : Resources.SelectItem;
        }

        [ObservableProperty]
        private string? _itemName = Resources.SelectItem;

        #endregion
    }
}
