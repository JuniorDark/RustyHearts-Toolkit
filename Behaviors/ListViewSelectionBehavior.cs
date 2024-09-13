using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;

namespace RHToolkit.Behaviors;

public class ListViewSelectionBehavior : Behavior<ListView>
{
    private bool _isUpdating; // Flag to prevent recursion

    public static readonly DependencyProperty SelectedItemsStringProperty =
        DependencyProperty.Register(
            nameof(SelectedItemsString),
            typeof(string),
            typeof(ListViewSelectionBehavior),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsStringChanged));

    public string SelectedItemsString
    {
        get { return (string)GetValue(SelectedItemsStringProperty); }
        set { SetValue(SelectedItemsStringProperty, value); }
    }

    private static void OnSelectedItemsStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = (ListViewSelectionBehavior)d;
        if (behavior.AssociatedObject == null)
            return;

        if (!behavior._isUpdating)
        {
            behavior.UpdateSelectedItems();
        }
    }

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
        if (_isUpdating)
            return;

        _isUpdating = true;

        // Update the SelectedItemsString property based on the currently selected items in the ListView
        var selectedIds = AssociatedObject.SelectedItems
                            .Cast<dynamic>()
                            .Select(item => item.ID.ToString())
                            .ToList();

        SelectedItemsString = string.Join(",", selectedIds);

        _isUpdating = false;
    }

    private void UpdateSelectedItems()
    {
        if (AssociatedObject.ItemsSource == null || _isUpdating)
            return;

        _isUpdating = true;

        var selectedIds = (SelectedItemsString ?? string.Empty).Split(',')
                            .Where(id => !string.IsNullOrWhiteSpace(id))
                            .Select(int.Parse)
                            .ToList();

        AssociatedObject.SelectedItems.Clear();
        foreach (var item in AssociatedObject.Items)
        {
            var itemId = (item as dynamic).ID;
            if (selectedIds.Contains(itemId))
            {
                AssociatedObject.SelectedItems.Add(item);
            }
        }

        _isUpdating = false;
    }
}