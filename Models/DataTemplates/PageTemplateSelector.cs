using System.Windows.Controls;

namespace RHToolkit.Models.DataTemplates;

public class PageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Page1 { get; set; }
    public DataTemplate? Page2 { get; set; }
    public DataTemplate? Page3 { get; set; }
    public DataTemplate? Page4 { get; set; }
    public DataTemplate? Page5 { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is int currentPage)
        {
            return currentPage switch
            {
                1 => Page1,
                2 => Page2,
                3 => Page3,
                4 => Page4,
                5 => Page5,
                _ => Page1,
            };
        }
        return Page1;
    }
}


