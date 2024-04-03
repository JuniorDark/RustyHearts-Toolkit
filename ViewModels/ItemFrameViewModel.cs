using RHGMTool.Models;
using RHGMTool.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using System.Windows.Media;
using static RHGMTool.Models.EnumService;

namespace RHGMTool.ViewModels
{
    public class ItemFrameViewModel : INotifyPropertyChanged
    {
        private ItemData? _item;

        public ItemData? Item
        {
            get { return _item; }
            set
            {
                _item = value;
                OnPropertyChanged();
                UpdateItemData();
            }
        }

        public void UpdateItemData()
        {
            if (Item != null)
            {
                ItemName = Item.Name;
                ItemNameColor = FrameData.GetBranchColor(Item.Branch);

                //Category = SQLiteDatabaseReader.GetCategoryName(Item.Category);
                //SubCategory = SQLiteDatabaseReader.GetSubCategoryName(Item.SubCategory);
                JobClass = GetEnumDescription((CharClass)Item.JobClass);
                Weight = Item.Weight > 0 ? $"{Item.Weight / 1000.0:0.000}Kg" : "";
                SellValue = Item.SellPrice > 0 ? $"{Item.SellPrice:N0} Gold" : "";
                ReqLevel = $"Required Level: {Item.LevelLimit}";
                ItemTrade = Item.ItemTrade == 0 ? "Trade Unavailable" : "";
                IsGemCategory = Item.Category == 29;
                RandomBuff = IsGemCategory ? "[Buff]" : "";
                RandomBuff01 = IsGemCategory ? "No Buff" : "";

                PetFood = Item.PetFood == 0 ? "This item cannot be used as Pet Food" : "This item can be used as Pet Food";
                PetFoodColor = Item.PetFood == 0 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e75151")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#eed040"));

                FormatRichTextBoxDescription(Item.Description);
            }
        }

        private static readonly string[] separator = ["<BR>", "<br>", "<Br>"];

        private void FormatRichTextBoxDescription(string? description)
        {
            if (string.IsNullOrEmpty(description))
            {
                // Clear the rich text box content if the description is empty
                RichTextBoxContent = null;
                return;
            }

            // Create a FlowDocument to hold the rich text content
            FlowDocument flowDocument = new();

            // Split the description into parts based on line breaks ("<BR>", "<br>", "<Br>")
            string[] parts = description.Split(separator, StringSplitOptions.None);

            foreach (string part in parts)
            {
                if (part.StartsWith("<COLOR:"))
                {
                    // Extract the color value from the tag, e.g., "<COLOR:06EBE8>"
                    int tagEnd = part.IndexOf('>');
                    if (tagEnd != -1)
                    {
                        string colorTag = part[7..tagEnd];
                        string text = part[(tagEnd + 1)..];

                        Run run = new(text)
                        {
                            Foreground = (Brush?)new BrushConverter().ConvertFromString("#" + colorTag.ToLower())
                        };

                        flowDocument.Blocks.Add(new Paragraph(run));
                    }
                }
                else
                {
                    // Create a Run element for the part with default color
                    Run run = new(part);

                    // Add the Run element to the FlowDocument
                    flowDocument.Blocks.Add(new Paragraph(run));
                }
            }

            // Set the FlowDocument as the content of the RichTextBox
            RichTextBoxContent = flowDocument;
        }

        // Property to bind to the RichTextBox content
        private FlowDocument? _richTextBoxContent;

        public FlowDocument? RichTextBoxContent
        {
            get { return _richTextBoxContent; }
            set
            {
                _richTextBoxContent = value;
                OnPropertyChanged(nameof(RichTextBoxContent));
            }
        }

        // Add properties for all UI elements
        public string? ItemName { get; private set; }
        public string? ItemNameColor { get; private set; }
        public string? Category { get; private set; }
        public string? SubCategory { get; private set; }
        public string? JobClass { get; private set; }
        public string? Weight { get; private set; }
        public string? SellValue { get; private set; }
        public string? ReqLevel { get; private set; }
        public string? ItemTrade { get; private set; }
        public bool IsGemCategory { get; private set; }
        public string? RandomBuff { get; private set; }
        public string? RandomBuff01 { get; private set; }
        public string? PetFood { get; private set; }
        public Brush? PetFoodColor { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
