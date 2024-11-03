using RHToolkit.Views.Pages;
using Wpf.Ui.Controls;

namespace RHToolkit.ViewModels.Windows;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = Resources.AppTitle;

    [ObservableProperty]
    private ObservableCollection<object> _menuItems =
    [
    new NavigationViewItem(Resources.Home, SymbolRegular.Home24, typeof(HomePage)),
    new NavigationViewItem()
    {
        Content = Resources.DatabaseTools,
        Icon = new SymbolIcon { Symbol = SymbolRegular.TextBulletListSquareToolbox20 },
        MenuItemsSource = new object[]
        {
            new NavigationViewItem(Resources.CharacterEdit, SymbolRegular.WindowDatabase24, typeof(CharacterEditPage)),
            new NavigationViewItem(Resources.CharacterRestore, SymbolRegular.WindowDatabase24, typeof(CharacterRestorePage)),
            new NavigationViewItem(Resources.Coupon, SymbolRegular.WindowDatabase24, typeof(CouponPage)),
            new NavigationViewItem(Resources.Tools, SymbolRegular.WindowDatabase24, typeof(DatabaseToolsPage)),
        }
    },
    new NavigationViewItem(Resources.EditTools, SymbolRegular.DocumentTextToolbox24, typeof(EditToolsPage)),
    new NavigationViewItem()
    {
        Content = Resources.SQLiteDatabase,
        Icon = new SymbolIcon { Symbol = SymbolRegular.TextBulletListSquareToolbox20 },
        MenuItemsSource = new object[]
        {
             new NavigationViewItem(Resources.Database, SymbolRegular.WindowDatabase24, typeof(DatabasePage)),
             new NavigationViewItem(Resources.SQLiteDatabaseManager, SymbolRegular.HomeDatabase24, typeof(GMDatabaseManagerPage)),
        }
    },
    ];

    [ObservableProperty]
    private ObservableCollection<object> _footerMenuItems =
    [
        new NavigationViewItem(Resources.Settings, SymbolRegular.Settings24, typeof(SettingsPage))
    ];

    [ObservableProperty]
    private ObservableCollection<MenuItem> _trayMenuItems =
    [
        new MenuItem { Header = Resources.Home, Tag = "tray_home" },
        new MenuItem { Header = "Close", Tag = "tray_close" }
    ];
}
