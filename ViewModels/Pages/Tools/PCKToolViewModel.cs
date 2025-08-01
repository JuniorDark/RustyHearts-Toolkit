using Microsoft.Win32;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.PCK;
using RHToolkit.Models.UISettings;
using System.Diagnostics;

namespace RHToolkit.ViewModels.Pages;

/// <summary>
/// ViewModel for the PCK Tool page.
/// </summary>
public partial class PCKToolViewModel : ObservableObject
{
    private CancellationTokenSource? _cts;
    private readonly System.Timers.Timer _filterUpdateTimer;

    public PCKToolViewModel()
    {
        _filterUpdateTimer = new System.Timers.Timer(500);
        _filterUpdateTimer.Elapsed += FilterUpdateTimerElapsed;
        _filterUpdateTimer.AutoReset = false;
    }


    #region Commands

    #region Folders
    [RelayCommand(CanExecute = nameof(CanSelectFolder))]
    private void SelectClientFolder()
    {
        var dlg = new OpenFolderDialog();
        if (dlg.ShowDialog() == true) SelectedClientFolder = dlg.FolderName;
    }

    [RelayCommand(CanExecute = nameof(CanSelectFolder))]
    private void SelectFilesToPackFolder()
    {
        var dlg = new OpenFolderDialog();
        if (dlg.ShowDialog() == true) SelectedFilesToPackFolder = dlg.FolderName;
    }

    private bool CanSelectFolder() => _cts is null;

    [RelayCommand(CanExecute = nameof(CanOpenUnpackDir))]
    private void OpenUnpackDirectory()
    {
        if (string.IsNullOrWhiteSpace(SelectedClientFolder)) return;
        try
        {
            string dirOutput = Path.Combine(SelectedClientFolder, "PckOutput");
            if (!Directory.Exists(dirOutput)) Directory.CreateDirectory(dirOutput);
            Process.Start(new ProcessStartInfo { FileName = dirOutput, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }
    private bool CanOpenUnpackDir() => !string.IsNullOrWhiteSpace(SelectedClientFolder);
    #endregion

    #region ReadFileList
    private List<PCKFile>? _allPckFiles;

    [RelayCommand(CanExecute = nameof(CanReadFileList))]
    private async Task ReadFileList()
    {
        if (SelectedClientFolder is null) return;
        if (!PCKManager.IsGameDirectory(SelectedClientFolder))
        {
            RHMessageBoxHelper.ShowOKMessage(Resources.PCKTool_SelectFolder, Resources.Error);
            return;
        }

        try
        {
            BeginBusy();
            ResetUI();

            _allPckFiles = await PCKReader.ReadPCKFileListAsync(SelectedClientFolder, _cts!.Token);

            if (_allPckFiles.Count > 0)
            {
                ProgressBarMiniumValue = 0;
                ProgressBarMaximumValue = _allPckFiles.Count;
                ProgressBarValue = 0;
                IsProgressBarVisible = Visibility.Visible;

                var progress = new Progress<int>(_ => ProgressBarValue++);
                ReportProgress(Resources.PCKTool_ImportingTreeView);
                var tree = await PCKManager.BuildTreeAsync(_allPckFiles, progress, _cts.Token);
                PckTreeView = tree;
                OriginalTree = tree;
                ReportProgress(string.Format(Resources.PCKTool_FileNumber, _allPckFiles.Count));
                IsUnpackControlsVisible = Visibility.Visible;
            }
        }
        catch (OperationCanceledException)
        {
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
    private bool CanReadFileList() => !string.IsNullOrWhiteSpace(SelectedClientFolder) && _cts is null;
    #endregion

    #region Unpack 
    [RelayCommand(CanExecute = nameof(CanUnpack))]
    private Task UnpackPCK() => Unpack(false);

    [RelayCommand(CanExecute = nameof(CanUnpack))]
    private Task UnpackAllPCK() => Unpack(true);

    private List<PCKFile>? _toUnpack;
    private async Task Unpack(bool all = false)
    {
        if (SelectedClientFolder is null) return;
        if (!PCKManager.IsGameDirectory(SelectedClientFolder))
        {
            RHMessageBoxHelper.ShowOKMessage(Resources.PCKTool_SelectFolder, Resources.Error);
            return;
        }

        try
        {
            if (PckTreeView is null) return;
            _toUnpack = all ? _allPckFiles : PCKManager.GetCheckedFiles(PckTreeView);
            if (_toUnpack is { Count: 0 })
            {
                RHMessageBoxHelper.ShowOKMessage(Resources.PCKTool_SelectFilesMessage, Resources.Info);
                return;
            }

            BeginBusy();
            ReportProgress(Resources.PCKTool_Unpacking);

            var progress = new Progress<(int pos, int count)>(t =>
            {
                IsProgressBarVisible = Visibility.Visible;
                ProgressBarMaximumValue = t.count;
                ProgressBarValue = t.pos;
                ReportProgress(string.Format(Resources.PCKTool_UnpackingFiles, t.pos, t.count));
            });

            using var pckReader = new PCKReader(SelectedClientFolder);
            await Task.Run(() => pckReader.UnpackPCK(_toUnpack!, ReplaceUnpackFile, progress, _cts!.Token));
            ReportProgress(Resources.PCKTool_UnpackingComplete);
            await Task.Delay(3000);
            ReportProgress(string.Format(Resources.PCKTool_FileNumber, _allPckFiles?.Count ?? 0));
        }
        catch (OperationCanceledException)
        {
            ReportProgress(Resources.PCKTool_CancelledMessage);
            await Task.Delay(3000);
            ReportProgress(string.Format(Resources.PCKTool_FileNumber, _allPckFiles?.Count ?? 0));
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
        finally
        {
            ProgressBarMiniumValue = 0;
            ProgressBarMaximumValue = 0;
            ProgressBarValue = 0;
            EndBusy();
        }
    }
    private bool CanUnpack() => PckTreeView is not null && _cts is null;
    #endregion

    #region PackFiles
    /// <summary>
    /// Packs the files from the selected folder into the PCK archives.
    /// </summary>
    /// <returns></returns>
    [RelayCommand(CanExecute = nameof(CanPack))]
    private async Task PackFilesToPCK()
    {
        if (SelectedFilesToPackFolder == null || SelectedClientFolder == null) return;

        try
        {
            BeginBusy();
            ResetUI();

            ReportProgress(Resources.PCKTool_CheckingFiles);

            var existingFiles = await PCKReader.ReadPCKFileListAsync(SelectedClientFolder, CancellationToken.None);
            var pckMap = existingFiles.ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);

            var filesToPack = await PCKManager.EnumerateAndFilterFilesToPackAsync(
                SelectedFilesToPackFolder,
                pckMap,
                _cts!.Token);

            if (filesToPack.Count == 0)
            {
                ReportProgress(Resources.PCKTool_NoFilesToPack);
                ResetUI();
                return;
            }

            bool create = existingFiles.Count == 0;
            var msg = create
                ? string.Format(Resources.PCKTool_CreateNewMessage, filesToPack.Count)
                : string.Format(Resources.PCKTool_UpdateMessage, existingFiles.Count, filesToPack.Count);

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
                ReportProgress(string.Format(Resources.PCKTool_Packing, t.pos, t.count));
            });

            using (var writer = new PCKWriter())
            {
                await PCKWriter.WritePCKFilesAsync(
                SelectedClientFolder,
                filesToPack,
                progress,
                create,
                _cts.Token);
            }

            ReportProgress(string.Format(Resources.PCKTool_PackingComplete, filesToPack.Count));
        }
        catch (OperationCanceledException)
        {
            ReportProgress(Resources.PCKTool_CancelledMessage);
            ResetUI();
        }
        catch (Exception ex)
        {
            ReportProgress(Resources.Error);
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
        finally
        {
            EndBusy();
        }
    }

    private bool CanPack() => !string.IsNullOrWhiteSpace(SelectedClientFolder) && !string.IsNullOrWhiteSpace(SelectedFilesToPackFolder) && _cts is null;
    #endregion

    #region Cancel Operation
    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelOperation() => _cts?.Cancel();
    private bool CanCancel() => _cts is not null;
    #endregion

    #region Filter

    private void FilterTreeBySearch()
    {

        if (OriginalTree is null || PckTreeView is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            PckTreeView = OriginalTree;
            return;
        }

        PckTreeView = PCKManager.FilterTree(OriginalTree, SearchText);
    }

    private void TriggerFilterUpdate()
    {
        _filterUpdateTimer.Stop();
        _filterUpdateTimer.Start();
    }

    private void FilterUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _filterUpdateTimer.Stop();
            FilterTreeBySearch();
        });
    }

    [ObservableProperty] private string? _searchText;
    partial void OnSearchTextChanged(string? value)
    {
        TriggerFilterUpdate();
    }
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
    }

