using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;

namespace RHToolkit.Behaviors
{
    public class ScrollIntoViewBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged += OnSelectionChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AssociatedObject.SelectedItem != null)
            {
                AssociatedObject.ScrollIntoView(AssociatedObject.SelectedItem);
            }
        }
    }
}
