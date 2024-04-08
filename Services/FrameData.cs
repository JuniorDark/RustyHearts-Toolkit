using static RHGMTool.Models.EnumService;

namespace RHGMTool.Services
{
    public class FrameData
    {
        private readonly SqLiteDatabaseService _databaseService;
        private readonly GMDbService _gMDbService;

        public FrameData()
        {
            _databaseService = new SqLiteDatabaseService();
            _gMDbService = new GMDbService(_databaseService);
        }

        public static string GetBranchColor(int branch)
        {
            return branch switch
            {
                2 => "#2adf00",
                4 => "#009cff",
                5 => "#eed040",
                6 => "#d200f8",
                _ => "White",
            };
        }

        public static string GetRankText(int rank)
        {
            return rank switch
            {
                5 => "1st Rank",
                4 => "2nd Rank",
                3 => "3rd Rank",
                2 => "4th Rank",
                1 => "5th Rank",
                _ => $"{rank}th Rank",
            };
        }

        public static (string text, string color) SetSocketColor(int colorId)
        {
            return (SocketColor)colorId switch
            {
                SocketColor.None => ("Unprocessed Gem Socket", "White"),
                SocketColor.Red => ("Processed Red Gem Socket", "#cc3300"),
                SocketColor.Blue => ("Processed Blue Gem Socket", "#0000ff"),
                SocketColor.Yellow => ("Processed Yellow Gem Socket", "#cccc00"),
                SocketColor.Green => ("Processed Green Gem Socket", "#339900"),
                SocketColor.Colorless => ("Processed Colorless Socket", "Gray"),
                SocketColor.Gray => ("Processed Gray Socket", "Gray"),
                _ => ("Unprocessed Gem Socket", "White"),
            };
        }

        public (string option, string color) GetOptionName(int option, int optionValue)
        {
            string fixedOption = _gMDbService.GetOptionName(option);
            (int secTime, float value, int maxValue) = _gMDbService.GetOptionValues(option);

            string colorHex = GetColorFromOption(fixedOption);
            string formattedOption = FormatNameID(fixedOption, $"{optionValue}", $"{secTime}", $"{value}", maxValue);

            return (formattedOption, colorHex);
        }

        public string FormatMainStat(int itemType, int physicalStat, int magicStat, int jobClass, int weaponId)
        {
            string mainStat = "";

            if ((ItemType)itemType == ItemType.Armor && physicalStat > 0 && magicStat > 0)
            {
                mainStat = $"Physical Defense +{physicalStat}\nMagic Defense +{magicStat}";
            }
            else if ((ItemType)itemType == ItemType.Weapon)
            {
                (int physicalAttackMin, int physicalAttackMax, int magicAttackMin, int magicAttackMax) = _gMDbService.GetWeaponStats(jobClass, weaponId);
                mainStat = $"Physical Damage +{physicalAttackMin}~{physicalAttackMax}\nMagic Damage +{magicAttackMin}~{magicAttackMax}";
            }

            return mainStat;
        }

        public static string FormatSellValue(int sellPrice)
        {
            return sellPrice > 0 ? $"{sellPrice:N0} Gold" : "";
        }

        public static string FormatRequiredLevel(int levelLimit)
        {
            return $"Required Level: {levelLimit}";
        }

        public static string FormatItemTrade(int itemTrade)
        {
            return itemTrade == 0 ? "Trade Unavailable" : "";
        }

        public static string FormatDurabilityValue(int itemType, int durability, int maxDurability)
        {
            if (itemType == 1 || itemType == 2)
            {
                return "";
            }

            return durability > 0 ? $"Durability: {durability / 100}/{maxDurability / 100}" : "";
        }

        public static string FormatWeight(int weight)
        {
            return weight > 0 ? $"{weight / 1000.0:0.000}Kg" : "";
        }

        public static string FormatReconstruction(int itemType, int reconstructionMax, int itemTrade)
        {
            if (itemType == 1 || itemType == 2)
            {
                return "";
            }

            return reconstructionMax > 0 && itemTrade != 0 ? $"Attribute Item ({reconstructionMax} Times/{reconstructionMax} Times)" : "Bound item (Binds when acquired)";
        }

        public static string FormatPetFood(int petFood)
        {
            return petFood == 0 ? "This item cannot be used as Pet Food" : "This item can be used as Pet Food";
        }

        public static string FormatPetFoodColor(int petFood)
        {
            return petFood == 0 ? "#e75151" : "#eed040";
        }


        private const string ColorTagStart = "<COLOR:";
        private const string ColorTagEnd = ">";
        private const string ColorTagClose = "</COLOR>";
        private const string LineBreakTag = "<br>";

