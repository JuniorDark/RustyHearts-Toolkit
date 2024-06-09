using RHToolkit.Messages;
using RHToolkit.Models.MessageBox;
using RHToolkit.Services;
using RHToolkit.Views.Windows;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models.Database
{
    public class CharacterManager(WindowsProviderService windowsProviderService, IDatabaseService databaseService)
    {
        private readonly WindowsProviderService _windowsProviderService = windowsProviderService;
        private readonly IDatabaseService _databaseService = databaseService;

        public static List<NameID>? GetJobItems(CharClass charClass)
        {
            List<NameID> jobItems = charClass switch
            {
                CharClass.Frantz or CharClass.Roselle or CharClass.Leila => GetEnumItems<FrantzJob>(),
                CharClass.Angela or CharClass.Edgar => GetEnumItems<AngelaJob>(),
                CharClass.Tude or CharClass.Meilin => GetEnumItems<TudeJob>(),
                CharClass.Natasha or CharClass.Ian => GetEnumItems<NatashaJob>(),
                _ => throw new ArgumentException($"Invalid character class: {charClass}"),
            };

            if (jobItems.Count > 0)
            {
                return jobItems;
            }

            return null;
        }

        public static string GetClassImage(int classValue)
        {
            return classValue switch
            {
                1 => "/Assets/images/char/ui_silhouette_frantz01.png",
                2 => "/Assets/images/char/ui_silhouette_angela01.png",
                3 => "/Assets/images/char/ui_silhouette_tude01.png",
                4 => "/Assets/images/char/ui_silhouette_natasha01.png",
                101 => "/Assets/images/char/ui_silhouette_roselle01.png",
                102 => "/Assets/images/char/ui_silhouette_leila01.png",
                201 => "/Assets/images/char/ui_silhouette_edgar01.png",
                301 => "/Assets/images/char/ui_silhouette_tude_girl01.png",
                401 => "/Assets/images/char/ui_silhouette_ian01.png",
                _ => "/Assets/images/char/ui_silhouette_frantz01.png",
            };
        }

        public static string GenerateCharacterDataMessage(CharacterData oldData, NewCharacterData newData, string messageType)
        {
            string message = "";

            void AppendChange(string property, object? oldValue, object? newValue)
            {
                if (!Equals(oldValue, newValue))
                {
                    if (messageType == "audit")
                    {
                        message += $"[<font color=blue>{property} Change</font>]<br><font color=red>Old -> {oldValue}, New ->  {newValue}<br></font>";
                    }
                    else if (messageType == "changes")
                    {
                        message += $"{property}: Old -> {oldValue}, New -> {newValue}\n";
                    }
                }
            }

            AppendChange("Level", oldData.Level, newData.Level);
            AppendChange("Experience", oldData.Experience, newData.Experience);
            AppendChange("Skill Points", oldData.SP, newData.SP);
            AppendChange("Total Skill Points", oldData.TotalSP, newData.TotalSP);
            AppendChange("Lobby", oldData.LobbyID, newData.LobbyID);
            AppendChange("Gold", oldData.Gold, newData.Gold);
            AppendChange("Hearts", oldData.Hearts, newData.Hearts);
            AppendChange("Storage Gold", oldData.StorageGold, newData.StorageGold);
            AppendChange("Storage Count", oldData.StorageCount, newData.StorageCount);
            AppendChange("Guild Exp", oldData.GuildPoint, newData.GuildPoint);
            AppendChange("Permission", oldData.Permission, newData.Permission);
            AppendChange("Block", oldData.BlockYN, newData.BlockYN);
            AppendChange("IsTradeEnable", oldData.IsTradeEnable, newData.IsTradeEnable);
            AppendChange("IsMoveEnable", oldData.IsMoveEnable, newData.IsMoveEnable);

            return message;
        }

        public static bool HasCharacterDataChanges(CharacterData oldData, NewCharacterData newData)
        {
            return oldData.Level != newData.Level ||
                   oldData.Experience != newData.Experience ||
                   oldData.SP != newData.SP ||
                   oldData.TotalSP != newData.TotalSP ||
                   oldData.LobbyID != newData.LobbyID ||
                   oldData.Gold != newData.Gold ||
                   oldData.Hearts != newData.Hearts ||
                   oldData.StorageGold != newData.StorageGold ||
                   oldData.StorageCount != newData.StorageCount ||
                   oldData.GuildPoint != newData.GuildPoint ||
                   oldData.Permission != newData.Permission ||
                   oldData.BlockYN != newData.BlockYN ||
                   oldData.IsTradeEnable != newData.IsTradeEnable ||
                   oldData.IsMoveEnable != newData.IsMoveEnable;
        }

        #region Windows

        private readonly Dictionary<Guid, Window> _itemWindows = [];

        public void OpenItemWindow(CharacterInfo characterInfo, ItemData itemData)
        {
            if (_itemWindows.TryGetValue(characterInfo.CharacterID, out Window? existingWindow))
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
                    itemWindow.Closed += (sender, args) =>
                    {
                        _itemWindows.Remove(characterInfo.CharacterID);
                        OpenWindowsCount--;
                    };
                    _itemWindows[characterInfo.CharacterID] = itemWindow;
                }
            }

            WeakReferenceMessenger.Default.Send(new CharacterInfoMessage(characterInfo, "ItemWindow", characterInfo.CharacterID));
            WeakReferenceMessenger.Default.Send(new ItemDataMessage(itemData, "ItemWindowViewModel", "EquipItem", characterInfo.CharacterID));
        }

        public int OpenWindowsCount = 0;
        private readonly Dictionary<Guid, Window> _characterWindows = [];
        private readonly Dictionary<Guid, Window> _titleWindows = [];
        private readonly Dictionary<Guid, Window> _sanctionWindows = [];
        private readonly Dictionary<Guid, Window> _fortuneWindows = [];

        private void OpenWindow<TWindow>(CharacterInfo characterInfo, Func<Window?> windowCreator, Dictionary<Guid, Window> windowsDictionary, string errorMessage)
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
                        OpenWindowsCount++;
                        window.Closed += (sender, args) =>
                        {
                            windowsDictionary.Remove(characterInfo.CharacterID);
                            OpenWindowsCount--;
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
}
