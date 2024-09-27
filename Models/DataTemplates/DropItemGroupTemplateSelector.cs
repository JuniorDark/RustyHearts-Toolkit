using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models.DataTemplates
{
    public class DropItemGroupTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? ItemDropGroupList { get; set; }
        public DataTemplate? ItemDropGroupListF { get; set; }
        public DataTemplate? ChampionItemItemDropGroupList { get; set; }
        public DataTemplate? InstanceItemDropGroupList { get; set; }
        public DataTemplate? QuestItemDropGroupList { get; set; }
        public DataTemplate? WorldItemDropGroupList { get; set; }
        public DataTemplate? RiddleBoxDropGroupList { get; set; }
        public DataTemplate? RareCardDropGroupList { get; set; }
        public DataTemplate? RareCardRewardItemList { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                ItemDropGroupType.ItemDropGroupListF => ItemDropGroupListF,
                ItemDropGroupType.ItemDropGroupList => ItemDropGroupList,
                ItemDropGroupType.ChampionItemItemDropGroupList => ChampionItemItemDropGroupList,
                ItemDropGroupType.InstanceItemDropGroupList or ItemDropGroupType.WorldInstanceItemDropGroupList => InstanceItemDropGroupList,
                ItemDropGroupType.QuestItemDropGroupList => QuestItemDropGroupList,
                ItemDropGroupType.EventWorldItemDropGroupList or ItemDropGroupType.WorldItemDropGroupList or ItemDropGroupType.WorldItemDropGroupListF => WorldItemDropGroupList,
                ItemDropGroupType.RiddleBoxDropGroupList => RiddleBoxDropGroupList,
                ItemDropGroupType.RareCardDropGroupList => RareCardDropGroupList,
                ItemDropGroupType.RareCardRewardItemList => RareCardRewardItemList,
                _ => null,
            };
        }
    }

}
