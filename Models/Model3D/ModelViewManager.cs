using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using Microsoft.Win32;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.Model3D.Animation;
using RHToolkit.Models.Model3D.Map;
using RHToolkit.Models.Model3D.MGM;
using System.Numerics;
using System.Windows.Media.Media3D;
using static RHToolkit.Models.Model3D.Map.MMP;
using static RHToolkit.Models.Model3D.MGM.MGMToHelix;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;
namespace RHToolkit.Models.Model3D;

/// <summary>
/// Manages the Model View Window and its functionalities.
/// </summary>
public partial class ModelViewManager : ObservableObject
{
    private const string FileDialogFilter =
    "Rusty Hearts Models (*.MMP;*.MGM;*.NAVI;*.MA)|*.mmp;*.mgm;*.navi;*.ma|Map Models (*.MMP)|*.mmp|MGM Models (*.MGM)|*.mgm|Navigation Mesh (*.NAVI)|*.navi|All Files (*.*)|*.*";

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
        return MmpModel is not null || MgmModel is not null || NaviModel is not null;
    }
    #endregion

    #region Export FBX
    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task ExportAs()
    {
        if (CurrentFile is null) return;

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
                if (MgmModel is not null)
                {
                    await MGMExporter.ExportMgmToFbx(MgmModel, dlg.FileName, EmbedTextures, ExportAnimation);
                    RHMessageBoxHelper.ShowOKMessage($"Exported MGM → FBX:\n{dlg.FileName}", "FBX Exporter");
                }
                else if (MmpModel is  not null)
                {
                    await MMPExporter.ExportMmpToFbx(MmpModel, dlg.FileName, EmbedTextures);
                    RHMessageBoxHelper.ShowOKMessage($"Exported MMP → FBX:\n{dlg.FileName}", "FBX Exporter");
                }
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

    private readonly List<MeshGeometryModel3D> _meshModels = [];
    private Element3D? _naviWireframeModel;        // LineGeometryModel3D
    private Element3D? _boneModel;        // LineGeometryModel3D
    private Element3D? _naviDebugGroup;        // GroupModel3D (portals + labels)
    private IReadOnlyDictionary<uint, Matrix4x4>? _worldByHash;

    private async Task LoadModelAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is null or empty.", nameof(filePath));

        var ext = Path.GetExtension(filePath)?.ToLowerInvariant();

        switch (ext)
        {
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
            ResetMeshCache();
            Camera = CreateDefaultCamera();

            if (MmpModel is not null)
            {
                Version = MmpModel.Version;
                ObjectCount = MmpModel.Objects.Count;
                MaterialCount = MmpModel.Materials.Count;
                BoneCount = 0;

                foreach (var node in MMPToHelix.CreateMMPNodes(MmpModel))
                {
                    Scene3D.Add(node);
                    CollectMeshesFrom(node);
                }

                LoadNavMesh();
                ApplyMeshWireframe(ShowMeshWireframe);
            }
            else if (MgmModel is not null)
            {
                MGMReader.ValidateMgmModel(MgmModel);

                IsNavMeshControlsVisible = Visibility.Collapsed;
                Version = MgmModel.Version;
                ObjectCount = MgmModel.Meshes.Count;
                MaterialCount = MgmModel.Materials.Count;
                BoneCount = MgmModel.Bones.Count;

                // Base MGM meshes
                foreach (var node in MGMToHelix.CreateMGMNodes(MgmModel))
                {
                    Scene3D.Add(node);
                    CollectMeshesFrom(node);
                }

                _boneModel = null;
                EnsureBones();
                ApplyBoneVisibility(ShowBones);

                ApplyMeshWireframe(ShowMeshWireframe);
                ApplyMeshSolidVisibility(ShowMeshSolid);
            }
            else if (NaviModel is not null)
            {
                Version = NaviModel.Header.Version;
                LoadNavMesh();
            }
            FrameScene(includeBones: true);
        });
    }

    #region NavMesh
    private void LoadNavMesh()
    {
        if (NaviModel is not null)
        {
            IsNavMeshControlsVisible = Visibility.Visible;
            _naviWireframeModel = null;
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
                    .GroupBy(n => n.NameHash)
                    .ToDictionary(g => g.Key, g => g.First().MWorld);
            }

            // base mesh(es)
            foreach (var meshGroup in NaviToHelix.CreateNaviNodes(NaviModel, _worldByHash, alpha))
                Scene3D.Add(meshGroup);

            // apply current toggles
            EnsureNaviWireframe();
            EnsureNaviDebug();
            ApplyNaviWireframeVisibility(ShowNaviWireframe);
            ApplyNaviDebugVisibility(ShowNaviDebug);
        }
    }

    // ===== builders =====
    private void EnsureNaviWireframe()
    {
        if (_naviWireframeModel != null || NaviModel is null) return;
        _naviWireframeModel = NaviToHelix.CreateNavTrianglesWireframe(
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
    private void ApplyNaviWireframeVisibility(bool visible)
    {
        if (_naviWireframeModel == null) return;
        if (visible && !Scene3D.Contains(_naviWireframeModel)) Scene3D.Add(_naviWireframeModel);
        if (!visible && Scene3D.Contains(_naviWireframeModel)) Scene3D.Remove(_naviWireframeModel);
    }

    private void ApplyNaviDebugVisibility(bool visible)
    {
        if (_naviDebugGroup == null) return;
        if (visible && !Scene3D.Contains(_naviDebugGroup)) Scene3D.Add(_naviDebugGroup);
        if (!visible && Scene3D.Contains(_naviDebugGroup)) Scene3D.Remove(_naviDebugGroup);
    }
    #endregion

    #region Bones
    private void EnsureBones()
    {
        if (_boneModel != null || MgmModel is null) return;

        var skel = new SkeletonOptions
        {
            BoneRadius = 0.4,
            ShowJoints = true,
            JointRadius = 0.35
        };

        _boneModel = CreateSkeletonModel(MgmModel, skel);
    }

    private void ApplyBoneVisibility(bool visible)
    {
        if (_boneModel == null) return;
        if (visible && !Scene3D.Contains(_boneModel)) Scene3D.Add(_boneModel);
        if (!visible && Scene3D.Contains(_boneModel)) Scene3D.Remove(_boneModel);
    }
    #endregion

    #region Mesh Wireframe
    private void CollectMeshesFrom(Element3D e)
    {
        switch (e)
        {
            case MeshGeometryModel3D m:
                _meshModels.Add(m);
                if (!_origFrontMat.ContainsKey(m)) _origFrontMat[m] = m.Material!;
                break;

            case GroupModel3D g:
                foreach (var child in g.Children)
                    CollectMeshesFrom(child);
                break;
        }
    }

    private void ResetMeshCache()
    {
        _meshModels.Clear();
        _origFrontMat.Clear();
        _origBackMat.Clear();
    }

    private void ApplyMeshWireframe(bool wireframe)
    {
        foreach (var m in _meshModels)
        {
            m.RenderWireframe = wireframe;
            m.WireframeColor = System.Windows.Media.Colors.Black;
        }
    }

    private readonly Dictionary<MeshGeometryModel3D, HelixToolkit.Wpf.SharpDX.Material> _origFrontMat = [];
    private readonly Dictionary<MeshGeometryModel3D, HelixToolkit.Wpf.SharpDX.Material> _origBackMat = [];

    private HelixToolkit.Wpf.SharpDX.Material? _invisibleMat;

    private HelixToolkit.Wpf.SharpDX.Material GetInvisibleMaterial()
    {
        return _invisibleMat ??= new PhongMaterial
        {
            DiffuseColor = new Color4(0, 0, 0, 0),
            AmbientColor = new Color4(0, 0, 0, 0),
            SpecularColor = new Color4(0, 0, 0, 0),
            EmissiveColor = new Color4(0, 0, 0, 0),
            ReflectiveColor = new Color4(0, 0, 0, 0)
        };
    }

    private void ApplyMeshSolidVisibility(bool showSolid)
    {
        var inv = GetInvisibleMaterial();

        foreach (var m in _meshModels)
        {
            if (showSolid)
            {
                if (_origFrontMat.TryGetValue(m, out var f)) m.Material = f;
            }
            else
            {
                m.Material = inv;
            }
        }
    }

    #endregion

    #region Camera
    [RelayCommand]
    private void ZoomExtents()
    {
        FrameScene(includeBones: ShowBones);
    }

    // Compute the scene bounds from the actual mesh vertex positions (respecting Transform)
    private bool TryGetSceneBounds(out Rect3D bounds, bool includeBones = false)
    {
        Rect3D acc = Rect3D.Empty;

        void Accumulate(Element3D e, Transform3D? parentT = null)
        {
            var t = Combine(parentT, (e as Element3D)?.Transform);

            switch (e)
            {
                case MeshGeometryModel3D m when m.Geometry is MeshGeometry3D g && g.Positions != null && g.Positions.Count > 0:
                    foreach (var v in g.Positions)
                    {
                        var p = new Point3D(v.X, v.Y, v.Z);
                        if (t != null) p = t.Transform(p);
                        if (double.IsNaN(p.X) || double.IsInfinity(p.X) ||
                            double.IsNaN(p.Y) || double.IsInfinity(p.Y) ||
                            double.IsNaN(p.Z) || double.IsInfinity(p.Z))
                            continue;

                        if (acc.IsEmpty) acc = new Rect3D(p, new Size3D(0, 0, 0));
                        else acc.Union(p);
                    }
                    break;

                case GroupModel3D g:
                    foreach (var child in g.Children)
                        Accumulate(child, t);
                    break;

                default:
                    if (includeBones && e is MeshGeometryModel3D bm && bm.Geometry is MeshGeometry3D bg && bg.Positions != null)
                    {
                        foreach (var v in bg.Positions)
                        {
                            var p = new Point3D(v.X, v.Y, v.Z);
                            if (t != null) p = t.Transform(p);
                            if (double.IsNaN(p.X) || double.IsInfinity(p.X) ||
                                double.IsNaN(p.Y) || double.IsInfinity(p.Y) ||
                                double.IsNaN(p.Z) || double.IsInfinity(p.Z))
                                continue;

                            if (acc.IsEmpty) acc = new Rect3D(p, new Size3D(0, 0, 0));
                            else acc.Union(p);
                        }
                    }
                    break;
            }
        }

        foreach (var e in Scene3D)
            Accumulate(e);

        bounds = acc;
        return !acc.IsEmpty && acc.SizeX > 0 && acc.SizeY > 0 && acc.SizeZ > 0;

        static Transform3D? Combine(Transform3D? a, Transform3D? b)
        {
            if (a == null) return b;
            if (b == null) return a;
            return new Transform3DGroup { Children = [a, b] };
        }
    }

    // Given bounds, place the PerspectiveCamera so the whole box fits.
    // Keeps a nice diagonal view; adjusts Near/Far and accounts for FOV.
    private void FrameCameraToBounds(Rect3D b, double fovDegrees = 45.0, double padding = 1.0)
    {
        if (b.IsEmpty) return;

        // center and radius
        var center = new Point3D(b.X + b.SizeX / 2.0, b.Y + b.SizeY / 2.0, b.Z + b.SizeZ / 2.0);
        var diag = Math.Sqrt(b.SizeX * b.SizeX + b.SizeY * b.SizeY + b.SizeZ * b.SizeZ);
        var radius = diag * 0.5;

        // keep current azimuth/elevation if you like; or pick a default diagonal
        var dir = Camera?.LookDirection ?? new Vector3D(-1, -0.6, -1);
        if (dir.LengthSquared == 0) dir = new Vector3D(-1, -0.6, -1);
        dir.Normalize();

        // FOV
        var fov = (Camera?.FieldOfView > 0) ? Camera.FieldOfView : fovDegrees;
        var fovRad = fov * Math.PI / 180.0;

        // distance so sphere fits vertically
        var distance = radius / Math.Tan(fovRad * 0.5) * padding;

        // place the camera and make it FACE the center
        var pos = center - dir * distance;
        Camera!.Position = pos;
        Camera.LookDirection = center - pos;   // <— always face the model
        Camera.UpDirection = new Vector3D(0, 1, 0);

        // clip planes suitable for this framing
        SetClipPlanes(center, radius);
    }

    private void SetClipPlanes(Point3D focus, double sceneRadius)
    {
        // distance from camera to the focus (scene center)
        var d = (focus - Camera.Position).Length;
        if (d <= 0) d = sceneRadius > 0 ? sceneRadius : 1.0;

        // near is a small fraction of the distance, clamped to keep precision
        var near = Math.Max(0.01, Math.Min(d * 0.02, 5.0));   // 2% of distance, max 5 units
                                                              // far should comfortably exceed both distance and scene size
        var far = Math.Max(d + sceneRadius * 5.0, near + 50.0);

        Camera.NearPlaneDistance = near;
        Camera.FarPlaneDistance = far;
    }

    // Convenience: frame the current Scene3D
    public void FrameScene(bool includeBones = false)
    {
        if (TryGetSceneBounds(out var bounds, includeBones))
            FrameCameraToBounds(bounds);
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
        NaviModel = null;
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
    private Visibility _isVisible = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _isMessageVisible = Visibility.Visible;

    [ObservableProperty]
    private Visibility _isNavMeshControlsVisible = Visibility.Collapsed;

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

    [ObservableProperty] private int _version;
    [ObservableProperty] private int _objectCount;
    [ObservableProperty] private int _materialCount;
    [ObservableProperty] private int _boneCount;

    [ObservableProperty]
    private bool _showNaviWireframe;
    partial void OnShowNaviWireframeChanged(bool value)
    {
        EnsureNaviWireframe();
        ApplyNaviWireframeVisibility(value);
    }
    [ObservableProperty]
    private bool _showNaviDebug;
    partial void OnShowNaviDebugChanged(bool value)
    {
        EnsureNaviDebug();
        ApplyNaviDebugVisibility(value);
    }

    [ObservableProperty] private bool _showBones;
    partial void OnShowBonesChanged(bool value)
    {
        EnsureBones();
        ApplyBoneVisibility(value);
    }

    [ObservableProperty]
    private bool _showMeshWireframe;

    partial void OnShowMeshWireframeChanged(bool value)
    {
        ApplyMeshWireframe(value);
    }

    [ObservableProperty]
    private bool _showMeshSolid = true;

    partial void OnShowMeshSolidChanged(bool value)
    {
        ApplyMeshSolidVisibility(value);
    }
    [ObservableProperty] private bool _embedTextures;
    [ObservableProperty] private bool _exportAnimation = false;
    #endregion
}