using RHToolkit.Models;
using RHToolkit.ViewModels.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using ListView = Wpf.Ui.Controls.ListView;

namespace RHToolkit.Views.Windows
{
    public partial class NPCShopEditorWindow : Window
    {
        public NPCShopEditorWindow(NPCShopEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NPCShopEditorViewModel.PreviewClassList))
            {
                UpdateListViewSelection();
            }
        }

        private void UpdateListViewSelection()
        {
            // Find the ListView within the DataTemplate
            var listView = FindVisualChild<ListView>(this, "classListView");

            if (listView != null && DataContext is NPCShopEditorViewModel viewModel)
            {
                var itemsToSelect = viewModel.PreviewClassList?
                    .Split(", ")
                    .Select(id => int.TryParse(id, out var intId) ? intId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.HasValue
                        ? listView.ItemsSource
                            .Cast<NameID>()
                            .FirstOrDefault(item => item.ID == id.Value)
                        : null)
                    .Where(item => item != null)
                    .ToList();

                if (itemsToSelect != null)
                {
                    listView.SelectedItems.Clear();
                    foreach (var item in itemsToSelect)
                    {
                        listView.SelectedItems.Add(item);
                    }
                }
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItems != null)
            {
                if (DataContext is NPCShopEditorViewModel viewModel)
                {
                    viewModel.SelectedClassItems = listView.SelectedItems.Cast<NameID>().ToList();
                }
            }
        }

        private T? FindVisualChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t && (child as FrameworkElement)?.Name == name)
                {
                    return t;
                }

                var childOfChild = FindVisualChild<T>(child, name);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is NPCShopEditorViewModel viewModel)
            {
                bool canClose = await viewModel.CloseFile();

                if (!canClose)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
