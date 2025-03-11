using Microsoft.Win32;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.PCK;
using RHToolkit.Models.UISettings;

namespace RHToolkit.ViewModels.Pages
{
    public partial class PCKToolViewModel() : ObservableObject
    {
        private CancellationTokenSource? _cancellationTokenSource;

        #region Commands

        #region SelectFolderCommand

        [RelayCommand]
        private void SelectFolder()
        {
            OpenFolderDialog openFolderDialog = new();

            if (openFolderDialog.ShowDialog() == true)
            {
                SelectedFolder = openFolderDialog.FolderName;
            }
        }
        #endregion

        #region ReadFileListCommand

        private List<PCKFile>? listPckFile = null;

        [RelayCommand(CanExecute = nameof(CanExecuteReadFileListCommand))]
        private async Task ReadFileList()
        {
            if (SelectedFolder is null) return;

            if (!PCKReader.IsGameDirectory(SelectedFolder))
            {
                RHMessageBoxHelper.ShowOKMessage(Resources.PCKTool_SelectFolder, Resources.Error);
                return;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                ResetUI();
                IsCancelVisible = Visibility.Visible;

                listPckFile = await Task.Run(() => PCKReader.ReadPCKFileList(SelectedFolder, _cancellationTokenSource.Token), _cancellationTokenSource.Token);

                if (listPckFile.Count > 0)
                {
                    ProgressBarMiniumValue = 0;
                    ProgressBarMaximumValue = listPckFile.Count;
                    ProgressBarValue = 0;
                    IsProgressBarVisible = Visibility.Visible;

                    PckTreeView = [];
                    await CreateTreeView(listPckFile, PckTreeView, ReportProgress, _cancellationTokenSource.Token);
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
                _cancellationTokenSource = null;
                IsCancelVisible = Visibility.Hidden;
                IsUnpackControlsVisible = Visibility.Visible;
                CancelOperationCommand.NotifyCanExecuteChanged();
                ReadFileListCommand.NotifyCanExecuteChanged();
                UnpackPCKCommand.NotifyCanExecuteChanged();
                UnpackAllPCKCommand.NotifyCanExecuteChanged();
            }
        }

        private void ResetUI()
        {
            IsProgressBarVisible = Visibility.Hidden;
            IsUnpackControlsVisible = Visibility.Hidden;
            ProgressBarMiniumValue = 0;
            ProgressBarMaximumValue = 0;
            ProgressBarValue = 0;
            PckTreeView = null;
            CancelOperationCommand.NotifyCanExecuteChanged();
            ReadFileListCommand.NotifyCanExecuteChanged();
            UnpackPCKCommand.NotifyCanExecuteChanged();
            UnpackAllPCKCommand.NotifyCanExecuteChanged();
        }

        public async Task CreateTreeView(List<PCKFile> listPckFile, ObservableCollection<PCKFileNodeViewModel> treeNodes, Action<string> reportProgress, CancellationToken cancellationToken)
        {
            try
            {
                if (listPckFile.Count > 0)
                {
                    SortedDictionary<string, PCKFileNode>? listNode = PCKReader.ConvertListToNode(listPckFile);
                    reportProgress(Resources.PCKTool_ImportingTreeView);
                    await Task.Run(() => AddNodes(listNode, treeNodes, cancellationToken), cancellationToken);
                }

                reportProgress(string.Format(Resources.PCKTool_FileNumber, listPckFile.Count));
            }
            catch (OperationCanceledException)
            {
                ResetUI();
                throw;
            }
            catch (Exception ex)
            {
                reportProgress(ex.Message);
                throw;
            }
        }

        private void AddNodes(SortedDictionary<string, PCKFileNode> listNode, ObservableCollection<PCKFileNodeViewModel> treeNodes, CancellationToken cancellationToken)
        {
            foreach (var kv in listNode)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var node = new PCKFileNodeViewModel
                {
                    Name = kv.Value.IsDir
                        ? kv.Key
                        : string.Format("{0} [{1}] {2}", kv.Key, kv.Value.PCKFile.FileSize, kv.Value.PCKFile.Archive),
                    IsDir = kv.Value.IsDir,
                    PckFile = kv.Value.PCKFile,
                };

                if (kv.Value.Nodes != null && kv.Value.Nodes.Count > 0)
                {
                    AddNodes(kv.Value.Nodes, node.Children, cancellationToken);
                }

                if (!kv.Value.IsDir)
                {
                    ProgressBarValue++;
                }

                cancellationToken.ThrowIfCancellationRequested();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    treeNodes.Add(node);
                });
            }
        }

        #endregion

        #region UnpackPCKCommand
        [RelayCommand(CanExecute = nameof(CanExecuteUnpackCommand))]
        private async Task UnpackPCK()
        {
            try
            {
                await Unpack(false);
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteUnpackCommand))]
        private async Task UnpackAllPCK()
        {
            try
            {
                await Unpack(true);
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
            }
        }
        #endregion

        private List<PCKFile>? listPckFileWrite = null;

        private async Task Unpack(bool unpackAll = false)
        {
            if (SelectedFolder is null) return;

            if (!PCKReader.IsGameDirectory(SelectedFolder))
            {
                RHMessageBoxHelper.ShowOKMessage(Resources.PCKTool_SelectFolder, Resources.Error);
                return;
            }

            try
            {
                if (PckTreeView is null) return;
                listPckFileWrite = unpackAll ? listPckFile : GetCheckedFiles(PckTreeView);

                if (listPckFileWrite is not null && listPckFileWrite.Count == 0)
                {
                    RHMessageBoxHelper.ShowOKMessage(Resources.PCKTool_SelectFilesMessage, Resources.Info);
                    return;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                CancelOperationCommand.NotifyCanExecuteChanged();
                ReadFileListCommand.NotifyCanExecuteChanged();
                UnpackPCKCommand.NotifyCanExecuteChanged();
                UnpackAllPCKCommand.NotifyCanExecuteChanged();
                IsCancelVisible = Visibility.Visible;

                ReportProgress(Resources.PCKTool_Unpacking);
                await Task.Run(() => PCKReader.UnpackPCK(listPckFileWrite!, ReplaceUnpackFile, SelectedFolder, ReportProgress, UnpackProgress, _cancellationTokenSource.Token));
                ReportProgress(Resources.PCKTool_UnpackingComplete);
                await Task.Delay(3000);
                ReportProgress(string.Format(Resources.PCKTool_FileNumber, listPckFile?.Count));
            }
            catch (OperationCanceledException)
            {
                ReportProgress(Resources.PCKTool_CancelledMessage);
                await Task.Delay(3000);
                ReportProgress(string.Format(Resources.PCKTool_FileNumber, listPckFile?.Count));
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
                _cancellationTokenSource = null;
                IsCancelVisible = Visibility.Hidden;
                CancelOperationCommand.NotifyCanExecuteChanged();
                ReadFileListCommand.NotifyCanExecuteChanged();
                UnpackPCKCommand.NotifyCanExecuteChanged();
                UnpackAllPCKCommand.NotifyCanExecuteChanged();
            }
        }

        private static List<PCKFile> GetCheckedFiles(IEnumerable<PCKFileNodeViewModel> nodes)
        {
            var checkedFiles = new List<PCKFile>();

            foreach (var node in nodes)
            {
                if (node.IsChecked == true && !node.IsDir)
                {
                    checkedFiles.Add(node.PckFile!);
                }

                if (node.Children.Count > 0)
                {
                    checkedFiles.AddRange(GetCheckedFiles(node.Children));
                }
            }

            return checkedFiles;
        }

        private void UnpackProgress(int pos, int count)
        {
            if (ProgressBarMaximumValue != count) ProgressBarMaximumValue = count;
            ProgressBarValue = pos;
            ReportProgress(string.Format(Resources.PCKTool_UnpackingFiles, pos, count));
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCancelOperationCommand))]
        private void CancelOperation()
        {
            _cancellationTokenSource?.Cancel();
        }

        private bool CanExecuteCancelOperationCommand() => _cancellationTokenSource is not null;

        private void ReportProgress(string message)
        {
            ProgressMessage = message;
        }

        private bool CanExecuteReadFileListCommand()
        {
            return !string.IsNullOrWhiteSpace(SelectedFolder) && _cancellationTokenSource is null;
        }

        private bool CanExecuteUnpackCommand()
        {
            return PckTreeView is not null && _cancellationTokenSource is null;
        }

        #endregion

        #region Properties

        [ObservableProperty]
        private Visibility _isCancelVisible = Visibility.Hidden;

        [ObservableProperty]
        private string? _selectedFolder = RegistrySettingsHelper.GetClientFolder();
        partial void OnSelectedFolderChanged(string? value)
        {
            if (value is not null) RegistrySettingsHelper.SetClientFolder(value);
            ReadFileListCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private string? _progressMessage;

        [ObservableProperty]
        private ObservableCollection<PCKFileNodeViewModel>? _pckTreeView;

        [ObservableProperty]
        private Visibility _isProgressBarVisible = Visibility.Hidden;

        [ObservableProperty]
        private Visibility _isUnpackControlsVisible = Visibility.Hidden;

        [ObservableProperty]
        private int _progressBarValue;

        [ObservableProperty]
        private int _progressBarMiniumValue;

        [ObservableProperty]
        private int _progressBarMaximumValue;

        [ObservableProperty]
        private bool _replaceUnpackFile;

        #endregion
    }
}
