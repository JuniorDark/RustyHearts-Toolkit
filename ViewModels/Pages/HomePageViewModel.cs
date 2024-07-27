using RHToolkit.Utilities.Converters;
using Wpf.Ui;

namespace RHToolkit.ViewModels.Pages
{
    public partial class HomePageViewModel(INavigationService navigationService) : ViewModel
    {
        [RelayCommand]
        private void OnCardClick(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }

            Type? pageType = NameToPageTypeConverter.Convert(parameter);

            if (pageType == null)
            {
                return;
            }

            _ = navigationService.Navigate(pageType);
        }
    }
}