        public static string FormatNameID(string option, string replacement01, string replacement02, string replacement03, int maxValue)
        {
            option = RemoveColorTags(option);

            option = option.Replace(ColorTagClose, "")
                           .Replace(ColorTagStart, "")
                           .Replace(LineBreakTag, " ");

            string valuePlaceholder01 = option.Contains("#@value01@#%") ? "#@value01@#%" : "#@value01@#";
            string valuePlaceholder02 = option.Contains("#@value02@#") ? "#@value02@#" : "#@value02@#%";
            string valuePlaceholder03 = option.Contains("#@value03@#%") ? "#@value03@#%" : "#@value03@#";

            bool hasValuePlaceholder01 = option.Contains(valuePlaceholder01);
            bool hasValuePlaceholder02 = option.Contains(valuePlaceholder02);
            bool hasValuePlaceholder03 = option.Contains(valuePlaceholder03);

            if (hasValuePlaceholder01 && !hasValuePlaceholder02 && !hasValuePlaceholder03)
            {
                option = FormatPercentage(option, valuePlaceholder01, replacement01, maxValue);
            }
            else if (!hasValuePlaceholder01 && hasValuePlaceholder02 && !hasValuePlaceholder03)
            {
                option = FormatPercentage(option, valuePlaceholder02, replacement01, maxValue);
            }
            else if (hasValuePlaceholder01 && hasValuePlaceholder02 && !hasValuePlaceholder03)
            {
                if (option.Contains("chance to cast"))
                {
                    option = FormatPercentage(option, valuePlaceholder01, replacement01, maxValue);
                    option = FormatPercentage(option, valuePlaceholder02, replacement03, maxValue);
                }
                else if (option.Contains("damage will be converted"))
                {
                    //valuePlaceholder02 = option.Contains("#@value02@#%") ? "#@value02@#%" : "#@value02@#";
                    option = option.Replace(valuePlaceholder01, "Physical + Magic");
                    option = FormatPercentage(option, valuePlaceholder02, replacement01, maxValue);
                }
                else if (option.Contains("Recover +"))
                {
                    option = FormatPercentage(option, valuePlaceholder01, replacement01, maxValue);
                    if (int.TryParse(replacement02, out int seconds))
                    {
                        int minutes = seconds / 60;
                        replacement02 = minutes.ToString();
                    }

                    option = option.Replace(valuePlaceholder02, replacement02);
                }
                else if (option.Contains("chance of") || option.Contains("chance to"))
                {
                    option = FormatPercentage(option, valuePlaceholder01, replacement01, maxValue);
                    option = FormatPercentage(option, valuePlaceholder02, replacement03, maxValue);
                }
                else
                {
                    option = FormatPercentage(option, valuePlaceholder01, replacement02, maxValue);
                    option = FormatPercentage(option, valuePlaceholder02, replacement01, maxValue);
                }
            }
            else if (hasValuePlaceholder01 && hasValuePlaceholder02 && hasValuePlaceholder03)
            {
                if (option.Contains("chance of") || option.Contains("chance to"))
                {
                    option = FormatPercentage(option, valuePlaceholder01, replacement01, maxValue);
                    option = FormatPercentage(option, valuePlaceholder02, replacement02, maxValue);
                    option = FormatPercentage(option, valuePlaceholder03, replacement03, maxValue);
                }
                else
                {
                    option = FormatPercentage(option, valuePlaceholder01, replacement01, maxValue);
                    option = FormatPercentage(option, valuePlaceholder02, replacement02, maxValue);
                    option = FormatPercentage(option, valuePlaceholder03, replacement03, maxValue);
                }
            }
            else if (!hasValuePlaceholder01 && hasValuePlaceholder02 && hasValuePlaceholder03)
            {
                if (option.Contains("When hit"))
                {
                    option = FormatPercentage(option, valuePlaceholder02, replacement02, maxValue);
                    option = FormatPercentage(option, valuePlaceholder02, replacement01, maxValue);
                    option = FormatPercentage(option, valuePlaceholder03, replacement01, maxValue);

                }
                else
                {
                    option = FormatPercentage(option, valuePlaceholder03, replacement03, maxValue);
                    option = FormatPercentage(option, valuePlaceholder02, replacement02, maxValue);
                }

            }

            return option;
        }

        private static string RemoveColorTags(string input)
        {
            int startIndex = input.IndexOf(ColorTagStart);

            while (startIndex != -1)
            {
                int endIndex = input.IndexOf(ColorTagEnd, startIndex);

                if (endIndex != -1)
                {
                    input = input.Remove(startIndex, endIndex - startIndex + 1);
                }

                startIndex = input.IndexOf(ColorTagStart);
            }

            return input;
        }

        private static string FormatPercentage(string input, string placeholder, string replacement, int maxValue)
        {
            if (placeholder.Contains('%') && int.TryParse(replacement, out int numericValue))
            {
                double formattedValue = (double)numericValue / maxValue;
                input = input.Replace(placeholder, $"{formattedValue:P}");
            }
            else
            {
                input = input.Replace(placeholder, replacement);
            }

            return input;
        }

        public static string GetColorFromOption(string option)
        {
            int startIndex = option.IndexOf(ColorTagStart);

            if (startIndex != -1)
            {
                int endIndex = option.IndexOf('>', startIndex);

                if (endIndex != -1)
                {
                    string colorHex = option.Substring(startIndex + 7, endIndex - startIndex - 7);
                    return "#" + colorHex;
                }
            }

            return "#ffffff";
        }

    }
}
