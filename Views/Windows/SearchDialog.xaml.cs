using Azure.Core.Pipeline;

namespace RHToolkit.Views.Windows
{
    public partial class SearchDialog : Window
    {
        public event Action<string, bool>? FindNext;
        public event Action<string, bool>? CountMatches;

        public SearchDialog()
        {
            InitializeComponent();
        }

        private void FindNext_Click(object sender, RoutedEventArgs e)
        {
            FindNext?.Invoke(SearchTextBox.Text, MatchCaseCheckBox.IsChecked == true);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Count_Click(object sender, RoutedEventArgs e)
        {
            CountMatches?.Invoke(SearchTextBox.Text, MatchCaseCheckBox.IsChecked == true);
        }

        public void ShowMessage(string message, Brush foregroundColor)
        {
            MessageTextBlock.Text = message;
            MessageTextBlock.Foreground = foregroundColor;
            MessageTextBlock.Visibility = Visibility.Visible;
        }
    }
}
