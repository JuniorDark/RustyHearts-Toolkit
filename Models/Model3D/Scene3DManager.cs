using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using RHToolkit.Models.Model3D.Map;
using RHToolkit.Models.Model3D.MGM;
using RHToolkit.Models.Model3D.Model;
using RHToolkit.Models.WDATA;
using RHToolkit.Services;
using System.Collections.Specialized;
using System.Numerics;
using System.Windows.Media.Media3D;
using static RHToolkit.Models.Model3D.Map.MMP;
using static RHToolkit.Models.Model3D.MGM.MGMToHelix;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;

namespace RHToolkit.Models.Model3D;

public partial class Scene3DManager : ObservableObject
{
    [ObservableProperty]
    private IEffectsManager _EffectsManager = new DefaultEffectsManager();

    [ObservableProperty]
    private ObservableElement3DCollection _Scene3D = [];

    [ObservableProperty]
    private PerspectiveCamera _Camera = new()
    {
        Position = new Point3D(0, 140, -200),
        LookDirection = new Vector3D(-10, -243, 1250),
        UpDirection = new Vector3D(0, 1, 0),
        NearPlaneDistance = 2,
        FarPlaneDistance = 500000
    };

    public static PerspectiveCamera CreateDefaultCamera() => new()
    {
        Position = new Point3D(0, 140, -200),
        LookDirection = new Vector3D(-10, -243, 1250),
        UpDirection = new Vector3D(0, 1, 0),
        NearPlaneDistance = 2,
        FarPlaneDistance = 500000
    };

    #region Camera

    // Movement speed for WASD controls
    private double _moveSpeed = 100.0;
    public double MoveSpeed
    {
        get => _moveSpeed;
        set => SetProperty(ref _moveSpeed, value);
    }

    private void MoveCamera(Vector3D direction, double speedMultiplier = 1.0)
    {
        direction.Normalize();
        var speed = MoveSpeed * speedMultiplier;
        Camera.Position = new Point3D(
            Camera.Position.X + direction.X * speed,
            Camera.Position.Y + direction.Y * speed,
            Camera.Position.Z + direction.Z * speed);
    }

    [RelayCommand]
    public void MoveForward() => MoveCamera(Camera.LookDirection);

    [RelayCommand]
    public void MoveForwardFast() => MoveCamera(Camera.LookDirection, 2.0);

    [RelayCommand]
    public void MoveBackward() => MoveCamera(-Camera.LookDirection);

    [RelayCommand]
    public void MoveBackwardFast() => MoveCamera(-Camera.LookDirection, 2.0);

    [RelayCommand]
    public void MoveLeft()
    {
        var right = Vector3D.CrossProduct(Camera.LookDirection, Camera.UpDirection);
        MoveCamera(-right);
    }

    [RelayCommand]
    public void MoveLeftFast()
    {
        var right = Vector3D.CrossProduct(Camera.LookDirection, Camera.UpDirection);
        MoveCamera(-right, 2.0);
    }

    [RelayCommand]
    public void MoveRight()
    {
        var right = Vector3D.CrossProduct(Camera.LookDirection, Camera.UpDirection);
        MoveCamera(right);
    }

    [RelayCommand]
    public void MoveRightFast()
    {
        var right = Vector3D.CrossProduct(Camera.LookDirection, Camera.UpDirection);
        MoveCamera(right, 2.0);
    }

    [RelayCommand]
    public void MoveUp() => MoveCamera(Camera.UpDirection);

    [RelayCommand]
    public void MoveUpFast() => MoveCamera(Camera.UpDirection, 2.0);

    [RelayCommand]
    public void MoveDown() => MoveCamera(-Camera.UpDirection);

    [RelayCommand]
    public void MoveDownFast() => MoveCamera(-Camera.UpDirection, 2.0);

    [RelayCommand]
    public void ZoomExtents(bool showBones)
    {
        FrameScene(includeBones: showBones);
    }

    [RelayCommand]
    public void ResetCameraPosition()
    {
        Camera = CreateDefaultCamera();
        FrameScene(includeBones: true);
    }

    public void ClearScene()
    {
        Scene3D.Clear();
        ResetMeshCache();
        Camera = CreateDefaultCamera();
        MmpModel = null;
        MgmModel = null;
        NaviModel = null;
    }

