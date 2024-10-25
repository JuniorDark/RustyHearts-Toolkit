using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models.DataTemplates
{
    public class WorldTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? World { get; set; }
        public DataTemplate? MapSelectCurtis { get; set; }
        public DataTemplate? DungeonInfoList { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                WorldType.World => World,
                WorldType.MapSelectCurtis => MapSelectCurtis,
                WorldType.DungeonInfoList => DungeonInfoList,
                _ => null,
            };
        }
    }

}
