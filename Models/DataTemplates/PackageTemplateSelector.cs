using System.Windows.Controls;

namespace RHToolkit.Models.DataTemplates
{
    public class PackageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? UnionPackage { get; set; }
        public DataTemplate? ConditionSelectItem { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                1 or 2 => UnionPackage,
                3 => ConditionSelectItem,
                _ => null,
            };
        }
    }

}
