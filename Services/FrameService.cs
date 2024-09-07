﻿using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Utilities;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Services
{
    public class FrameService(IGMDatabaseService gmDatabaseService) : IFrameService
    {
        private readonly IGMDatabaseService _gmDatabaseService = gmDatabaseService;

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
                5 => $"{Resources.Rank1}",
                4 => $"{Resources.Rank2}",
                3 => $"{Resources.Rank3}",
                2 => $"{Resources.Rank4}",
                1 => $"{Resources.Rank5}",
                _ => $"{Resources.Rank5}",
            };
        }

        public static string GetSocketText(int colorId)
        {
            return (SocketColor)colorId switch
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

        public static string GetSocketColor(int colorId)
        {
            return (SocketColor)colorId switch
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

        public string GetString(int stringId)
        {
            string stringName = _gmDatabaseService.GetString(stringId);

            return stringName;
        }

        public string GetOptionName(int option, int optionValue, bool isFixedOption = false)
        {
            string optionName = _gmDatabaseService.GetOptionName(option);
            (int secTime, float value) = _gmDatabaseService.GetOptionValues(option);

            string formattedOption = FormatNameID(option, optionName, $"{optionValue}", $"{secTime}", $"{value}");

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

        public string FormatMainStat(int itemType, int physicalStat, int magicStat, int jobClass, int weaponId, int enhanceLevel)
        {
            string mainStat = "";
            var enhanceValue = _gmDatabaseService.GetEnhanceValue(enhanceLevel);

            if ((ItemType)itemType == ItemType.Armor && physicalStat > 0 && magicStat > 0)
            {
                if (enhanceLevel > 0)
                {
                    mainStat = $"{Resources.PhysicalDefense} +{physicalStat}\nAdd {Resources.PhysicalDefense} +{Math.Round(physicalStat * enhanceValue)}\n{Resources.MagicDefense} +{magicStat}\nAdd {Resources.MagicDefense} +{Math.Round(magicStat * enhanceValue)}";
                }
                else
                {
                    mainStat = $"{Resources.PhysicalDefense} +{physicalStat}\n{Resources.MagicDefense} +{magicStat}";
                }

            }
            else if ((ItemType)itemType == ItemType.Weapon)
            {
                (int physicalAttackMin, int physicalAttackMax, int magicAttackMin, int magicAttackMax) = _gmDatabaseService.GetWeaponStats(jobClass, weaponId);

                if (enhanceLevel > 0)
                {
                    mainStat = $"{Resources.PhysicalDamage} +{physicalAttackMin}~{physicalAttackMax}\nAdd {Resources.PhysicalDamage} +{Math.Round(physicalAttackMax * enhanceValue)}\n{Resources.MagicDamage} +{magicAttackMin}~{magicAttackMax}\nAdd {Resources.MagicDamage} +{Math.Round(magicAttackMax * enhanceValue)}";
                }
                else
                {
                    mainStat = $"{Resources.PhysicalDamage} +{physicalAttackMin}~{physicalAttackMax}\n{Resources.MagicDamage} +{magicAttackMin}~{magicAttackMax}";
                }
            }

            return mainStat;
        }

        public string FormatSetEffect(int setId)
        {
            (int nSetOption00, int nSetOptionvlue00, int nSetOption01, int nSetOptionvlue01, int nSetOption02, int nSetOptionvlue02, int nSetOption03, int nSetOptionvlue03, int nSetOption04, int nSetOptionvlue04) = _gmDatabaseService.GetSetInfo(setId);

            if (nSetOption00 == 0 && nSetOption01 == 0 && nSetOption02 == 0 && nSetOption03 == 0 && nSetOption04 == 0)
                return string.Empty;

            string setEffect01 = GetOptionName(nSetOption00, nSetOptionvlue00);
            string setEffect02 = GetOptionName(nSetOption01, nSetOptionvlue01);
            string setEffect03 = GetOptionName(nSetOption02, nSetOptionvlue02);
            string setEffect04 = GetOptionName(nSetOption03, nSetOptionvlue03);
            string setEffect05 = GetOptionName(nSetOption04, nSetOptionvlue04);

            string setEffect = $"{Resources.SetEffect}\n";
            if (nSetOption00 != 0)
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

        public static string FormatSellValue(int sellPrice)
        {
            return sellPrice > 0 ? $"Sell Value: {sellPrice:N0} {Resources.Gold}" : string.Empty;
        }

        public static string FormatRequiredLevel(int levelLimit)
        {
            return levelLimit != 0 ? $"{Resources.RequiredLevel}: {levelLimit}" : string.Empty;
        }

        public static string FormatItemTrade(int itemTrade)
        {
            return itemTrade == 0 ? $"{Resources.TradeUnavailable}" : string.Empty;
        }

        public static string FormatDurability(int durability)
        {
            return durability > 0 ? $"{Resources.Durability}: {durability / 100}/{durability / 100}" : string.Empty;
        }

        public static string FormatWeight(int weight)
        {
            return weight > 0 ? $"{weight / 1000.0:0.000}{Resources.Kg}" : string.Empty;
        }

        public static string FormatReconstruction(int reconstruction, int reconstructionMax, int itemTrade)
        {
            return reconstructionMax > 0 && itemTrade != 0 ? $"{Resources.AttributeItem} ({reconstruction} {Resources.Times}/{reconstructionMax} {Resources.Times})" : $"{Resources.BoundItem}";
        }

        public static string FormatPetFood(int petFood)
        {
            return petFood == 0 ? $"{Resources.PetFoodDescNo}" : $"{Resources.PetFoodDescYes}";
        }

        public static string FormatPetFoodColor(int petFood)
        {
            return petFood == 0 ? "#e75151" : "#eed040";
        }

        public static string FormatAugmentStone(int value)
        {
            return value > 0 ? $"{Resources.PhysicalMagicDamage} +{value}" : string.Empty;
        }

        public static string FormatAugmentStoneLevel(int value)
        {
            return value > 0 ? $"Augment Level +{value}" : string.Empty;
        }

        private const string ColorTagStart = "<COLOR:";
        private const string ColorTagEnd = ">";
        private const string ColorTagClose = "</COLOR>";
        private const string LineBreakTag = "<br>";

        public static string FormatNameID(int optionID, string optionName, string replacement01, string replacement02, string replacement03)
        {
            optionName = RemoveColorTags(optionName);

            optionName = optionName.Replace(ColorTagClose, "")
                           .Replace(ColorTagStart, "")
                           .Replace(LineBreakTag, " ");

            string valuePlaceholder01 = optionName.Contains("#@value01@#%") ? "#@value01@#%" : "#@value01@#";
            string valuePlaceholder02 = optionName.Contains("#@value02@#%") ? "#@value02@#%" : "#@value02@#";
            string valuePlaceholder03 = optionName.Contains("#@value03@#%") ? "#@value03@#%" : "#@value03@#";

            bool hasValuePlaceholder01 = optionName.Contains(valuePlaceholder01);
            bool hasValuePlaceholder02 = optionName.Contains(valuePlaceholder02);
            bool hasValuePlaceholder03 = optionName.Contains(valuePlaceholder03);

            if (hasValuePlaceholder01 && !hasValuePlaceholder02 && !hasValuePlaceholder03)
            {
                optionName = FormatPercentage(optionName, valuePlaceholder01, replacement01);
            }
            else if (!hasValuePlaceholder01 && hasValuePlaceholder02 && !hasValuePlaceholder03)
            {
                optionName = FormatPercentage(optionName, valuePlaceholder02, replacement01);
            }
            else if (hasValuePlaceholder01 && hasValuePlaceholder02 && !hasValuePlaceholder03)
            {
                if (optionName.Contains(Resources.OptionChanceToCast))
                {
                    optionName = FormatPercentage(optionName, valuePlaceholder01, replacement01);
                    optionName = FormatPercentage(optionName, valuePlaceholder02, replacement03);
                }
                else if (optionName.Contains(Resources.OptionDamageConverted))
                {
                    optionName = optionName.Replace(valuePlaceholder01, Resources.PhysicalMagic);
                    optionName = FormatPercentage(optionName, valuePlaceholder02, replacement01, true);
                }
                else if (optionName.Contains(Resources.OptionRecoverPlus))
                {
                    optionName = FormatPercentage(optionName, valuePlaceholder01, replacement01);
                    if (int.TryParse(replacement02, out int seconds))
                    {
                        int minutes = seconds / 60;
                        replacement02 = minutes.ToString();
                    }

                    optionName = optionName.Replace(valuePlaceholder02, replacement02);
                }
                else if (optionName.Contains(Resources.OptionChanceOf) || optionName.Contains(Resources.OptionChanceTo))
                {
                    optionName = FormatPercentage(optionName, valuePlaceholder01, replacement01);
                    optionName = FormatPercentage(optionName, valuePlaceholder02, replacement03);
                }
                else if (optionName.Contains("Skill Damage +") || optionName.Contains("Skill Cooldown -"))
                {
                    string skillName = GetSkillName(optionID);

                    optionName = optionName.Replace(valuePlaceholder01, skillName);
                    optionName = FormatPercentage(optionName, valuePlaceholder02, replacement01);
                }
                else
                {
                    optionName = FormatPercentage(optionName, valuePlaceholder01, replacement02);
                    optionName = FormatPercentage(optionName, valuePlaceholder02, replacement01);
                }
            }
            else if (hasValuePlaceholder01 && hasValuePlaceholder02 && hasValuePlaceholder03)
            {
                if (optionName.Contains(Resources.OptionChanceOf) || optionName.Contains(Resources.OptionChanceTo))
                {
                    optionName = FormatPercentage(optionName, valuePlaceholder01, replacement01);
                    optionName = FormatPercentage(optionName, valuePlaceholder02, replacement02);
                    optionName = FormatPercentage(optionName, valuePlaceholder03, replacement03);
                }
                else
                {
                    optionName = FormatPercentage(optionName, valuePlaceholder01, replacement01);
                    optionName = FormatPercentage(optionName, valuePlaceholder02, replacement02);
                    optionName = FormatPercentage(optionName, valuePlaceholder03, replacement03);
                }
            }
            else if (!hasValuePlaceholder01 && hasValuePlaceholder02 && hasValuePlaceholder03)
            {
                if (optionName.Contains(Resources.OptionWhenHit))
                {
                    optionName = FormatPercentage(optionName, "#@value02@#%", replacement03);
                    optionName = FormatPercentage(optionName, "#@value03@#%", replacement01);
                    optionName = optionName.Replace("#@value02@#", replacement02);
                }
                else
                {
                    optionName = FormatPercentage(optionName, valuePlaceholder03, replacement03);
                    optionName = FormatPercentage(optionName, valuePlaceholder02, replacement02);
                }

            }

            return optionName;
        }

        private static string GetSkillName(int optionID)
        {
            return optionID switch
            {
                5002 or 5014 or 5017 or 5029 or 5038 or 5043 => "NotFound",
                5003 or 5018 => "Weapon",
                5004 or 5019 => "Curse",
                5005 or 5020 => "Black Sorcery",
                5006 or 5021 => "Summon",
                5007 or 5022 => "Wind",
                5008 or 5023 => "Water",
                5009 or 5024 => "Fire",
                5010 or 5025 => "Light",
                5011 or 5026 => "Earth",
                5012 or 5027 => "Ki Gong",
                5013 or 5028 => "Savage",
                5015 or 5030 or 5041 or 5046 => "Special",
                5016 or 5031 => "Fighting",
                5032 or 5033 or 5034 or 5035 or 5036 or 5037 or 5048 or 5049 => "Unknown Skill Name",
                5039 or 5044 => "Tactical",
                5040 or 5045 => "Heavy Weapon",
                5042 or 5047 => "Firearm",
                _ => "Unknown Skill Name",
            };
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

        private static string FormatPercentage(string input, string placeholder, string replacement, bool isFixedOption = false)
        {
            if (placeholder.Contains('%') && int.TryParse(replacement, out int numericValue))
            {
                double formattedValue;

                if (isFixedOption)
                {
                    formattedValue = numericValue;
                }
                else
                {
                    formattedValue = (double)numericValue / 100;
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

        public string FormatTitleEffect(int titleId)
        {
            try
            {
                (int titleCategory, int remainTime, int nAddEffectID00, int nAddEffectID01, int nAddEffectID02, int nAddEffectID03, int nAddEffectID04, int nAddEffectID05, string titleDesc) = _gmDatabaseService.GetTitleInfo(titleId);

                string formattedRemainTime = DateTimeFormatter.FormatRemainTime(remainTime);

                StringBuilder description = new();

                if (nAddEffectID00 == 0)
                {
                    return string.Empty;
                }
                else
                {
                    description.Append("[Title Effect]");

                    if (!string.IsNullOrEmpty(_gmDatabaseService.GetAddEffectName(nAddEffectID00)))
                        description.AppendLine().Append(_gmDatabaseService.GetAddEffectName(nAddEffectID00));
                    if (!string.IsNullOrEmpty(_gmDatabaseService.GetAddEffectName(nAddEffectID01)))
                        description.AppendLine().Append(_gmDatabaseService.GetAddEffectName(nAddEffectID01));
                    if (!string.IsNullOrEmpty(_gmDatabaseService.GetAddEffectName(nAddEffectID02)))
                        description.AppendLine().Append(_gmDatabaseService.GetAddEffectName(nAddEffectID02));
                    if (!string.IsNullOrEmpty(_gmDatabaseService.GetAddEffectName(nAddEffectID03)))
                        description.AppendLine().Append(_gmDatabaseService.GetAddEffectName(nAddEffectID03));
                    if (!string.IsNullOrEmpty(_gmDatabaseService.GetAddEffectName(nAddEffectID04)))
                        description.AppendLine().Append(_gmDatabaseService.GetAddEffectName(nAddEffectID04));
                    if (!string.IsNullOrEmpty(_gmDatabaseService.GetAddEffectName(nAddEffectID05)))
                        description.AppendLine().Append(_gmDatabaseService.GetAddEffectName(nAddEffectID05));

                    description.AppendLine($"\n\nValid For: {formattedRemainTime}");
                }

                return description.ToString();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage("Error: " + ex.Message);
                return string.Empty;
            }
        }
    }
}
