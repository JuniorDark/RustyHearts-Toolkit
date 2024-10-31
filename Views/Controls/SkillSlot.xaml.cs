using RHToolkit.ViewModels.Controls;
using System.Windows.Controls;

namespace RHToolkit.Views.Controls
{
    public partial class SkillSlot : UserControl
    {
        public static readonly DependencyProperty IsButtonEnabledProperty = DependencyProperty.Register(
            "IsButtonEnabled", typeof(bool), typeof(SkillSlot), new PropertyMetadata(true));

        public static readonly DependencyProperty AddSkillCommandProperty = DependencyProperty.Register(
            "AddSkillCommand", typeof(ICommand), typeof(SkillSlot));

        public static readonly DependencyProperty RemoveSkillCommandProperty = DependencyProperty.Register(
            "RemoveSkillCommand", typeof(ICommand), typeof(SkillSlot));

        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            "CommandParameter", typeof(string), typeof(SkillSlot), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty SkillDataViewModelProperty = DependencyProperty.Register(
            "SkillDataViewModel", typeof(SkillDataViewModel), typeof(SkillSlot), new PropertyMetadata(null));
        
        public static readonly DependencyProperty SkillTypeProperty = DependencyProperty.Register(
            "SkillType", typeof(string), typeof(SkillSlot), new PropertyMetadata(string.Empty));

        public bool IsButtonEnabled
        {
            get => (bool)GetValue(IsButtonEnabledProperty);
            set => SetValue(IsButtonEnabledProperty, value);
        }

        public ICommand AddSkillCommand
        {
            get => (ICommand)GetValue(AddSkillCommandProperty);
            set => SetValue(AddSkillCommandProperty, value);
        }

        public ICommand RemoveSkillCommand
        {
            get => (ICommand)GetValue(RemoveSkillCommandProperty);
            set => SetValue(RemoveSkillCommandProperty, value);
        }

        public string CommandParameter
        {
            get => (string)GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public SkillDataViewModel SkillDataViewModel
        {
            get => (SkillDataViewModel)GetValue(SkillDataViewModelProperty);
            set => SetValue(SkillDataViewModelProperty, value);
        }

        public string SkillType
        {
            get => (string)GetValue(SkillTypeProperty);
            set => SetValue(SkillTypeProperty, value);
        }

        public SkillSlot()
        {
            InitializeComponent();
        }
    }

}
