using RHToolkit.ViewModels.Windows.Tools.VM;
using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models.DataTemplates
{
    public class DropItemGroupItemsTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? ItemDropGroupListFItems { get; set; }
        public DataTemplate? ItemDropGroupListItems { get; set; }
        public DataTemplate? InstanceItemDropGroupListItems { get; set; }
        public DataTemplate? WorldItemDropGroupListItems { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is ItemDropGroup dropItem)
            {
                return dropItem.DropItemGroupType switch
                {
                    ItemDropGroupType.ItemDropGroupListF => ItemDropGroupListFItems,
                    ItemDropGroupType.ItemDropGroupList => ItemDropGroupListItems,
                    ItemDropGroupType.ChampionItemItemDropGroupList or ItemDropGroupType.InstanceItemDropGroupList or ItemDropGroupType.QuestItemDropGroupList or ItemDropGroupType.WorldInstanceItemDropGroupList => InstanceItemDropGroupListItems,
                    ItemDropGroupType.EventWorldItemDropGroupList or ItemDropGroupType.WorldItemDropGroupList or ItemDropGroupType.WorldItemDropGroupListF => WorldItemDropGroupListItems,
                    _ => null,
                };
            }
            return null;
        }
    }

}