    /// <summary>
    /// Try to compute the scene bounds from the actual mesh vertex positions (respecting Transform)
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="includeBones"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Frame the camera to fit the given bounds
    /// </summary>
    /// <param name="b"></param>
    /// <param name="fovDegrees"></param>
    /// <param name="padding"></param>
    private void FrameCameraToBounds(Rect3D b, double fovDegrees = 45.0, double padding = 1.0)
    {
        if (b.IsEmpty) return;

        // center and radius
        var center = new Point3D(b.X + b.SizeX / 2.0, b.Y + b.SizeY / 2.0, b.Z + b.SizeZ / 2.0);
        var diag = Math.Sqrt(b.SizeX * b.SizeX + b.SizeY * b.SizeY + b.SizeZ * b.SizeZ);
        var radius = diag * 0.5;

        // look direction
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

    /// <summary>
    /// Set near and far clip planes based on focus point and scene radius
    /// </summary>
    /// <param name="focus"></param>
    /// <param name="sceneRadius"></param>
    private void SetClipPlanes(Point3D focus, double sceneRadius)
    {
        // distance from camera to the focus (scene center)
        var d = (focus - Camera.Position).Length;
        if (d <= 0) d = sceneRadius > 0 ? sceneRadius : 1.0;

        // near is a small fraction of the distance, clamped to keep precision
        var near = Math.Max(0.01, Math.Min(d * 0.02, 5.0));   // 2% of distance, max 5 units
        var far = Math.Max(d + sceneRadius * 5.0, near + 50.0);

        Camera.NearPlaneDistance = near;
        Camera.FarPlaneDistance = far;
    }

    /// <summary>
    /// Frame the camera to fit the entire scene
    /// </summary>
    /// <param name="includeBones"></param>
    public void FrameScene(bool includeBones = false)
    {
        if (TryGetSceneBounds(out var bounds, includeBones))
            FrameCameraToBounds(bounds);
    }
    #endregion

    #region MMP Overlays

    private readonly ObbOverlayManager<EventBox> _eventOverlay = new("EventBoxesRoot", PhongMaterials.Orange);
    private readonly ObbOverlayManager<AniBG> _aniOverlay = new("AniBGRoot", PhongMaterials.Yellow);
    private readonly ObbOverlayManager<Gimmick> _gimmickOverlay = new("GimmickRoot", PhongMaterials.Blue);
    private readonly ObbOverlayManager<ItemBox> _itemOverlay = new("ItemBoxesRoot", PhongMaterials.Green);
    private readonly ObbOverlayManager<NpcBox> _npcOverlay = new("NpcBoxRoot", PhongMaterials.Red);

    // Cached service reference for NPC model path resolution
    private IGMDatabaseService? _gmDatabaseService;
    private string _npcClientAssetsFolder = string.Empty;

    public void AttachOverlays(WData? wData, string clientAssetsFolder = "", string mmpDirectory = "")
    {
        // Get the GMDatabaseService from App.Services for NPC model resolution
        _gmDatabaseService = App.Services.GetService<IGMDatabaseService>();
        _npcClientAssetsFolder = clientAssetsFolder;

        _eventOverlay.AttachRoot(Scene3D);
        _aniOverlay.AttachRoot(Scene3D);
        _gimmickOverlay.AttachRoot(Scene3D);
        _itemOverlay.AttachRoot(Scene3D);
        _npcOverlay.AttachRoot(Scene3D);

        // Set model path context for overlays that support models
        _aniOverlay.SetModelPathContext(clientAssetsFolder, mmpDirectory);
        _gimmickOverlay.SetModelPathContext(clientAssetsFolder, mmpDirectory);
        _itemOverlay.SetModelPathContext(clientAssetsFolder, mmpDirectory);
        
        // Configure NPC overlay with custom resolver for NpcName property
        _npcOverlay.SetModelPathContext(clientAssetsFolder);
        _npcOverlay.SetCustomModelPathResolver(ResolveNpcModelPath, nameof(NpcBox.NpcName));

        // clear any previous bindings
        _eventOverlay.ClearAll();
        _aniOverlay.ClearAll();
        _gimmickOverlay.ClearAll();
        _itemOverlay.ClearAll();
        _npcOverlay.ClearAll();

        if (wData?.AniBGs is not null) _aniOverlay.BindCollection(wData.AniBGs);
        if (wData?.Gimmicks is not null) _gimmickOverlay.BindCollection(wData.Gimmicks);
        if (wData?.ItemBoxes is not null) _itemOverlay.BindCollection(wData.ItemBoxes);

        if (wData?.EventBoxGroups is not null)
        {
            foreach (var g in wData.EventBoxGroups)
            {
                if (g.Boxes is not null)
                {
                    // Bind NpcBox entities to NPC overlay, others to event overlay
                    if (g.Type == EventBoxType.NpcBox)
                    {
                        _npcOverlay.BindCollection(g.Boxes);
                    }
                    else
                    {
                        _eventOverlay.BindCollection(g.Boxes);
                    }
                }
            }

            if (wData.EventBoxGroups is INotifyCollectionChanged groupIncc)
            {
                groupIncc.CollectionChanged -= OnEventBoxGroupsChanged;
                groupIncc.CollectionChanged += OnEventBoxGroupsChanged;
            }
        }
    }

    /// <summary>
    /// Resolves the NPC model path using IGMDatabaseService and client assets folder.
    /// </summary>
    private string? ResolveNpcModelPath(NpcBox npcBox)
    {
        if (string.IsNullOrWhiteSpace(npcBox.NpcName))
            return null;

        if (string.IsNullOrEmpty(_npcClientAssetsFolder))
            return null;

        // Use database service to get the model path from NpcName
        string npcModel;
        if (_gmDatabaseService != null)
        {
            npcModel = _gmDatabaseService.GetNpcModelByName(npcBox.NpcName);
            if (string.IsNullOrEmpty(npcModel))
                return null;
        }
        else
        {
            // Fallback: use NpcName directly as path
            npcModel = npcBox.NpcName;
        }

        var mdataPath = Path.Combine(_npcClientAssetsFolder, npcModel);

        if (File.Exists(mdataPath))
        {
            try
            {
                // Read the mdata file to get the MGM path
                var mDataModel = Task.Run(() => MDataModelPathReader.ReadAsync(mdataPath)).GetAwaiter().GetResult();

                if (!string.IsNullOrWhiteSpace(mDataModel.MgmPath))
                {
                    // Resolve the MGM path relative to the mdata file's directory
                    return ResolveMgmPathFromMdata(_npcClientAssetsFolder, mdataPath, mDataModel.MgmPath);
                }
            }
            catch
            {
                // Fall back to direct MGM path if mdata reading fails
            }
        }

        // Fall back: change extension to .mgm directly
        return Path.ChangeExtension(mdataPath, ".mgm");
    }

    public static string ResolveModelPath(
    string clientAssetsFolder,
    string mmpDirectory,
    string modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath) || modelPath == @".\")
            return string.Empty;

        modelPath = modelPath.Replace('/', '\\');

        string resolvedMdataPath;

        // Case 1: .\object\... → relative to MMP directory
        if (modelPath.StartsWith(@".\"))
        {
            var relative = modelPath.Substring(2); // remove ".\"
            resolvedMdataPath = Path.Combine(mmpDirectory, relative);
        }
        else
        {
            // Case 2: ..\..\..\something → logical asset-root relative
            while (modelPath.StartsWith(@"..\"))
            {
                modelPath = modelPath.Substring(3);
            }

            resolvedMdataPath = Path.Combine(clientAssetsFolder, modelPath);
        }

        // Check if the resolved path is an mdata file (or would be)
        var mdataPath = Path.ChangeExtension(resolvedMdataPath, ".mdata");
        if (File.Exists(mdataPath))
        {
            try
            {
                // Read the mdata file to get the MGM path
                var mDataModel = Task.Run(() => MDataModelPathReader.ReadAsync(mdataPath)).GetAwaiter().GetResult();

                if (!string.IsNullOrWhiteSpace(mDataModel.MgmPath))
                {
                    // Resolve the MGM path relative to the mdata file's directory
                    return ResolveMgmPathFromMdata(clientAssetsFolder, mdataPath, mDataModel.MgmPath);
                }
            }
            catch
            {
                // Fall back to direct MGM path if mdata reading fails
            }
        }

        // Fall back: change extension to .mgm directly
        return Path.ChangeExtension(resolvedMdataPath, ".mgm");
    }

    /// <summary>
    /// Resolves the MGM path from an mdata file's MgmPath property.
    /// </summary>
    public static string ResolveMgmPathFromMdata(string clientAssetsFolder, string mdataFilePath, string mgmPath)
    {
        if (string.IsNullOrWhiteSpace(mgmPath))
            return string.Empty;

        mgmPath = mgmPath.Replace('/', '\\');

        // If it's a relative path starting with .\, resolve relative to mdata directory
        if (mgmPath.StartsWith(@".\"))
        {
            var mdataDir = Path.GetDirectoryName(mdataFilePath) ?? string.Empty;
            var relative = mgmPath.Substring(2);
            return Path.ChangeExtension(Path.Combine(mdataDir, relative), ".mgm");
        }

        // If it's a relative path with ..\ prefixes, navigate up from mdata directory
        if (mgmPath.StartsWith(@"..\"))
        {
            var mdataDir = Path.GetDirectoryName(mdataFilePath) ?? string.Empty;

            while (mgmPath.StartsWith(@"..\"))
            {
                mdataDir = Path.GetDirectoryName(mdataDir) ?? string.Empty;
                mgmPath = mgmPath.Substring(3);
            }

            return Path.ChangeExtension(Path.Combine(mdataDir, mgmPath), ".mgm");
        }

        // If it's an absolute-style path or asset-root relative, resolve against client assets folder
        if (!string.IsNullOrEmpty(clientAssetsFolder))
        {
            return Path.ChangeExtension(Path.Combine(clientAssetsFolder, mgmPath), ".mgm");
        }

        // Last resort: just ensure .mgm extension
        return Path.ChangeExtension(mgmPath, ".mgm");
    }

    private void OnEventBoxGroupsChanged(object? s, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (EventBoxGroup g in e.OldItems)
            {
                if (g.Boxes is INotifyCollectionChanged oldColl)
                {
                    if (g.Type == EventBoxType.NpcBox)
                        _npcOverlay.UnbindCollection(oldColl);
                    else
                        _eventOverlay.UnbindCollection(oldColl);
                }
            }
        }
        if (e.NewItems is not null)
        {
            foreach (EventBoxGroup g in e.NewItems)
            {
                if (g.Boxes is INotifyCollectionChanged newColl)
                {
                    if (g.Type == EventBoxType.NpcBox)
                        _npcOverlay.BindCollection(newColl);
                    else
                        _eventOverlay.BindCollection(newColl);
                }
            }
        }
    }

    #endregion

    #region Camera focus

    // animation state
    private bool _camAnimating;
    private DateTime _camAnimStart;
    private TimeSpan _camAnimDuration = TimeSpan.FromMilliseconds(700);
    private Point3D _camFromPos, _camToPos, _camTarget;

    private static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);
    private static Point3D Lerp(Point3D a, Point3D b, double t)
        => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);

    private void StartCameraAnimation(Point3D toPos, Point3D target, TimeSpan? duration = null)
    {
        _camAnimDuration = duration ?? TimeSpan.FromMilliseconds(700);
        _camFromPos = Camera.Position;
        _camToPos = toPos;
        _camTarget = target;
        _camAnimStart = DateTime.Now;

        if (!_camAnimating)
        {
            CompositionTarget.Rendering += OnCameraRendering;
            _camAnimating = true;
        }
    }

    public void StopCameraAnimation()
    {
        if (!_camAnimating) return;
        CompositionTarget.Rendering -= OnCameraRendering;
        _camAnimating = false;
    }

    private void OnCameraRendering(object? sender, EventArgs e)
    {
        double t = (DateTime.Now - _camAnimStart).TotalMilliseconds / _camAnimDuration.TotalMilliseconds;
        if (t >= 1) { t = 1; StopCameraAnimation(); }

        double s = EaseOutCubic(t);
        var pos = Lerp(_camFromPos, _camToPos, s);

        Camera.Position = pos;

        var look = new Vector3D(_camTarget.X - pos.X, _camTarget.Y - pos.Y, _camTarget.Z - pos.Z);
        Camera.LookDirection = look;
        Camera.UpDirection = new Vector3D(0, 1, 0);
    }

    /// <summary>
    /// Moves the camera to frame <paramref name="e"/> and makes it visible.
    /// </summary>
    public void FocusCamera(IObbEntity e)
    {
        // Compute scaled full sizes.
        double sx = Math.Max(1e-4, e.Extents.X * 2 * e.Scale.X);
        double sy = Math.Max(1e-4, e.Extents.Y * 2 * e.Scale.Y);
        double sz = Math.Max(1e-4, e.Extents.Z * 2 * e.Scale.Z);

        var target = new Point3D(
            -e.Position.X,
            e.Position.Y + sy * 0.75,
            e.Position.Z);

        // Bounding-sphere radius from scaled full sizes.
        double radius = 0.5 * Math.Sqrt(sx * sx + sy * sy + sz * sz);

        // Distance based on FOV so the whole object fits, plus padding.
        double fov = Camera?.FieldOfView > 0 ? Camera.FieldOfView : 45.0;
        double fovRad = fov * Math.PI / 180.0;
        double distFit = radius / Math.Tan(0.5 * fovRad);

        // Keep a safe minimum distance and add padding.
        double distance = Math.Clamp(distFit * 2.0, 250.0, 200000.0);

        var approachDir = CreateDefaultCamera().LookDirection;
        if (approachDir.LengthSquared < 1e-8) approachDir = new Vector3D(1, 1, -1);
        approachDir.Normalize();

        // Place the camera back from the target and above it to reduce ground domination.
        var dest = target - approachDir * distance;
        dest = new Point3D(dest.X, dest.Y + Math.Max(200.0, sy * 0.30), dest.Z);

        StartCameraAnimation(dest, target, TimeSpan.FromMilliseconds(500));
    }

    #endregion

    #region Scene Visibility
    private readonly List<MeshGeometryModel3D> _meshModels = [];
    private Element3D? _naviWireframeModel;        // LineGeometryModel3D
    public Element3D? BoneModel { get; set; } // LineGeometryModel3D
    private Element3D? _naviDebugGroup;        // GroupModel3D (portals + labels)
    private IReadOnlyDictionary<uint, Matrix4x4>? _worldByHash;

    #region NavMesh
    public void LoadNavMesh(bool showNaviWireframe = false, bool showNaviDebug = false)
    {
        if (NaviModel is not null)
        {
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
            ApplyNaviWireframeVisibility(showNaviWireframe);
            ApplyNaviDebugVisibility(showNaviDebug);
        }
    }

    // ===== builders =====
    public void EnsureNaviWireframe()
    {
        if (_naviWireframeModel != null || NaviModel is null) return;
        _naviWireframeModel = NaviToHelix.CreateNavTrianglesWireframe(
            NaviModel, _worldByHash
        );
    }

    public void EnsureNaviDebug()
    {
        if (_naviDebugGroup != null || NaviModel is null) return;
        _naviDebugGroup = NaviToHelix.CreateNavOverlay(
            NaviModel, _worldByHash
        );
    }

    // ===== add/remove helpers =====
    public void ApplyNaviWireframeVisibility(bool visible)
    {
        if (_naviWireframeModel == null) return;
        if (visible && !Scene3D.Contains(_naviWireframeModel)) Scene3D.Add(_naviWireframeModel);
        if (!visible && Scene3D.Contains(_naviWireframeModel)) Scene3D.Remove(_naviWireframeModel);
    }

    public void ApplyNaviDebugVisibility(bool visible)
    {
        if (_naviDebugGroup == null) return;
        if (visible && !Scene3D.Contains(_naviDebugGroup)) Scene3D.Add(_naviDebugGroup);
        if (!visible && Scene3D.Contains(_naviDebugGroup)) Scene3D.Remove(_naviDebugGroup);
    }
    #endregion

    #region Bones
    public void EnsureBones()
    {
        if (BoneModel != null || MgmModel is null) return;

        var skel = new SkeletonOptions
        {
            BoneRadius = 0.4,
            ShowJoints = true,
            JointRadius = 0.35
        };

        BoneModel = CreateSkeletonModel(MgmModel, skel);
    }

    public void ApplyBoneVisibility(bool visible)
    {
        if (BoneModel == null) return;
        if (visible && !Scene3D.Contains(BoneModel)) Scene3D.Add(BoneModel);
        if (!visible && Scene3D.Contains(BoneModel)) Scene3D.Remove(BoneModel);
    }
    #endregion

    #region Mesh Wireframe
    public void CollectMeshesFrom(Element3D e)
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

    public void ResetMeshCache()
    {
        _meshModels.Clear();
        _origFrontMat.Clear();
        _origBackMat.Clear();
    }

    public void ApplyMeshWireframe(bool wireframe)
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

    public void ApplyMeshSolidVisibility(bool showSolid)
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

    [ObservableProperty] private MmpModel? _mmpModel;
    [ObservableProperty] private MgmModel? _mgmModel;
    [ObservableProperty] private NaviMeshFile? _naviModel;

    #endregion
}
