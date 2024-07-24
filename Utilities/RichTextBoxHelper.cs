using System.Windows.Controls;
using System.Windows.Documents;

namespace RHToolkit.Utilities
{
    public static class RichTextBoxHelper
    {
        public static readonly DependencyProperty FormattedTextProperty =
            DependencyProperty.RegisterAttached("FormattedText", typeof(string), typeof(RichTextBoxHelper),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextChanged));

        public static string GetFormattedText(DependencyObject obj)
        {
            return (string)obj.GetValue(FormattedTextProperty);
        }

        public static void SetFormattedText(DependencyObject obj, string value)
        {
            obj.SetValue(FormattedTextProperty, value);
        }

        private static void OnFormattedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichTextBox richTextBox && e.NewValue != null)
            {
                if (e.NewValue is string formattedText)
                {
                    richTextBox.Document = FormatRichTextBoxDescription(formattedText);
                }
                else
                {
                    richTextBox.Document = null;
                }
            }
        }


        private static FlowDocument FormatRichTextBoxDescription(string description)
        {
            FlowDocument document = new();
            string[] separators = ["<BR>", "<br>", "<Br>"];
            string[] parts = description.Split(separators, StringSplitOptions.None);

            foreach (string part in parts)
            {
                Paragraph paragraph = new();
                string[] colorSeparator = ["</COLOR>", "<COLOR>", "</color>"];
                string[] colorParts = part.Split(colorSeparator, StringSplitOptions.None);

                foreach (string colorPart in colorParts)
                {
                    int startTagIndex = colorPart.IndexOf("<COLOR:");
                    if (startTagIndex != -1)
                    {
                        string textBeforeColor = colorPart[..startTagIndex];
                        paragraph.Inlines.Add(new Run(textBeforeColor));
                        int endTagIndex = colorPart.IndexOf('>', startTagIndex);
                        if (endTagIndex != -1)
                        {
                            string colorTag = colorPart.Substring(startTagIndex + 7, endTagIndex - startTagIndex - 7);
                            if (!string.IsNullOrEmpty(colorTag))
                            {
                                colorTag = colorTag.PadRight(6, '0');
                                Color customColor;
                                try
                                {
                                    customColor = (Color)ColorConverter.ConvertFromString("#" + colorTag.ToLower());
                                }
                                catch (FormatException)
                                {
                                    customColor = Colors.White;
                                }

                                string textAfterColor = colorPart[(endTagIndex + 1)..];
                                Run run = new(textAfterColor)
                                {
                                    Foreground = new SolidColorBrush(customColor)
                                };
                                paragraph.Inlines.Add(run);
                            }
                        }
                    }
                    else
                    {
                        paragraph.Inlines.Add(new Run(colorPart));
                    }
                }

                document.Blocks.Add(paragraph);
            }

            return document;
        }

    }
}
