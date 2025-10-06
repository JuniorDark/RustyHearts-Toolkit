using Microsoft.Win32;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.Model3D;
using RHToolkit.Models.UISettings;

namespace RHToolkit.ViewModels.Pages;

/// <summary>
/// ViewModel for the Model Tools page.
/// </summary>
public partial class ModelToolsViewModel : ObservableObject
{
    private CancellationTokenSource? _cts;

    #region Commands

    #region Folders
    [RelayCommand(CanExecute = nameof(CanSelectFolder))]
    private void SelectInputFolder()
    {
        var dlg = new OpenFolderDialog();
        if (dlg.ShowDialog() == true) InputFolder = dlg.FolderName;
    }

    [RelayCommand(CanExecute = nameof(CanSelectFolder))]
    private void SelectOutputFolder()
    {
        var dlg = new OpenFolderDialog();
        if (dlg.ShowDialog() == true) OutputFolder = dlg.FolderName;
    }

    private bool CanSelectFolder() => _cts is null;

    #endregion

    #region Export Files Command
    private bool CanExport() => !string.IsNullOrWhiteSpace(InputFolder) && !string.IsNullOrWhiteSpace(OutputFolder) && _cts is null;
    /// <summary>
    /// Exports the files from the input folder to the output folder.
    /// </summary>
    /// <returns></returns>
    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportFiles()
    {
        if (InputFolder == null || OutputFolder == null) return;

        try
        {
            BeginBusy();
            ResetUI();

            ReportProgress(Resources.ModelTool_ReadingFiles);

            var filesToExport = await ModelManager.EnumerateFilesToExportAsync(InputFolder, _cts!.Token);

            if (filesToExport.Count == 0)
            {
                ReportProgress(Resources.ModelTool_NoFiles);
                ResetUI();
                return;
            }

            var msg = string.Format(Resources.ModelTool_ExportMessage, filesToExport.Count);
            var answer = RHMessageBoxHelper.ConfirmMessageYesNoCancel(msg);
            if (answer != MessageBoxResult.Yes)
            {
                ResetUI();
                return;
            }

            var progress = new Progress<(string file, int pos, int count)>(t =>
            {
                IsProgressBarVisible = Visibility.Visible;
                ProgressBarMaximumValue = t.count;
                ProgressBarValue = t.pos;
                ReportProgress(string.Format(Resources.ModelTool_ExportingFiles, t.file, t.pos, t.count));
            });

            var summary = await ModelManager.ExportFilesAsync(
                OutputFolder,
                filesToExport,
                progress,
                _cts.Token);

            // Counts
            ReportProgress($"Exported {summary.Exported}/{summary.Total}. Skipped {summary.Skipped}.");

            // Show skipped list (if any)
            if (summary.Skipped > 0)
            {
                // Build a readable message (trim if very long)
                const int maxLines = 50;
                var lines = summary.SkippedFiles
                    .Select(s => $"- {s.File} ({s.Reason})")
                    .Take(maxLines)
                    .ToList();

                var more = summary.SkippedFiles.Count - lines.Count;
                if (more > 0) lines.Add($"... (+{more} more)");

                var text = "Skipped files:\n" + string.Join(Environment.NewLine, lines);

                RHMessageBoxHelper.ShowOKMessage(text, $"Exported {summary.Exported}/{summary.Total}. Skipped {summary.Skipped}.");

                try
                {
                    var logPath = Path.Combine(OutputFolder, "export_skipped.txt");
                    File.WriteAllLines(logPath, summary.SkippedFiles.Select(s => $"{s.File}\t{s.Reason}"));
                }
                catch { }
            }
        }
        catch (OperationCanceledException)
        {
            ReportProgress(Resources.OperationCancelledMessage);
            ResetUI();
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
        finally
        {
            EndBusy();
        }
    }

    #endregion

    #region Cancel Operation
    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelOperation() => _cts?.Cancel();
    private bool CanCancel() => _cts is not null;
    #endregion

    #endregion

    #region Helpers
    
    private void BeginBusy()
    {
        _cts = new CancellationTokenSource();
        IsCancelVisible = Visibility.Visible;
        IsTextBoxEnabled = false;
        NotifyCanExecuteCommands();
    }
    private void EndBusy()
    {
        _cts = null;
        IsCancelVisible = Visibility.Hidden;
        IsTextBoxEnabled = true;
        NotifyCanExecuteCommands();
        ResetUI();
    }

    private void NotifyCanExecuteCommands()
    {
        CancelOperationCommand.NotifyCanExecuteChanged();
        ExportFilesCommand.NotifyCanExecuteChanged();
        SelectInputFolderCommand.NotifyCanExecuteChanged();
        SelectOutputFolderCommand.NotifyCanExecuteChanged();
    }

    private void ResetUI()
    {
        ReportProgress("");
        IsProgressBarVisible = Visibility.Hidden;
        IsExportControlsVisible = Visibility.Collapsed;
        ProgressBarMiniumValue = 0;
        ProgressBarMaximumValue = 0;
        ProgressBarValue = 0;
        NotifyCanExecuteCommands();
    }

    private void ReportProgress(string msg) => ProgressMessage = msg;
    #endregion

    #region Properties

    [ObservableProperty] private Visibility _isCancelVisible = Visibility.Hidden;

    [ObservableProperty] private string? _inputFolder = RegistrySettingsHelper.GetInputFolder();
    partial void OnInputFolderChanged(string? value)
    {
        if (value is not null) RegistrySettingsHelper.SetInputFolder(value);
        ExportFilesCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty] private string? _outputFolder = RegistrySettingsHelper.GetOutputFolder();
    partial void OnOutputFolderChanged(string? value)
    {
        if (value is not null) RegistrySettingsHelper.SetOutputFolder(value);
        ExportFilesCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty] private string? _progressMessage;
    [ObservableProperty] private Visibility _isProgressBarVisible = Visibility.Hidden;
    [ObservableProperty] private Visibility _isProgressMessageVisible = Visibility.Visible;
    [ObservableProperty] private Visibility _isExportControlsVisible = Visibility.Collapsed;
    [ObservableProperty] private int _progressBarValue;
    [ObservableProperty] private int _progressBarMiniumValue;
    [ObservableProperty] private int _progressBarMaximumValue;
    [ObservableProperty] private bool _isTextBoxEnabled = true;

    #endregion
}