using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models.DataTemplates
{
    public class QuestTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? Mission { get; set; }
        public DataTemplate? MissionReward { get; set; }
        public DataTemplate? PartyMission { get; set; }
        public DataTemplate? Quest { get; set; }
        public DataTemplate? QuestAcquire { get; set; }
        public DataTemplate? QuestComplete { get; set; }
        public DataTemplate? QuestGroup { get; set; }
        public DataTemplate? QuestGroupComplete { get; set; }
        public DataTemplate? QuestString { get; set; }
        public DataTemplate? QuestGroupString { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                QuestType.Mission => Mission,
                QuestType.MissionReward => MissionReward,
                QuestType.PartyMission => PartyMission,
                QuestType.Quest => Quest,
                QuestType.QuestAcquire => QuestAcquire,
                QuestType.QuestComplete or QuestType.QuestRequest => QuestComplete,
                QuestType.QuestGroup => QuestGroup,
                QuestType.QuestString or QuestType.QuestAcquireString => QuestString,
                QuestType.QuestGroupString => QuestGroupString,
                QuestType.QuestGroupComplete or QuestType.QuestGroupRequest => QuestGroupComplete,
                _ => null,
            };
        }
    }

}
