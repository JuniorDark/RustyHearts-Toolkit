using RHToolkit.Services;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models.Database
{
    /// <summary>
    /// Manages character data operations.
    /// </summary>
    public class CharacterDataManager(IDatabaseService databaseService)
    {
        private readonly IDatabaseService _databaseService = databaseService;

        /// <summary>
        /// Gets the job items for a specified character class.
        /// </summary>
        /// <param name="charClass">The character class.</param>
        /// <returns>A list of job items for the specified character class.</returns>
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

        /// <summary>
        /// Gets the image path for a specified character class value.
        /// </summary>
        /// <param name="classValue">The character class value.</param>
        /// <returns>The image path for the specified character class value.</returns>
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

        /// <summary>
        /// Generates a message describing changes in character data.
        /// </summary>
        /// <param name="oldData">The old character data.</param>
        /// <param name="newData">The new character data.</param>
        /// <param name="messageType">The type of message to generate ("audit" or "changes").</param>
        /// <returns>A message describing the changes in character data.</returns>
        public static string GenerateCharacterDataMessage(CharacterData oldData, NewCharacterData newData, string messageType)
        {
            string message = "";

            void AppendChange(string property, object? oldValue, object? newValue)
            {
                if (!Equals(oldValue, newValue))
                {
                    if (messageType == "audit")
                    {
                        message += $"[<font color=blue>{property} Change</font>]<br><font color=red>{Resources.OldValue} -> {oldValue}, {Resources.NewValue} -> {newValue}<br></font>";
                    }
                    else if (messageType == "changes")
                    {
                        message += $"{property}: {Resources.OldValue} -> {oldValue}, {Resources.NewValue} -> {newValue}\n";
                    }
                }
            }

            AppendChange("Level", oldData.Level, newData.Level);
            AppendChange("Experience", oldData.Experience, newData.Experience);
            AppendChange("Skill Points", oldData.SP, newData.SP);
            AppendChange("Total Skill Points", oldData.TotalSP, newData.TotalSP);
            AppendChange("Lobby ID", oldData.LobbyID, newData.LobbyID);
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

        /// <summary>
        /// Determines if there are changes between old and new character data.
        /// </summary>
        /// <param name="oldData">The old character data.</param>
        /// <param name="newData">The new character data.</param>
        /// <returns>True if there are changes, otherwise false.</returns>
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