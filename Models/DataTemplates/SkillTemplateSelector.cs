using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models.DataTemplates
{
    public class SkillTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? Skill { get; set; }
        public DataTemplate? SkillTree { get; set; }
        public DataTemplate? SkillUI { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                SkillType.SkillFrantz or SkillType.SkillAngela or SkillType.SkillTude or SkillType.SkillNatasha => Skill,
                SkillType.SkillTreeFrantz or SkillType.SkillTreeAngela or SkillType.SkillTreeTude or SkillType.SkillTreeNatasha => SkillTree,
                SkillType.SkillUIFrantz or SkillType.SkillUIAngela or SkillType.SkillUITude or SkillType.SkillUINatasha => SkillUI,
                _ => null,
            };
        }
    }

}
