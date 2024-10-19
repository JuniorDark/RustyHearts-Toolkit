using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models.DataTemplates
{
    public class EnemyTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? Enemy { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                EnemyType.Enemy => Enemy,
                _ => null,
            };
        }
    }

}
