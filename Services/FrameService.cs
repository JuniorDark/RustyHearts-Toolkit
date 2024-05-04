using RHToolkit.Properties;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Services
{
    public class FrameService(IGMDatabaseService gmDatabaseService) : IFrameService
    {
        private readonly IGMDatabaseService _gmDatabaseService = gmDatabaseService;

        public string GetBranchColor(int branch)
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

        public string GetRankText(int rank)
        {
            return rank switch
            {
                5 => $"{Resources.Rank1}",
                4 => $"{Resources.Rank2}",
                3 => $"{Resources.Rank3}",
                2 => $"{Resources.Rank4}",
                1 => $"{Resources.Rank5}",
                _ => $"{rank} {Resources.Rank}",
            };
        }

        public string GetSocketText(int colorId)
        {
            return ((SocketColor)colorId) switch
            {
                SocketColor.None => Resources.SocketColorNoneDesc,
                SocketColor.Red => Resources.SocketColorRedDesc,
                SocketColor.Blue => Resources.SocketColorBlueDesc,
                SocketColor.Yellow => Resources.SocketColorYellowDesc,
                SocketColor.Green => Resources.SocketColorGreenDesc,
                SocketColor.Colorless => Resources.SocketColorColorlessDesc,
                SocketColor.Gray => Resources.SocketColorGrayDesc,
                _ => Resources.SocketColorNoneDesc,
            };
        }

        public string GetSocketColor(int colorId)
        {
            return ((SocketColor)colorId) switch
            {
                SocketColor.None => "White",
                SocketColor.Red => "#cc3300",
                SocketColor.Blue => "#0000ff",
                SocketColor.Yellow => "#cccc00",
                SocketColor.Green => "#339900",
                SocketColor.Colorless => "Gray",
                SocketColor.Gray => "Gray",
                _ => "White",
            };
        }


        public string GetOptionName(int option, int optionValue, bool isFixedOption = false)
        {
            string optionName = _gmDatabaseService.GetOptionName(option);
            (int secTime, float value, int maxValue) = _gmDatabaseService.GetOptionValues(option);

            string formattedOption = FormatNameID(optionName, $"{optionValue}", $"{secTime}", $"{value}", maxValue, isFixedOption);

            return option != 0 ? formattedOption : Resources.NoBuff;
        }

        public string GetColorFromOption(int option)
        {
            string optionName = _gmDatabaseService.GetOptionName(option);

            int startIndex = optionName.IndexOf(ColorTagStart);

            if (startIndex != -1)
            {
                int endIndex = optionName.IndexOf('>', startIndex);

                if (endIndex != -1)
                {
                    string colorHex = optionName.Substring(startIndex + 7, endIndex - startIndex - 7);
                    return "#" + colorHex;
                }
            }

            return "#ffffff";
        }

        public string FormatMainStat(int itemType, int physicalStat, int magicStat, int jobClass, int weaponId)
        {
            string mainStat = "";

            if ((ItemType)itemType == ItemType.Armor && physicalStat > 0 && magicStat > 0)
            {
                mainStat = $"{Resources.PhysicalDefense} +{physicalStat}\n{Resources.MagicDefense} +{magicStat}";
            }

            else if ((ItemType)itemType == ItemType.Weapon)
            {
                (int physicalAttackMin, int physicalAttackMax, int magicAttackMin, int magicAttackMax) = _gmDatabaseService.GetWeaponStats(jobClass, weaponId);
                mainStat = $"{Resources.PhysicalDamage} +{physicalAttackMin}~{physicalAttackMax}\n{Resources.MagicDamage} +{magicAttackMin}~{magicAttackMax}";
            }

            return mainStat;
        }

        public string FormatSetEffect(int setId)
        {
            (int nSetOption00, int nSetOptionvlue00, int nSetOption01, int nSetOptionvlue01, int nSetOption02, int nSetOptionvlue02, int nSetOption03, int nSetOptionvlue03, int nSetOption04, int nSetOptionvlue04) = _gmDatabaseService.GetSetInfo(setId);

            if (nSetOption00 == 0)
                return "";

            string setEffect01 = GetOptionName(nSetOption00, nSetOptionvlue00);
            string setEffect02 = GetOptionName(nSetOption01, nSetOptionvlue01);
            string setEffect03 = GetOptionName(nSetOption02, nSetOptionvlue02);
            string setEffect04 = GetOptionName(nSetOption03, nSetOptionvlue03);
            string setEffect05 = GetOptionName(nSetOption04, nSetOptionvlue04);

            string setEffect = $"{Resources.SetEffect}\n";
            setEffect += $"{Resources.Set2}: {setEffect01}\n";
            if (nSetOption01 != 0)
                setEffect += $"{Resources.Set3}: {setEffect02}\n";
            if (nSetOption02 != 0)
                setEffect += $"{Resources.Set4}: {setEffect03}\n";
            if (nSetOption03 != 0)
                setEffect += $"{Resources.Set5}: {setEffect04}\n";
            if (nSetOption04 != 0)
                setEffect += $"{Resources.Set6}: {setEffect05}\n";

            return setEffect;
        }

        public string FormatSellValue(int sellPrice)
        {
            return sellPrice > 0 ? $"{sellPrice:N0} {Resources.Gold}" : "";
        }

        public string FormatRequiredLevel(int levelLimit)
        {
            return $"{Resources.RequiredLevel}: {levelLimit}";
        }

        public string FormatItemTrade(int itemTrade)
        {
            return itemTrade == 0 ? $"{Resources.TradeUnavailable}" : "";
        }

        public string FormatDurability(int durability)
        {
            return durability > 0 ? $"{Resources.Durability}: {durability / 100}/{durability / 100}" : "";
        }

        public string FormatWeight(int weight)
        {
            return weight > 0 ? $"{weight / 1000.0:0.000}{Resources.Kg}" : "";
        }

        public string FormatReconstruction(int reconstruction, int reconstructionMax, int itemTrade)
        {
            return reconstructionMax > 0 && itemTrade != 0 ? $"{Resources.AttributeItem} ({reconstruction} {Resources.Times}/{reconstructionMax} {Resources.Times})" : $"{Resources.BoundItem}";
        }

        public string FormatPetFood(int petFood)
        {
            return petFood == 0 ? $"{Resources.PetFoodDescNo}" : $"{Resources.PetFoodDescYes}";
        }

        public string FormatPetFoodColor(int petFood)
        {
            return petFood == 0 ? "#e75151" : "#eed040";
        }

        public string FormatAugmentStone(int value)
        {
            return value > 0 ? $"{Resources.PhysicalMagicDamage} +{value}" : "";
        }

        private const string ColorTagStart = "<COLOR:";
        private const string ColorTagEnd = ">";
        private const string ColorTagClose = "</COLOR>";
        private const string LineBreakTag = "<br>";

        public string FormatNameID(string option, string replacement01, string replacement02, string replacement03, int maxValue, bool isFixedOption = false)
        {
            option = RemoveColorTags(option);

            option = option.Replace(ColorTagClose, "")
                           .Replace(ColorTagStart, "")
                           .Replace(LineBreakTag, " ");

            string valuePlaceholder01 = option.Contains("#@value01@#%") ? "#@value01@#%" : "#@value01@#";
            string valuePlaceholder02 = option.Contains("#@value02@#%") ? "#@value02@#%" : "#@value02@#";
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
                if (option.Contains(Resources.OptionChanceToCast))
                {
                    option = FormatPercentage(option, valuePlaceholder01, replacement01, maxValue);
                    option = FormatPercentage(option, valuePlaceholder02, replacement03, maxValue);
                }
                else if (option.Contains(Resources.OptionDamageConverted))
                {
                    option = option.Replace(valuePlaceholder01, Resources.PhysicalMagic);
                    option = FormatPercentage(option, valuePlaceholder02, replacement01, maxValue, isFixedOption);
                }
                else if (option.Contains(Resources.OptionRecoverPlus))
                {
                    option = FormatPercentage(option, valuePlaceholder01, replacement01, maxValue);
                    if (int.TryParse(replacement02, out int seconds))
                    {
                        int minutes = seconds / 60;
                        replacement02 = minutes.ToString();
                    }

                    option = option.Replace(valuePlaceholder02, replacement02);
                }
                else if (option.Contains(Resources.OptionChanceOf) || option.Contains(Resources.OptionChanceTo))
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
                if (option.Contains(Resources.OptionChanceOf) || option.Contains(Resources.OptionChanceTo))
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
                if (option.Contains(Resources.OptionWhenHit))
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

        private static string FormatPercentage(string input, string placeholder, string replacement, int maxValue, bool isFixedOption = false)
        {
            if (placeholder.Contains('%') && int.TryParse(replacement, out int numericValue))
            {
                double formattedValue;

                if (maxValue == 10000)
                {
                    if (input.Contains(Resources.OptionDamageConverted) && isFixedOption)
                    {
                        formattedValue = numericValue;
                    }
                    else
                    {
                        formattedValue = (double)numericValue / 100;
                    }
                    
                }
                
                else
                {
                    if (input.Contains(Resources.OptionAllElementalDamage))
                    {
                        formattedValue = (double)numericValue / maxValue;
                    }
                    else
                    {
                        formattedValue = numericValue;
                    }

                }

                string formattedPercentage = formattedValue.ToString("0.00");
                input = input.Replace(placeholder, $"{formattedPercentage}%");
            }
            else
            {
                input = input.Replace(placeholder, replacement);
            }

            return input;
        }

    }
}
