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

    /// <summary>
    /// Called when the SelectedItemsString property changes.
    /// Updates the selected items in the ListView.
    /// </summary>
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

    /// <summary>
    /// Called when the behavior is attached to the ListView.
    /// Subscribes to the SelectionChanged event.
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.SelectionChanged += OnSelectionChanged;
    }

    /// <summary>
    /// Called when the behavior is detached from the ListView.
    /// Unsubscribes from the SelectionChanged event.
    /// </summary>
    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.SelectionChanged -= OnSelectionChanged;
    }

    /// <summary>
    /// Handles the SelectionChanged event for the ListView.
    /// Updates the SelectedItemsString property based on the currently selected items in the ListView.
    /// </summary>
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

    /// <summary>
    /// Updates the selected items in the ListView based on the SelectedItemsString property.
    /// </summary>
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