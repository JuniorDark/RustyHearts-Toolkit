using RHToolkit.Messages;
using RHToolkit.Models;
using RHToolkit.Models.Database;
using RHToolkit.Models.MessageBox;
using RHToolkit.Views.Windows;

namespace RHToolkit.Services;

public class WindowsService(WindowsProviderService windowsProviderService) : IWindowsService
{
    private readonly WindowsProviderService _windowsProviderService = windowsProviderService;

    #region Windows

    private readonly Dictionary<Guid, Window> _itemWindows = [];

    public void OpenItemWindow(Guid token, string messageType, ItemData itemData, CharacterInfo? characterInfo = null)
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

        if (characterInfo != null)
        {
            WeakReferenceMessenger.Default.Send(new CharacterInfoMessage(characterInfo, "ItemWindow", token));
        }

        WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "ItemWindow", messageType, token));
    }

    private static int _openWindowsCount = 0;
    private readonly Dictionary<Guid, Window> _characterWindows = [];
    private readonly Dictionary<Guid, Window> _equipmentWindows = [];
    private readonly Dictionary<Guid, Window> _inventoryWindows = [];
    private readonly Dictionary<Guid, Window> _titleWindows = [];
    private readonly Dictionary<Guid, Window> _sanctionWindows = [];
    private readonly Dictionary<Guid, Window> _fortuneWindows = [];

    public static int OpenWindowsCount { get => _openWindowsCount; set => _openWindowsCount = value; }

    private static void OpenWindow<TWindow>(CharacterInfo characterInfo, Func<Window?> windowCreator, Dictionary<Guid, Window> windowsDictionary, string errorMessage)
    {
        if (!SqlCredentialValidator.ValidateCredentials())
        {
            return;
        }

        try
        {
            if (windowsDictionary.TryGetValue(characterInfo.CharacterID, out Window? existingWindow))
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
                    windowsDictionary.Add(characterInfo.CharacterID, window);
                    _openWindowsCount++;
                    window.Closed += (sender, args) =>
                    {
                        windowsDictionary.Remove(characterInfo.CharacterID);
                        _openWindowsCount--;
                    };
                }
            }

            WeakReferenceMessenger.Default.Send(new CharacterInfoMessage(characterInfo, typeof(TWindow).Name, characterInfo.CharacterID));
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Error: {errorMessage}: {ex.Message}", "Error");
        }
    }

    public void OpenCharacterWindow(CharacterInfo characterInfo)
    {
        OpenWindow<CharacterWindow>(characterInfo, () => _windowsProviderService.ShowInstance<CharacterWindow>(true), _characterWindows, "CharacterWindow");
    }

    public void OpenEquipmentWindow(CharacterInfo characterInfo)
    {
        OpenWindow<EquipmentWindow>(characterInfo, () => _windowsProviderService.ShowInstance<EquipmentWindow>(true), _equipmentWindows, "EquipmentWindow");
    }

    public void OpenInventoryWindow(CharacterInfo characterInfo)
    {
        OpenWindow<InventoryWindow>(characterInfo, () => _windowsProviderService.ShowInstance<InventoryWindow>(true), _inventoryWindows, "InventoryWindow");
    }

    public void OpenTitleWindow(CharacterInfo characterInfo)
    {
        OpenWindow<TitleWindow>(characterInfo, () => _windowsProviderService.ShowInstance<TitleWindow>(true), _titleWindows, "TitleWindow");
    }

    public void OpenSanctionWindow(CharacterInfo characterInfo)
    {
        OpenWindow<SanctionWindow>(characterInfo, () => _windowsProviderService.ShowInstance<SanctionWindow>(true), _sanctionWindows, "SanctionWindow");
    }

    public void OpenFortuneWindow(CharacterInfo characterInfo)
    {
        OpenWindow<FortuneWindow>(characterInfo, () => _windowsProviderService.ShowInstance<FortuneWindow>(true), _fortuneWindows, "FortuneWindow");
    }

    #endregion
}
