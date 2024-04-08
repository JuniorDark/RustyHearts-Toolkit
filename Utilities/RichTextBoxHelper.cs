using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RHGMTool.Utilities
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
            if (d is RichTextBox richTextBox)
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

        private static readonly string[] separator = ["<BR>", "<br>", "<Br>"];

        private static FlowDocument FormatRichTextBoxDescription(string description)
        {
            FlowDocument document = new();
            List<string> parts = new(description.Split(separator, StringSplitOptions.None));

            foreach (string part in parts)
            {
                Paragraph paragraph = new();
                string text = part;

                if (part.StartsWith("<COLOR:"))
                {
                    int tagEnd = part.IndexOf('>');
                    if (tagEnd != -1)
                    {
                        string colorTag = part[7..tagEnd];
                        Color customColor = (Color)ColorConverter.ConvertFromString("#" + colorTag.ToLower());

                        int closingTagIndex = part.IndexOf("</COLOR>", tagEnd, StringComparison.OrdinalIgnoreCase);
                        if (closingTagIndex != -1)
                        {
                            text = part.Substring(tagEnd + 1, closingTagIndex - tagEnd - 1); // Extract text between color tags
                        }
                        else
                        {
                            closingTagIndex = part.IndexOf("<COLOR>", tagEnd, StringComparison.OrdinalIgnoreCase);
                            if (closingTagIndex != -1)
                            {
                                text = part.Substring(tagEnd + 1, closingTagIndex - tagEnd - 1); // Extract text between color tags
                            }
                            else
                            {
                                text = part[(tagEnd + 1)..]; // Extract text after color tag
                            }
                        }

                        Run run = new(text)
                        {
                            Foreground = new SolidColorBrush(customColor)
                        };
                        paragraph.Inlines.Add(run);
                    }
                }
                else
                {
                    paragraph.Inlines.Add(new Run(text));
                }

                document.Blocks.Add(paragraph);
            }

            return document;
        }




    }
}
