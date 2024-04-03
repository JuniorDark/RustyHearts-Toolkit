using static RHGMTool.Models.EnumService;

namespace RHGMTool.Services
{
    public class FrameData
    {
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
                SocketColor.Red => ("Processed Red Gem Socket", "Red"),
                SocketColor.Blue => ("Processed Blue Gem Socket", "Blue"),
                SocketColor.Yellow => ("Processed Yellow Gem Socket", "Yellow"),
                SocketColor.Green => ("Processed Green Gem Socket", "Green"),
                SocketColor.Colorless => ("Processed Colorless Socket", "Gray"),
                SocketColor.Gray => ("Processed Gray Socket", "Gray"),
                _ => ("Unprocessed Gem Socket", "White"),
            };
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
