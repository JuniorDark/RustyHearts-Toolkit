using RHToolkit.Views.Pages;
using Wpf.Ui.Controls;

namespace RHToolkit.ViewModels.Windows;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = "Rusty Hearts Toolkit";

    [ObservableProperty]
    private ObservableCollection<object> _menuItems =
    [
        new NavigationViewItem("Home", SymbolRegular.Home24, typeof(HomePage)),
    new NavigationViewItem()
    {
        Content = "Database Tools",
        Icon = new SymbolIcon { Symbol = SymbolRegular.TextBulletListSquareToolbox20 },
        MenuItemsSource = new object[]
        {
            new NavigationViewItem("Tools", SymbolRegular.WindowDatabase24, typeof(DatabaseToolsPage)),
             new NavigationViewItem("Database", SymbolRegular.WindowDatabase24, typeof(DatabasePage)),
        }
    },
    new NavigationViewItemSeparator(),
    ];

    [ObservableProperty]
    private ObservableCollection<object> _footerMenuItems =
    [
        new NavigationViewItem("Settings", SymbolRegular.Settings24, typeof(SettingsPage))
    ];

    [ObservableProperty]
    private ObservableCollection<MenuItem> _trayMenuItems =
    [
        new MenuItem { Header = "Home", Tag = "tray_home" },
    new MenuItem { Header = "Close", Tag = "tray_close" }
    ];
}
