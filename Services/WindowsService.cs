using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Views.Windows;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Services;

/// <summary>
/// Provides methods to manage and open various windows in the application.
/// </summary>
public class WindowsService(WindowsProviderService windowsProviderService) : IWindowsService
{
    private readonly WindowsProviderService _windowsProviderService = windowsProviderService;

    #region Windows

    #region Item Window

    private readonly Dictionary<Guid, Window> _itemWindows = [];

    /// <summary>
    /// Opens an item window with the specified parameters.
    /// </summary>
    /// <param name="token">The unique token for the window.</param>
    /// <param name="messageType">The type of the message.</param>
    /// <param name="itemData">The item data to display.</param>
    /// <param name="characterData">The character data to display, if any.</param>
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

    #region Skill Window

    private readonly Dictionary<Guid, Window> _skillWindows = [];

    /// <summary>
    /// Opens a skill window with the specified parameters.
    /// </summary>
    /// <param name="token">The unique token for the window.</param>
    /// <param name="messageType">The type of the message.</param>
    /// <param name="skillData">The skill data to display.</param>
    /// <param name="characterData">The character data to display, if any.</param>
    public void OpenSkillWindow(Guid token, string messageType, SkillData skillData, CharacterData? characterData = null)
    {
        if (_skillWindows.TryGetValue(token, out Window? existingWindow))
        {
            if (existingWindow.WindowState == WindowState.Minimized)
            {
                existingWindow.WindowState = WindowState.Normal;
            }

            existingWindow.Focus();
        }
        else
        {
            var skillWindow = _windowsProviderService.ShowInstance<SkillWindow>(true);
            if (skillWindow != null)
            {
                _openWindowsCount++;
                skillWindow.Closed += (sender, args) =>
                {
                    _skillWindows.Remove(token);
                    _openWindowsCount--;
                };
                _skillWindows[token] = skillWindow;
            }
        }

        if (characterData != null)
        {
            WeakReferenceMessenger.Default.Send(new CharacterDataMessage(characterData, "SkillWindow", token));
        }

        WeakReferenceMessenger.Default.Send(new SkillDataMessage(skillData, "SkillWindow", messageType, token));
    }

    #endregion

    #region Npc Shop Window

    private readonly Dictionary<Guid, Window> _npcShopWindows = [];

    /// <summary>
    /// Opens an NPC shop window with the specified parameters.
    /// </summary>
    /// <param name="token">The unique token for the window.</param>
    /// <param name="shopID">The ID of the shop.</param>
    /// <param name="shopTitle">The title of the shop, if any.</param>
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

    /// <summary>
    /// Opens an item mix window with the specified parameters.
    /// </summary>
    /// <param name="token">The unique token for the window.</param>
    /// <param name="group">The group of the item mix.</param>
    /// <param name="messageType">The type of the message, if any.</param>
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

    /// <summary>
    /// Opens a window of the specified type with the given character data.
    /// </summary>
    /// <typeparam name="TWindow">The type of the window to open.</typeparam>
    /// <param name="characterData">The character data to display.</param>
    /// <param name="windowCreator">The function to create the window instance.</param>
    /// <param name="windowsDictionary">The dictionary to store the window instances.</param>
    /// <param name="errorMessage">The error message to display in case of an exception.</param>
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

    /// <summary>
    /// Opens a character window with the specified character data.
    /// </summary>
    /// <param name="characterData">The character data to display.</param>
    public void OpenCharacterWindow(CharacterData characterData)
    {
        OpenWindow<CharacterWindow>(characterData, () => _windowsProviderService.ShowInstance<CharacterWindow>(true), _characterWindows, "CharacterWindow");
    }

    /// <summary>
    /// Opens an equipment window with the specified character data.
    /// </summary>
    /// <param name="characterData">The character data to display.</param>
    public void OpenEquipmentWindow(CharacterData characterData)
    {
        OpenWindow<EquipmentWindow>(characterData, () => _windowsProviderService.ShowInstance<EquipmentWindow>(true), _equipmentWindows, "EquipmentWindow");
    }

    /// <summary>
    /// Opens an inventory window with the specified character data.
    /// </summary>
    /// <param name="characterData">The character data to display.</param>
    public void OpenInventoryWindow(CharacterData characterData)
    {
        OpenWindow<InventoryWindow>(characterData, () => _windowsProviderService.ShowInstance<InventoryWindow>(true), _inventoryWindows, "InventoryWindow");
    }

    /// <summary>
    /// Opens a storage window with the specified character data.
    /// </summary>
    /// <param name="characterData">The character data to display.</param>
    public void OpenStorageWindow(CharacterData characterData)
    {
        OpenWindow<StorageWindow>(characterData, () => _windowsProviderService.ShowInstance<StorageWindow>(true), _storageWindows, "StorageWindow");
    }

    /// <summary>
    /// Opens a title window with the specified character data.
    /// </summary>
    /// <param name="characterData">The character data to display.</param>
    public void OpenTitleWindow(CharacterData characterData)
    {
        OpenWindow<TitleWindow>(characterData, () => _windowsProviderService.ShowInstance<TitleWindow>(true), _titleWindows, "TitleWindow");
    }

    /// <summary>
    /// Opens a sanction window with the specified character data.
    /// </summary>
    /// <param name="characterData">The character data to display.</param>
    public void OpenSanctionWindow(CharacterData characterData)
    {
        OpenWindow<SanctionWindow>(characterData, () => _windowsProviderService.ShowInstance<SanctionWindow>(true), _sanctionWindows, "SanctionWindow");
    }

    /// <summary>
    /// Opens a fortune window with the specified character data.
    /// </summary>
    /// <param name="characterData">The character data to display.</param>
    public void OpenFortuneWindow(CharacterData characterData)
    {
        OpenWindow<FortuneWindow>(characterData, () => _windowsProviderService.ShowInstance<FortuneWindow>(true), _fortuneWindows, "FortuneWindow");
    }

    #endregion

    #region Rare Card Reward Window

    private readonly Dictionary<Guid, Window> _rareCardRewardWindows = [];

    /// <summary>
    /// Opens a rare card reward window with the specified parameters.
    /// </summary>
    /// <param name="token">The unique token for the window.</param>
    /// <param name="id">The ID of the rare card reward.</param>
    /// <param name="messageType">The type of the message, if any.</param>
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

    #region Drop Group List Window

    private readonly Dictionary<Guid, Window> _dropGroupListWindows = [];

    /// <summary>
    /// Opens a drop group list window with the specified parameters.
    /// </summary>
    /// <param name="token">The unique token for the window.</param>
    /// <param name="id">The ID of the drop group.</param>
    /// <param name="dropGroupType">The type of the drop group.</param>
    public void OpenDropGroupListWindow(Guid token, int id, ItemDropGroupType dropGroupType)
    {
        if (_dropGroupListWindows.TryGetValue(token, out Window? existingWindow))
        {
            if (existingWindow.WindowState == WindowState.Minimized)
            {
                existingWindow.WindowState = WindowState.Normal;
            }

            existingWindow.Focus();
        }
        else
        {
            var dropGroupListWindow = _windowsProviderService.ShowInstance<DropGroupListWindow>(true);
            if (dropGroupListWindow != null)
            {
                _openWindowsCount++;
                dropGroupListWindow.Closed += (sender, args) =>
                {
                    _dropGroupListWindows.Remove(token);
                    _openWindowsCount--;
                };
                _dropGroupListWindows[token] = dropGroupListWindow;
            }
        }

        WeakReferenceMessenger.Default.Send(new DropGroupMessage(id, token, dropGroupType));
    }
    #endregion

    #endregion
}