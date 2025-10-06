using HelixToolkit.Wpf.SharpDX;
using Microsoft.Win32;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.Model3D.MGM;
using RHToolkit.Models.Model3D.MMP;
using System.Windows.Media.Media3D;
using static RHToolkit.Models.Model3D.MMP.MMP;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;

namespace RHToolkit.Models.Model3D;

/// <summary>
/// Manages the Model View Window and its functionalities.
/// </summary>
public partial class ModelViewManager : ObservableObject
{
    private const string FileDialogFilter =
    "Rusty Hearts Models (*.MMP;*.MGM)|*.mmp;*.mgm|MMP Models (*.MMP)|*.mmp|MGM Models (*.MGM)|*.mgm|All Files (*.*)|*.*";

    #region File

    #region Load

    [RelayCommand]
    private async Task Load()
    {
        bool isLoaded = await LoadFile();

        if (isLoaded)
        {
            IsLoaded();
        }
    }
    /// <summary>
    /// Opens a file dialog to load a file and reads its contents.
    /// </summary>
    /// <returns>True if the file was loaded successfully, otherwise false.</returns>
    public async Task<bool> LoadFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = FileDialogFilter,
            FilterIndex = 1
        };

        if (dlg.ShowDialog() != true) return false;

        try
        {
            await CloseFile();

            CurrentFile = dlg.FileName;
            CurrentFileName = Path.GetFileName(dlg.FileName);

            await LoadModelAsync(dlg.FileName);

            OnCanExecuteFileCommandChanged();

            return true;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage(
                $"Error loading {CurrentFileName}: {ex.Message}\n {ex.StackTrace}", Resources.Error);
            ClearFile();
            return false;
        }
    }

    /// <summary>
    /// Updates the editor state to indicate that a file has been loaded.
    /// </summary>
    public void IsLoaded()
    {
        Title = $"3D Model Viewer '{CurrentFileName}'";
        IsMessageVisible = Visibility.Hidden;
        Message = string.Empty;
        IsVisible = Visibility.Visible;
        OnCanExecuteFileCommandChanged();
    }

    /// <summary>
    /// Updates the execution state of file commands.
    /// </summary>
    private void OnCanExecuteFileCommandChanged()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CloseFileCommand.NotifyCanExecuteChanged();
            ExportAsCommand.NotifyCanExecuteChanged();
            ImportFromFbxCommand.NotifyCanExecuteChanged();
        });
        
    }

    /// <summary>
    /// Determines if file commands can be executed.
    /// </summary>
    /// <returns>True if file commands can be executed, otherwise false.</returns>
    private bool CanExecuteFileCommand()
    {
        return MmpModel is not null;
    }
    #endregion

    #region Export FBX
    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task ExportAs()
    {
        if (MmpModel is null || CurrentFile is null) return;

        string fileName = Path.GetFileNameWithoutExtension(CurrentFile);

        var dlg = new SaveFileDialog
        {
            Filter = "Autodesk FBX (*.fbx)|*.fbx|All Files (*.*)|*.*",
            FilterIndex = 1,
            FileName = fileName + ".fbx"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                await Task.Run(() => MMPExporterAspose.ExportMmpToFbx(MmpModel, dlg.FileName));
                RHMessageBoxHelper.ShowOKMessage($"Exported MMP → FBX:\n{dlg.FileName}", "FBX Exporter");
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"Error exporting file: {ex.Message}", Resources.Error);
            }
        }
    }

    #endregion

    #region Export FBX to MMP
    [RelayCommand]
    public static async Task ImportFromFbx()
    {
        var ofd = new OpenFileDialog
        {
            Filter = "Autodesk FBX (*.fbx)|*.fbx|All Files (*.*)|*.*",
            FilterIndex = 1,
            Multiselect = false
        };

        if (ofd.ShowDialog() != true) return;

        var fbxPath = ofd.FileName;
        var dir = Path.GetDirectoryName(fbxPath)!;
        var baseName = Path.GetFileNameWithoutExtension(fbxPath);

        // Find the original MMP with same name in the same folder
        var mmpPath = Path.Combine(dir, baseName + ".mmp");
        if (!File.Exists(mmpPath))
        {
            RHMessageBoxHelper.ShowOKMessage(
                $"Could not find the original MMP next to the FBX:\n{mmpPath}",
                "Import FBX");
            return;
        }

        // Ask where to save the rebuilt MMP (default: same folder, same name + _new)
        var sfd = new SaveFileDialog
        {
            Filter = "Rusty Hearts Map File (*.mmp)|*.mmp|All Files (*.*)|*.*",
            FilterIndex = 1,
            FileName = baseName + "_new.mmp",
            InitialDirectory = dir
        };
        if (sfd.ShowDialog() != true) return;

        var outMmpPath = sfd.FileName;

        try
        {
            await Task.Run(() =>
            {
                MMPWriter.RebuildFromFbx(mmpPath, fbxPath, outMmpPath);
            });

            RHMessageBoxHelper.ShowOKMessage($"Exported FBX → MMP:\n{outMmpPath}", "MMP Exporter");
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Export failed: {ex.Message}", Resources.Error);
        }
    }

    #endregion

    #region Model View

    [ObservableProperty]
    private ObservableElement3DCollection _Scene3D = [];

    [ObservableProperty]
    private IEffectsManager _EffectsManager = new DefaultEffectsManager();

    private async Task LoadModelAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is null or empty.", nameof(filePath));

        var ext = Path.GetExtension(filePath)?.ToLowerInvariant();

        switch (ext)
        {
            case ".mgm":
                {
                    ClearFile();
                    throw new NotImplementedException($"MGM IS NOT SUPPORTED YET!!!");
                }
            //case ".mgm":
            //    {
            //        Message = "Loading MGM model...";
            //        var mgm = await MGMReader.ReadAsync(filePath).ConfigureAwait(false);
            //        MgmModel = mgm;
            //        MmpModel = null;
            //        break;
            //    }
            case ".mmp":
                {
                    Message = "Loading MMP model...";
                    var mmp = await MMPReader.ReadAsync(filePath).ConfigureAwait(false);
                    if (mmp.Version < 6)
                        throw new NotSupportedException($"MMP version '{mmp.Version}' is not supported.");
                    MmpModel = mmp;
                    MgmModel = null;
                    break;
                }
            default:
                throw new NotSupportedException($"Unsupported file extension: {ext}");
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Scene3D.Clear();
            Camera = CreateDefaultCamera();

            if (MmpModel is not null)
            {
                Version = MmpModel.Version;
                foreach (var node in MMPToHelix.CreateMMPNodes(MmpModel))
                    Scene3D.Add(node);
            }
            else if (MgmModel is not null)
            {
                Version = MgmModel.Version;
                foreach (var node in MGMToHelix.CreateMGMNodes(MgmModel))
                    Scene3D.Add(node);
            }
        });
    }

    private static PerspectiveCamera CreateDefaultCamera() => new()
    {
        Position = new Point3D(0, 2000, 0),
        LookDirection = new Vector3D(-5, -12, -5),
        UpDirection = new Vector3D(0, 1, 0),
        NearPlaneDistance = 0.5,
        FarPlaneDistance = 500000
    };

    [ObservableProperty]
    private PerspectiveCamera _Camera = new()
    {
        Position = new Point3D(0, 2000, 0),
        LookDirection = new Vector3D(-5, -12, -5),
        UpDirection = new Vector3D(0, 1, 0),
        NearPlaneDistance = 0.5,
        FarPlaneDistance = 500000
    };

    #endregion

    #region Close File
    /// <summary>
    /// Closes the current file, prompting the user to save changes if necessary.
    /// </summary>
    /// <returns>True if the file was closed successfully, otherwise false.</returns>
    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task <bool> CloseFile()
    {
        await Task.Run(() =>
        {
            ClearFile();
        });
        return true;
    }

    /// <summary>
    /// Clears the current file and resets the Wdata and related properties.
    /// </summary>
    public void ClearFile()
    {
        MmpModel = null;
        MgmModel = null;
        CurrentFile = null;
        CurrentFileName = null;
        IsVisible = Visibility.Hidden;
        Message = Resources.OpenFile;
        IsMessageVisible = Visibility.Visible;
        Title = "3D Model Viewer";
        OnCanExecuteFileCommandChanged();
    }
    #endregion

    #region Close Window

    /// <summary>
    /// Closes the specified window.
    /// </summary>
    /// <param name="window">The window to close.</param>
    [RelayCommand]
    public static void CloseWindow(Window window)
    {
        try
        {
            window?.Close();
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }
    #endregion

    #endregion

    #region Properties

    [ObservableProperty]
    private string _title = "Model Viewer";

    [ObservableProperty]
    private Visibility _isVisible = Visibility.Hidden;

    [ObservableProperty]
    private Visibility _isMessageVisible = Visibility.Visible;

    [ObservableProperty]
    private MmpModel? _mmpModel;
    partial void OnMmpModelChanged(MmpModel? value)
    {
        OnCanExecuteFileCommandChanged();
    }
    [ObservableProperty]
    private MgmModel? _mgmModel;
    partial void OnMgmModelChanged(MgmModel? value)
    {
        OnCanExecuteFileCommandChanged();
    }

    [ObservableProperty]
    private string? _currentFile;

    [ObservableProperty]
    private string? _currentFileName;

    [ObservableProperty]
    private string? _message = Resources.OpenFile;

    [ObservableProperty]
    private int _version;

    #endregion
}