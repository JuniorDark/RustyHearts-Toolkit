using RHToolkit.Models.MessageBox;
using RHToolkit.Utilities;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Services
{
    /// <summary>
    /// Provides various services related to formatting and retrieving data.
    /// </summary>
    public class FrameService(IGMDatabaseService gmDatabaseService) : IFrameService
    {
        private readonly IGMDatabaseService _gmDatabaseService = gmDatabaseService;

        /// <summary>
        /// Gets the color associated with a branch.
        /// </summary>
        /// <param name="branch">The branch identifier.</param>
        /// <returns>The color as a hex string.</returns>
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

        /// <summary>
        /// Gets the text associated with a rank.
        /// </summary>
        /// <param name="rank">The rank identifier.</param>
        /// <returns>The rank text.</returns>
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

        /// <summary>
        /// Gets the description text for a socket color.
        /// </summary>
        /// <param name="colorId">The color identifier.</param>
        /// <returns>The socket color description.</returns>
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

        /// <summary>
        /// Gets the color associated with a socket.
        /// </summary>
        /// <param name="colorId">The color identifier.</param>
        /// <returns>The socket color as a hex string.</returns>
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

        /// <summary>
        /// Gets the string associated with a string identifier.
        /// </summary>
        /// <param name="stringId">The string identifier.</param>
        /// <returns>The string value.</returns>
        public string GetString(int stringId)
        {
            string stringName = _gmDatabaseService.GetString(stringId);

            return stringName;
        }

        /// <summary>
        /// Gets the formatted option name.
        /// </summary>
        /// <param name="option">The option identifier.</param>
        /// <param name="optionValue">The option value.</param>
        /// <param name="isFixedOption">Indicates if the option is fixed.</param>
        /// <returns>The formatted option name.</returns>
        public string GetOptionName(int option, int optionValue, bool isFixedOption = false)
        {
            string optionName = _gmDatabaseService.GetOptionName(option);
            (int secTime, float value) = _gmDatabaseService.GetOptionValues(option);

            string formattedOption = FormatNameID(option, optionName, $"{optionValue}", $"{secTime}", $"{value}");

            return option != 0 ? formattedOption : Resources.NoBuff;
        }

        /// <summary>
        /// Gets the color from an option.
        /// </summary>
        /// <param name="option">The option identifier.</param>
        /// <returns>The color as a hex string.</returns>
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

        /// <summary>
        /// Formats the main stat of an item.
        /// </summary>
        /// <param name="itemType">The item type.</param>
        /// <param name="physicalStat">The physical stat value.</param>
        /// <param name="magicStat">The magic stat value.</param>
        /// <param name="jobClass">The job class identifier.</param>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <param name="enhanceLevel">The enhance level.</param>
        /// <param name="gearLevel">The gear level.</param>
        /// <returns>The formatted main stat.</returns>
        public string FormatMainStat(int itemType, int physicalStat, int magicStat, int jobClass, int weaponId, int enhanceLevel, int gearLevel)
        {
            string mainStat = "";

            if ((ItemType)itemType == ItemType.Armor && physicalStat > 0 && magicStat > 0)
            {
                if (enhanceLevel > 0)
                {
                    (double defenseValue, int armorPlus) = _gmDatabaseService.GetArmorEnhanceValue(enhanceLevel);

                    double basePercentage = defenseValue * 100;
                    double percentage = basePercentage + armorPlus;

                    mainStat = $"{Resources.PhysicalDefense} +{physicalStat}\n" +
                               $"{Resources.ReducePhysicalDamage} +{percentage:0.00}%\n" +
                               $"{Resources.MagicDefense} +{magicStat}\n" +
                               $"{Resources.ReduceMagicDamage} +{percentage:0.00}%";

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
                    (var weaponValue, var weaponPlus) = _gmDatabaseService.GetWeaponEnhanceValue(enhanceLevel);
                    mainStat = $"{Resources.PhysicalDamage} +{physicalAttackMin}~{physicalAttackMax}\n{Resources.Add} {Resources.PhysicalDamage} +{Math.Round(physicalAttackMin * weaponValue + weaponPlus)}\n{Resources.MagicDamage} +{magicAttackMin}~{magicAttackMax}\n{Resources.Add} {Resources.MagicDamage} +{Math.Round(magicAttackMin * weaponValue + weaponPlus)}";
                }
                else
                {
                    mainStat = $"{Resources.PhysicalDamage} +{physicalAttackMin}~{physicalAttackMax}\n{Resources.MagicDamage} +{magicAttackMin}~{magicAttackMax}";
                }
            }

            return mainStat;
        }

        /// <summary>
        /// Formats the set effect of an item.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <returns>The formatted set effect.</returns>
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

        /// <summary>
        /// Formats the sell value of an item.
        /// </summary>
        /// <param name="sellPrice">The sell price.</param>
        /// <returns>The formatted sell value.</returns>
        public static string FormatSellValue(int sellPrice)
        {
            return sellPrice > 0 ? $"{Resources.SellValue}: {sellPrice:N0} {Resources.Gold}" : string.Empty;
        }

        /// <summary>
        /// Formats the required level of an item.
        /// </summary>
        /// <param name="levelLimit">The level limit.</param>
        /// <returns>The formatted required level.</returns>
        public static string FormatRequiredLevel(int levelLimit)
        {
            return levelLimit != 0 ? $"{Resources.RequiredLevel}: {levelLimit}" : string.Empty;
        }

        /// <summary>
        /// Formats the item trade status.
        /// </summary>
        /// <param name="itemTrade">The item trade status.</param>
        /// <returns>The formatted item trade status.</returns>
        public static string FormatItemTrade(int itemTrade)
        {
            return itemTrade == 0 ? $"{Resources.TradeUnavailable}" : string.Empty;
        }

        /// <summary>
        /// Formats the durability of an item.
        /// </summary>
        /// <param name="durability">The durability value.</param>
        /// <returns>The formatted durability.</returns>
        public static string FormatDurability(int durability)
        {
            return durability > 0 ? $"{Resources.Durability}: {durability / 100}/{durability / 100}" : string.Empty;
        }

        /// <summary>
        /// Formats the weight of an item.
        /// </summary>
        /// <param name="weight">The weight value.</param>
        /// <returns>The formatted weight.</returns>
        public static string FormatWeight(int weight)
        {
            return weight > 0 ? $"{weight / 1000.0:0.000}{Resources.Kg}" : string.Empty;
        }

        /// <summary>
        /// Formats the reconstruction status of an item.
        /// </summary>
        /// <param name="reconstruction">The reconstruction value.</param>
        /// <param name="reconstructionMax">The maximum reconstruction value.</param>
        /// <param name="itemBinding">The item binding status.</param>
        /// <returns>The formatted reconstruction status.</returns>
        public static string FormatReconstruction(int reconstruction, int reconstructionMax, int itemBinding)
        {
            if (reconstructionMax > 0 && itemBinding == 1)
            {
                return $"{Resources.AttributeItem} ({reconstruction} {Resources.Times}/{reconstructionMax} {Resources.Times})";
            }
            else if (reconstructionMax > 0 && itemBinding == 0)
            {
                return $"{Resources.BoundItem}";
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Formats the pet food status.
        /// </summary>
        /// <param name="petFood">The pet food status.</param>
        /// <returns>The formatted pet food status.</returns>
        public static string FormatPetFood(int petFood)
        {
            return petFood == 0 ? $"{Resources.PetFoodDescNo}" : $"{Resources.PetFoodDescYes}";
        }

        /// <summary>
        /// Formats the color associated with pet food.
        /// </summary>
        /// <param name="petFood">The pet food status.</param>
        /// <returns>The color as a hex string.</returns>
        public static string FormatPetFoodColor(int petFood)
        {
            return petFood == 0 ? "#e75151" : "#eed040";
        }

        /// <summary>
        /// Formats the augment stone value.
        /// </summary>
        /// <param name="value">The augment stone value.</param>
        /// <returns>The formatted augment stone value.</returns>
        public static string FormatAugmentStone(int value)
        {
            return value > 0 ? $"{Resources.PhysicalMagicDamage} +{value}" : string.Empty;
        }

        /// <summary>
        /// Formats the augment stone level.
        /// </summary>
        /// <param name="value">The augment stone level.</param>
        /// <returns>The formatted augment stone level.</returns>
        public static string FormatAugmentStoneLevel(int value)
        {
            return value > 0 ? $"{Resources.AugmentLevel} +{value}" : string.Empty;
        }

        /// <summary>
        /// Formats the cooldown value.
        /// </summary>
        /// <param name="value">The cooldown value.</param>
        /// <returns>The formatted cooldown value.</returns>
        public static string FormatCooldown(float value)
        {
            return value > 0 ? string.Format(Resources.CooldownText, $"{value:F2}") : string.Empty;
        }

        private const string ColorTagStart = "<COLOR:";
        private const string ColorTagEnd = ">";
        private const string ColorTagClose = "</COLOR>";
        private const string LineBreakTag = "<br>";

        /// <summary>
        /// Formats the name ID with the given replacements.
        /// </summary>
        /// <param name="optionID">The option identifier.</param>
        /// <param name="optionName">The option name.</param>
        /// <param name="replacement01">The first replacement value.</param>
        /// <param name="replacement02">The second replacement value.</param>
        /// <param name="replacement03">The third replacement value.</param>
        /// <returns>The formatted name ID.</returns>
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
                else if (optionName.Contains(Resources.OptionSkillDamagePlus) || optionName.Contains(Resources.OptionSkillCooldownLess))
                {
                    string skillName = GetOptionSkillName(optionID);

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

        /// <summary>
        /// Gets the skill name associated with an option ID.
        /// </summary>
        /// <param name="optionID">The option identifier.</param>
        /// <returns>The skill name.</returns>
        private static string GetOptionSkillName(int optionID)
        {
            return optionID switch
            {
                5002 or 5014 or 5017 or 5029 or 5038 or 5043 => Resources.OptionSkillNotFound,
                5003 or 5018 => Resources.OptionSkillWeapon,
                5004 or 5019 => Resources.OptionSkillCurse,
                5005 or 5020 => Resources.OptionSkillBlackSorcery,
                5006 or 5021 => Resources.OptionSkillSummon,
                5007 or 5022 => Resources.OptionSkillWind,
                5008 or 5023 => Resources.OptionSkillWater,
                5009 or 5024 => Resources.OptionSkillFire,
                5010 or 5025 => Resources.OptionSkillLight,
                5011 or 5026 => Resources.OptionSkillEarth,
                5012 or 5027 => Resources.OptionSkillKiGong,
                5013 or 5028 => Resources.OptionSkillSavage,
                5015 or 5030 or 5041 or 5046 => Resources.OptionSkillSpecial,
                5016 or 5031 => Resources.OptionSkillFighting,
                5032 or 5033 or 5034 or 5035 or 5036 or 5037 or 5048 or 5049 => Resources.OptionSkillUnknown,
                5039 or 5044 => Resources.OptionSkillTactical,
                5040 or 5045 => Resources.OptionSkillHeavyWeapon,
                5042 or 5047 => Resources.OptionSkillFirearm,
                _ => Resources.OptionSkillUnknown,
            };
        }

        /// <summary>
        /// Removes color tags from the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The string without color tags.</returns>
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

        /// <summary>
        /// Formats a percentage value in the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="placeholder">The placeholder to be replaced.</param>
        /// <param name="replacement">The replacement value.</param>
        /// <param name="isFixedOption">Indicates if the option is fixed.</param>
        /// <returns>The formatted string with the percentage value.</returns>
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

        /// <summary>
        /// Formats the title effect for a given title ID.
        /// </summary>
        /// <param name="titleId">The title identifier.</param>
        /// <returns>The formatted title effect.</returns>
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
                    description.Append($"[{Resources.TitleEffect}]");

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

                    description.AppendLine($"\n\n{Resources.ValidFor}: {formattedRemainTime}");
                }

                return description.ToString();
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: " + ex.Message);
                return string.Empty;
            }
        }
    }
}
