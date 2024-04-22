using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using RHToolkit.Models;
using RHToolkit.Views;

namespace RHToolkit.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel()
        {

        }

        [RelayCommand]
        private static void OpenItemDatabaseWindow()
        {
            var viewModel = App.Services.BuildServiceProvider().GetRequiredService<ItemWindowViewModel>();
            var itemWindow = new ItemWindow(viewModel);
            itemWindow.Show();
        }

        [RelayCommand]
        private static void OpenMailWindow()
        {
            var viewModel = App.Services.BuildServiceProvider().GetRequiredService<MailWindowViewModel>();
            var mailWindow = new MailWindow(viewModel);
            mailWindow.Show();

        }

        [ObservableProperty]
        private bool _isTextBoxEnabled = true;

        [ObservableProperty]
        private string? _sQLServer = "192.168.44.208";
        partial void OnSQLServerChanged(string? value)
        {
            SqlCredentials.SQLServer = value;
        }

        [ObservableProperty]
        private string? _sQLUser = "sa";
        partial void OnSQLUserChanged(string? value)
        {
            SqlCredentials.SQLUser = value;
        }

        [ObservableProperty]
        private string? _sQLPwd = "RustyHearts";
        partial void OnSQLPwdChanged(string? value)
        {
            SqlCredentials.SQLPwd = value;
        }
    }
}
