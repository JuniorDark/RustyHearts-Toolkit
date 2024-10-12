﻿using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Views.Windows;

namespace RHToolkit.Services;

public class WindowsService(WindowsProviderService windowsProviderService) : IWindowsService
{
    private readonly WindowsProviderService _windowsProviderService = windowsProviderService;

    #region Windows

    #region Item Window

    private readonly Dictionary<Guid, Window> _itemWindows = [];

    public void OpenItemWindow(Guid token, string messageType, ItemData itemData, CharacterData? characterData = null)
    {
        if (_itemWindows.TryGetValue(token, out Window? existingWindow))
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
                _openWindowsCount++;
                itemWindow.Closed += (sender, args) =>
                {
                    _itemWindows.Remove(token);
                    _openWindowsCount--;
                };
                _itemWindows[token] = itemWindow;
            }
        }

        if (characterData != null)
        {
            WeakReferenceMessenger.Default.Send(new CharacterDataMessage(characterData, "ItemWindow", token));
        }

        WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "ItemWindow", messageType, token));
    }

    #endregion

    #region Npc Shop Window

    private readonly Dictionary<Guid, Window> _npcShopWindows = [];

    public void OpenNpcShopWindow(Guid token, NameID shopID, NameID? shopTitle)
    {
        if (_npcShopWindows.TryGetValue(token, out Window? existingWindow))
        {
            if (existingWindow.WindowState == WindowState.Minimized)
            {
                existingWindow.WindowState = WindowState.Normal;
            }

            existingWindow.Focus();
        }
        else
        {
            var npcShopWindow = _windowsProviderService.ShowInstance<NpcShopWindow>(true);
            if (npcShopWindow != null)
            {
                _openWindowsCount++;
                npcShopWindow.Closed += (sender, args) =>
                {
                    _npcShopWindows.Remove(token);
                    _openWindowsCount--;
                };
                _npcShopWindows[token] = npcShopWindow;
            }
        }

        WeakReferenceMessenger.Default.Send(new NpcShopMessage(shopID, token, shopTitle));
    }

    private readonly Dictionary<Guid, Window> _itemMixWindows = [];

    public void OpenItemMixWindow(Guid token, string group, string? messageType)
    {
        if (_itemMixWindows.TryGetValue(token, out Window? existingWindow))
        {
            if (existingWindow.WindowState == WindowState.Minimized)
            {
                existingWindow.WindowState = WindowState.Normal;
            }

            existingWindow.Focus();
        }
        else
        {
            var itemMixWindow = _windowsProviderService.ShowInstance<ItemMixWindow>(true);
            if (itemMixWindow != null)
            {
                _openWindowsCount++;
                itemMixWindow.Closed += (sender, args) =>
                {
                    _itemMixWindows.Remove(token);
                    _openWindowsCount--;
                };
                _itemMixWindows[token] = itemMixWindow;
            }
        }

        WeakReferenceMessenger.Default.Send(new ItemMixMessage(group, token, messageType));
    }
    #endregion

    #region Database Windows

    private static int _openWindowsCount = 0;
    private readonly Dictionary<Guid, Window> _characterWindows = [];
    private readonly Dictionary<Guid, Window> _equipmentWindows = [];
    private readonly Dictionary<Guid, Window> _inventoryWindows = [];
    private readonly Dictionary<Guid, Window> _storageWindows = [];
    private readonly Dictionary<Guid, Window> _titleWindows = [];
    private readonly Dictionary<Guid, Window> _sanctionWindows = [];
    private readonly Dictionary<Guid, Window> _fortuneWindows = [];

    public static int OpenWindowsCount { get => _openWindowsCount; set => _openWindowsCount = value; }

    private static void OpenWindow<TWindow>(CharacterData characterData, Func<Window?> windowCreator, Dictionary<Guid, Window> windowsDictionary, string errorMessage)
    {
        if (!SqlCredentialValidator.ValidateCredentials())
        {
            return;
        }

        try
        {
            if (windowsDictionary.TryGetValue(characterData.CharacterID, out Window? existingWindow))
            {
                if (existingWindow.WindowState == WindowState.Minimized)
                {
                    existingWindow.WindowState = WindowState.Normal;
                }

                existingWindow.Focus();
            }
            else
            {
                var window = windowCreator.Invoke();
                if (window != null)
                {
                    windowsDictionary.Add(characterData.CharacterID, window);
                    _openWindowsCount++;
                    window.Closed += (sender, args) =>
                    {
                        windowsDictionary.Remove(characterData.CharacterID);
                        _openWindowsCount--;
                    };
                }
            }

            WeakReferenceMessenger.Default.Send(new CharacterDataMessage(characterData, typeof(TWindow).Name, characterData.CharacterID));
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {errorMessage}: {ex.Message}", "Error");
        }
    }

    public void OpenCharacterWindow(CharacterData characterData)
    {
        OpenWindow<CharacterWindow>(characterData, () => _windowsProviderService.ShowInstance<CharacterWindow>(true), _characterWindows, "CharacterWindow");
    }

    public void OpenEquipmentWindow(CharacterData characterData)
    {
        OpenWindow<EquipmentWindow>(characterData, () => _windowsProviderService.ShowInstance<EquipmentWindow>(true), _equipmentWindows, "EquipmentWindow");
    }

    public void OpenInventoryWindow(CharacterData characterData)
    {
        OpenWindow<InventoryWindow>(characterData, () => _windowsProviderService.ShowInstance<InventoryWindow>(true), _inventoryWindows, "InventoryWindow");
    }

    public void OpenStorageWindow(CharacterData characterData)
    {
        OpenWindow<StorageWindow>(characterData, () => _windowsProviderService.ShowInstance<StorageWindow>(true), _storageWindows, "StorageWindow");
    }

    public void OpenTitleWindow(CharacterData characterData)
    {
        OpenWindow<TitleWindow>(characterData, () => _windowsProviderService.ShowInstance<TitleWindow>(true), _titleWindows, "TitleWindow");
    }

    public void OpenSanctionWindow(CharacterData characterData)
    {
        OpenWindow<SanctionWindow>(characterData, () => _windowsProviderService.ShowInstance<SanctionWindow>(true), _sanctionWindows, "SanctionWindow");
    }

    public void OpenFortuneWindow(CharacterData characterData)
    {
        OpenWindow<FortuneWindow>(characterData, () => _windowsProviderService.ShowInstance<FortuneWindow>(true), _fortuneWindows, "FortuneWindow");
    }

    #endregion

    #region Rare Card Reward Window

    private readonly Dictionary<Guid, Window> _rareCardRewardWindows = [];

    public void OpenRareCardRewardWindow(Guid token, int id, string? messageType)
    {
        if (_rareCardRewardWindows.TryGetValue(token, out Window? existingWindow))
        {
            if (existingWindow.WindowState == WindowState.Minimized)
            {
                existingWindow.WindowState = WindowState.Normal;
            }

            existingWindow.Focus();
        }
        else
        {
            var rareCardRewardWindow = _windowsProviderService.ShowInstance<RareCardRewardWindow>(true);
            if (rareCardRewardWindow != null)
            {
                _openWindowsCount++;
                rareCardRewardWindow.Closed += (sender, args) =>
                {
                    _rareCardRewardWindows.Remove(token);
                    _openWindowsCount--;
                };
                _rareCardRewardWindows[token] = rareCardRewardWindow;
            }
        }

        WeakReferenceMessenger.Default.Send(new IDMessage(id, token, messageType));
    }
    #endregion

    #endregion
}