    private void NotifyCanExecuteCommands()
    {
        CancelOperationCommand.NotifyCanExecuteChanged();
        ReadFileListCommand.NotifyCanExecuteChanged();
        UnpackPCKCommand.NotifyCanExecuteChanged();
        UnpackAllPCKCommand.NotifyCanExecuteChanged();
        PackFilesToPCKCommand.NotifyCanExecuteChanged();
        SelectClientFolderCommand.NotifyCanExecuteChanged();
        SelectFilesToPackFolderCommand.NotifyCanExecuteChanged();
    }

    private void ResetUI()
    {
        IsProgressBarVisible = Visibility.Hidden;
        IsUnpackControlsVisible = Visibility.Collapsed;
        ProgressBarMiniumValue = 0;
        ProgressBarMaximumValue = 0;
        ProgressBarValue = 0;
        PckTreeView = null;
        OriginalTree = null;
        NotifyCanExecuteCommands();
    }

    private void ReportProgress(string msg) => ProgressMessage = msg;
    #endregion

    #region Properties

    [ObservableProperty] private Visibility _isCancelVisible = Visibility.Hidden;

    [ObservableProperty] private string? _selectedClientFolder = RegistrySettingsHelper.GetClientFolder();
    partial void OnSelectedClientFolderChanged(string? value)
    {
        if (value is not null) RegistrySettingsHelper.SetClientFolder(value);
        ReadFileListCommand.NotifyCanExecuteChanged();
        OpenUnpackDirectoryCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty] private string? _selectedFilesToPackFolder = RegistrySettingsHelper.GetFilesToPackFolder();
    partial void OnSelectedFilesToPackFolderChanged(string? value)
    {
        if (value is not null) RegistrySettingsHelper.SetFilesToPackFolder(value);
        PackFilesToPCKCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty] private string? _progressMessage;
    [ObservableProperty] private ObservableCollection<PCKFileNodeViewModel>? _pckTreeView;
    [ObservableProperty] private ObservableCollection<PCKFileNodeViewModel>? _originalTree;
    [ObservableProperty] private Visibility _isProgressBarVisible = Visibility.Hidden;
    [ObservableProperty] private Visibility _isProgressMessageVisible = Visibility.Visible;
    [ObservableProperty] private Visibility _isUnpackControlsVisible = Visibility.Collapsed;
    [ObservableProperty] private int _progressBarValue;
    [ObservableProperty] private int _progressBarMiniumValue;
    [ObservableProperty] private int _progressBarMaximumValue;
    [ObservableProperty] private bool _replaceUnpackFile;
    [ObservableProperty] private bool _isTextBoxEnabled = true;

    #endregion
}