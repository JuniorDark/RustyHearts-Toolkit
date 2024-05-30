namespace RHToolkit.Views.Windows;

public partial class SearchDialog : Window
{
    public event Action<string, bool>? FindNext;
    public event Action<string, bool>? ReplaceFindNext;
    public event Action<string, bool>? CountMatches;
    public event Action<string, string, bool>? Replace;
    public event Action<string, string, bool>? ReplaceAll;

    public SearchDialog()
    {
        InitializeComponent();
    }

    private void FindNext_Click(object sender, RoutedEventArgs e)
    {
        FindNext?.Invoke(FindTextBox.Text, MatchCaseCheckBox.IsChecked == true);
    }

    private void ReplaceFindNext_Click(object sender, RoutedEventArgs e)
    {
        ReplaceFindNext?.Invoke(ReplaceFindTextBox.Text, MatchCaseCheckBox.IsChecked == true);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Count_Click(object sender, RoutedEventArgs e)
    {
        CountMatches?.Invoke(FindTextBox.Text, MatchCaseCheckBox.IsChecked == true);
    }

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        Replace?.Invoke(ReplaceFindTextBox.Text, ReplaceTextBox.Text, MatchCaseCheckBox.IsChecked == true);
    }

    private void ReplaceAll_Click(object sender, RoutedEventArgs e)
    {
        ReplaceAll?.Invoke(ReplaceFindTextBox.Text, ReplaceTextBox.Text, MatchCaseCheckBox.IsChecked == true);
    }

    public void ShowMessage(string message, Brush foregroundColor)
    {
        MessageTextBlock.Text = message;
        MessageTextBlock.Foreground = foregroundColor;
        MessageTextBlock.Visibility = Visibility.Visible;
    }

}
