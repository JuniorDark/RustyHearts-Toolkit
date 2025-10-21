using HelixToolkit.Wpf.SharpDX;
using Microsoft.Win32;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.Model3D.Map;
using RHToolkit.Models.Model3D.MGM;
using System.Numerics;
using System.Windows.Media.Media3D;
using static RHToolkit.Models.Model3D.Map.MMP;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;

namespace RHToolkit.Models.Model3D;

/// <summary>
/// Manages the Model View Window and its functionalities.
/// </summary>
public partial class ModelViewManager : ObservableObject
{
    private const string FileDialogFilter =
    "Rusty Hearts Models (*.MMP;*.MGM;*.NAVI)|*.mmp;*.mgm;*.navi|Map Models (*.MMP)|*.mmp|MGM Models (*.MGM)|*.mgm|Navigation Mesh (*.NAVI)|*.navi|All Files (*.*)|*.*";

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
                $"Error loading {CurrentFileName}: {ex.Message}", Resources.Error);
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
        return MmpModel is not null || NaviModel is not null;
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
                var exporter = new MMPExporterAspose();
                await exporter.ExportMmpToFbx(MmpModel, dlg.FileName);
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
            await MMPWriter.RebuildFromFbx(mmpPath, fbxPath, outMmpPath);

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

    private static PerspectiveCamera CreateDefaultCamera() => new()
    {
        Position = new Point3D(0, 140, -200),
        LookDirection = new Vector3D(-10, -243, 1250),
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

    [ObservableProperty]
    private IEffectsManager _EffectsManager = new DefaultEffectsManager();

    private Element3D? _wireframeModel;        // LineGeometryModel3D
    private Element3D? _naviDebugGroup;        // GroupModel3D (portals + labels)
    private IReadOnlyDictionary<uint, Matrix4x4>? _worldByHash;

    private async Task LoadModelAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is null or empty.", nameof(filePath));

        var ext = Path.GetExtension(filePath)?.ToLowerInvariant();

        switch (ext)
        {
            case ".height":
                {
                    Message = "Loading model...";
                    var height = await HeightReader.ReadAsync(filePath).ConfigureAwait(false);
                    MgmModel = null;
                    MmpModel = null;
                    NaviModel = null;
                    break;
                }
            case ".mgm":
                {
                    Message = "Loading MGM model...";
                    var mgm = await MGMReader.ReadAsync(filePath).ConfigureAwait(false);
                    MgmModel = mgm;
                    MmpModel = null;
                    NaviModel = null;
                    break;
                }
            case ".mmp":
                {
                    Message = "Loading MMP model...";
                    var mmp = await MMPReader.ReadAsync(filePath).ConfigureAwait(false);
                    if (mmp.Version < 6)
                        throw new NotSupportedException($"MMP version '{mmp.Version}' is not supported.");
                    MmpModel = mmp;
                    var naviPath = Path.ChangeExtension(filePath, ".navi");
                    if (File.Exists(naviPath))
                    {
                        NaviModel = await NaviReader.ReadAsync(naviPath).ConfigureAwait(false);
                    }
                    MgmModel = null;
                    break;
                }
            case ".navi":
                {
                    Message = "Loading NAVI model...";
                    var navi = await NaviReader.ReadAsync(filePath).ConfigureAwait(false);
                    //var heightPath = Path.ChangeExtension(filePath, ".height");
                    //await HeightWriter.BuildFromNaviFileAsync(filePath, heightPath, 20, 50, 2);
                    NaviModel = navi;
                    MgmModel = null;
                    MmpModel = null;
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
                LoadNavMesh();
            }
            else if (MgmModel is not null)
            {
                IsNavMeshControlsVisible = Visibility.Hidden;
                Version = MgmModel.Version;
                foreach (var node in MGMToHelix.CreateMGMNodes(MgmModel))
                    Scene3D.Add(node);
            }
            else if (NaviModel is not null)
            {
                Version = NaviModel.Header.Version;
                LoadNavMesh();
            }
        });
    }

    #region NavMesh
    private void LoadNavMesh()
    {
        if (NaviModel is not null)
        {
            IsNavMeshControlsVisible = Visibility.Visible;
            _wireframeModel = null;
            _naviDebugGroup = null;
            float alpha;

            if (MmpModel is not null)
            {
                alpha = 0.0f;
                _worldByHash = MmpModel.Nodes
                   .GroupBy(n => n.NameHash)
                   .ToDictionary(g => g.Key, g => g.First().MWorld);
            }
            else
            {
                alpha = 0.25f;
                _worldByHash = NaviModel.Nodes
                    .GroupBy(n => n.NameKey)
                    .ToDictionary(g => g.Key, g => g.First().MWorld);
            }

            // base mesh(es)
            foreach (var meshGroup in NaviToHelix.CreateNaviNodes(NaviModel, _worldByHash, alpha))
                Scene3D.Add(meshGroup);

            // apply current toggles
            EnsureWireframe();
            EnsureNaviDebug();
            ApplyWireframeVisibility(ShowWireframe);
            ApplyNaviDebugVisibility(ShowNaviDebug);
        }
    }

    // ===== builders =====
    private void EnsureWireframe()
    {
        if (_wireframeModel != null || NaviModel is null) return;
        _wireframeModel = NaviToHelix.CreateNavTrianglesWireframe(
            NaviModel, _worldByHash
        );
    }

    private void EnsureNaviDebug()
    {
        if (_naviDebugGroup != null || NaviModel is null) return;
        _naviDebugGroup = NaviToHelix.CreateNavOverlay(
            NaviModel, _worldByHash
        );
    }

    // ===== add/remove helpers =====
    private void ApplyWireframeVisibility(bool visible)
    {
        if (_wireframeModel == null) return;
        if (visible && !Scene3D.Contains(_wireframeModel)) Scene3D.Add(_wireframeModel);
        if (!visible && Scene3D.Contains(_wireframeModel)) Scene3D.Remove(_wireframeModel);
    }

    private void ApplyNaviDebugVisibility(bool visible)
    {
        if (_naviDebugGroup == null) return;
        if (visible && !Scene3D.Contains(_naviDebugGroup)) Scene3D.Add(_naviDebugGroup);
        if (!visible && Scene3D.Contains(_naviDebugGroup)) Scene3D.Remove(_naviDebugGroup);
    }
    #endregion

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
    private Visibility _isNavMeshControlsVisible = Visibility.Hidden;

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
    private NaviMeshFile? _naviModel;
    partial void OnNaviModelChanged(NaviMeshFile? value)
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

    [ObservableProperty]
    private bool _showWireframe;
    partial void OnShowWireframeChanged(bool value)
    {
        EnsureWireframe();
        ApplyWireframeVisibility(value);
    }
    [ObservableProperty]
    private bool _showNaviDebug;
    partial void OnShowNaviDebugChanged(bool value)
    {
        EnsureNaviDebug();
        ApplyNaviDebugVisibility(value);
    }

    #endregion
}