using RHToolkit.Services;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models.Database
{
    public class CharacterHelper(IDatabaseService databaseService)
    {
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

    }
}
