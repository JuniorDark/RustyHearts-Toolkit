using HelixToolkit.Wpf.SharpDX;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Media.Media3D;
using Material = HelixToolkit.Wpf.SharpDX.Material;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;
using HelixToolkit.Geometry;
using HelixToolkit.SharpDX;
using System.Numerics;
using RHToolkit.Models.Model3D.MGM;
using RHToolkit.Models.Model3D.Model;

namespace RHToolkit.Models.WDATA;

/// <summary>
/// Delegate for custom model path resolution.
/// </summary>
/// <param name="item">The entity to resolve the model path for.</param>
/// <returns>The resolved model file path, or null if not available.</returns>
public delegate string? ModelPathResolver<T>(T item) where T : class, IObbEntity;

/// <summary>
/// Manages a set of overlay OBBs in a Helix 3D scene, based on a collection of IObbEntity items.
/// Supports both simple model path properties and custom model path resolution (e.g., for NPC models).
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ObbOverlayManager<T> where T : class, IObbEntity
{
    private readonly GroupModel3D _root;
    private readonly Material _material;
    private readonly Dictionary<T, Element3D> _nodes = [];
    private readonly Dictionary<T, string?> _loadedModelPaths = [];
    private readonly HashSet<INotifyCollectionChanged> _boundCollections = [];
    private MeshGeometry3D? _unitBox;

    // Context for resolving model paths
    private string _clientAssetsFolder = string.Empty;
    private string _mmpDirectory = string.Empty;

    // Custom model path resolver (e.g., for NPC models that need database lookup)
    private ModelPathResolver<T>? _customModelPathResolver;

    // Property name to watch for model changes (defaults to "Model", can be "NpcName" for NpcBox)
    private string _modelPropertyName = "Model";

    private sealed class Subs
    {
        public ObservablePrimitives.Vector3? Pos;
        public ObservablePrimitives.Vector3? Scale;
        public ObservablePrimitives.Quaternion? Rot;
        public ObservablePrimitives.Vector3? Ext;

        public PropertyChangedEventHandler? PosH;
        public PropertyChangedEventHandler? ScaleH;
        public PropertyChangedEventHandler? RotH;
        public PropertyChangedEventHandler? ExtH;
    }
    private readonly Dictionary<T, Subs> _subs = [];

    public ObbOverlayManager(string rootName, Material material)
    {
        _root = new GroupModel3D { Name = rootName };
        _material = material;
    }

    /// <summary>
    /// Sets a custom model path resolver for entities that need special handling (e.g., NPC models).
    /// </summary>
    /// <param name="resolver">The custom resolver function.</param>
    /// <param name="modelPropertyName">The property name to watch for changes (e.g., "NpcName").</param>
    public void SetCustomModelPathResolver(ModelPathResolver<T> resolver, string modelPropertyName = "Model")
    {
        _customModelPathResolver = resolver;
        _modelPropertyName = modelPropertyName;
    }

    /// <summary>
    /// Sets the context folders for resolving model paths.
    /// </summary>
    public void SetModelPathContext(string clientAssetsFolder, string mmpDirectory = "")
    {
        _clientAssetsFolder = clientAssetsFolder;
        _mmpDirectory = mmpDirectory;
    }

    public void AttachRoot(ObservableElement3DCollection scene)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!scene.Contains(_root)) scene.Add(_root);
        });
    }

    public void ClearAll()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var kv in _nodes.ToList()) RemoveNode(kv.Key);
            _root.Children.Clear();
        });

        _loadedModelPaths.Clear();

        foreach (var c in _boundCollections.ToList())
            UnbindCollection(c);
    }

    public void BindCollection(INotifyCollectionChanged collection)
    {
        if (!_boundCollections.Add(collection))
            return;

        collection.CollectionChanged += OnCollectionChanged;

        // Handle both IEnumerable<T> and IEnumerable<EventBox> (with T items via OfType)
        if (collection is IEnumerable<T> seq)
        {
            foreach (var item in seq) AttachItem(item);
        }
        else if (collection is IEnumerable<EventBox> eventSeq)
        {
            foreach (var item in eventSeq.OfType<T>()) AttachItem(item);
        }
    }

    public void UnbindCollection(INotifyCollectionChanged collection)
    {
        if (!_boundCollections.Remove(collection))
            return;

        collection.CollectionChanged -= OnCollectionChanged;

        // Handle both IEnumerable<T> and IEnumerable<EventBox> (with T items via OfType)
        if (collection is IEnumerable<T> seq)
        {
            foreach (var item in seq) DetachItem(item);
        }
        else if (collection is IEnumerable<EventBox> eventSeq)
        {
            foreach (var item in eventSeq.OfType<T>()) DetachItem(item);
        }
    }

    private void OnCollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is T typedItem) DetachItem(typedItem);
            }
        }
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is T typedItem) AttachItem(typedItem);
            }
        }
    }

    private void AttachItem(T item)
    {
        item.PropertyChanged -= OnItemChanged;
        item.PropertyChanged += OnItemChanged;

        HookNested(item);

        if (item.IsVisible && !_nodes.ContainsKey(item))
            CreateNode(item);
    }

    private void DetachItem(T item)
    {
        item.PropertyChanged -= OnItemChanged;
        UnhookNested(item);
        RemoveNode(item);
        _loadedModelPaths.Remove(item);
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not T item) return;

        if (e.PropertyName == nameof(IObbEntity.IsVisible))
        {
            if (item.IsVisible) { if (!_nodes.ContainsKey(item)) CreateNode(item); }
            else RemoveNode(item);
            return;
        }

        if (e.PropertyName is nameof(IObbEntity.Position)
                           or nameof(IObbEntity.Scale)
                           or nameof(IObbEntity.Rotation)
                           or nameof(IObbEntity.Extents))
        {
            ResubscribeNested(item, e.PropertyName!);
            UpdateNodeTransform(item);
            return;
        }

        if (e.PropertyName == nameof(IObbEntity.Name) && _nodes.TryGetValue(item, out var n))
            n.Tag = item.Name;

        // Handle model property change (either "Model" or custom property like "NpcName")
        if (e.PropertyName == _modelPropertyName)
        {
            OnModelPathChanged(item);
        }
    }

    private void OnModelPathChanged(T item)
    {
        if (!item.IsVisible) return;

        var newModelPath = ResolveModelPathForItem(item);
        var currentLoadedPath = _loadedModelPaths.GetValueOrDefault(item);

        if (newModelPath != currentLoadedPath)
        {
            // Remove old node and create new one
            RemoveNode(item);
            CreateNode(item);
        }
    }

    private void HookNested(T item)
    {
        var s = new Subs();
        _subs[item] = s;

        // Position
        s.Pos = item.Position;
        if (s.Pos != null)
        {
            s.PosH = (_, __) => UpdateNodeTransform(item);
            s.Pos.PropertyChanged += s.PosH;
        }

        // Scale
        s.Scale = item.Scale;
        if (s.Scale != null)
        {
            s.ScaleH = (_, __) => UpdateNodeTransform(item);
            s.Scale.PropertyChanged += s.ScaleH;
        }

        // Rotation
        s.Rot = item.Rotation;
        if (s.Rot != null)
        {
            s.RotH = (_, __) => UpdateNodeTransform(item);
            s.Rot.PropertyChanged += s.RotH;
        }

        // Extents
        s.Ext = item.Extents;
        if (s.Ext != null)
        {
            s.ExtH = (_, __) => UpdateNodeTransform(item);
            s.Ext.PropertyChanged += s.ExtH;
        }
    }

    private void UnhookNested(T item)
    {
        if (!_subs.TryGetValue(item, out var s)) return;

        if (s.Pos != null && s.PosH != null) s.Pos.PropertyChanged -= s.PosH;
        if (s.Scale != null && s.ScaleH != null) s.Scale.PropertyChanged -= s.ScaleH;
        if (s.Rot != null && s.RotH != null) s.Rot.PropertyChanged -= s.RotH;
        if (s.Ext != null && s.ExtH != null) s.Ext.PropertyChanged -= s.ExtH;

        _subs.Remove(item);
    }

    private void ResubscribeNested(T item, string parentPropName)
    {
        if (!_subs.TryGetValue(item, out var s))
        {
            HookNested(item);
            return;
        }

        switch (parentPropName)
        {
            case nameof(IObbEntity.Position):
                if (s.Pos != null && s.PosH != null) s.Pos.PropertyChanged -= s.PosH;
                s.Pos = item.Position;
                if (s.Pos != null)
                {
                    s.PosH = (_, __) => UpdateNodeTransform(item);
                    s.Pos.PropertyChanged += s.PosH;
                }
                break;

            case nameof(IObbEntity.Scale):
                if (s.Scale != null && s.ScaleH != null) s.Scale.PropertyChanged -= s.ScaleH;
                s.Scale = item.Scale;
                if (s.Scale != null)
                {
                    s.ScaleH = (_, __) => UpdateNodeTransform(item);
                    s.Scale.PropertyChanged += s.ScaleH;
                }
                break;

            case nameof(IObbEntity.Rotation):
                if (s.Rot != null && s.RotH != null) s.Rot.PropertyChanged -= s.RotH;
                s.Rot = item.Rotation;
                if (s.Rot != null)
                {
                    s.RotH = (_, __) => UpdateNodeTransform(item);
                    s.Rot.PropertyChanged += s.RotH;
                }
                break;

            case nameof(IObbEntity.Extents):
                if (s.Ext != null && s.ExtH != null) s.Ext.PropertyChanged -= s.ExtH;
                s.Ext = item.Extents;
                if (s.Ext != null)
                {
                    s.ExtH = (_, __) => UpdateNodeTransform(item);
                    s.Ext.PropertyChanged += s.ExtH;
                }
                break;
        }
    }

    private void UpdateNodeTransform(T item)
    {
        if (_nodes.TryGetValue(item, out var node))
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Check if this is a model (GroupModel3D) or a box (MeshGeometryModel3D)
                var isModel = _loadedModelPaths.TryGetValue(item, out var path) && !string.IsNullOrEmpty(path);
                node.Transform = isModel ? BuildModelTransform(item) : BuildBoxTransform(item);
            });
        }
    }

    private void CreateNode(T item)
    {
        var resolvedPath = ResolveModelPathForItem(item);

        Element3D? node = null;

        // Try to load the model if path is valid
        if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
        {
            try
            {
                var mgmModel = Task.Run(() => MGMReader.ReadAsync(resolvedPath)).GetAwaiter().GetResult();
                MGMReader.ValidateMgmModel(mgmModel);
                mgmModel.FilePath = resolvedPath;

                // Create a group to hold all mesh nodes from the model
                var group = new GroupModel3D { Tag = item.Name };
                foreach (var meshNode in MGMToHelix.CreateMGMNodes(mgmModel))
                {
                    group.Children.Add(meshNode);
                }

                group.Transform = BuildModelTransform(item);
                node = group;
                _loadedModelPaths[item] = resolvedPath;
            }
            catch
            {
                // Fall back to box on any error
                node = null;
            }
        }

        // Fall back to unit box if model loading failed or path is empty
        if (node == null)
        {
            EnsureUnitBox();
            node = new MeshGeometryModel3D
            {
                Geometry = _unitBox!,
                Material = _material,
                Tag = item.Name,
                Transform = BuildBoxTransform(item)
            };
            _loadedModelPaths[item] = null;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            _root.Children.Add(node);
            _nodes[item] = node;
        });
    }

    private void RemoveNode(T item)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_nodes.TryGetValue(item, out var node))
            {
                _root.Children.Remove(node);
                _nodes.Remove(item);
            }
        });
    }

    private void EnsureUnitBox()
    {
        if (_unitBox != null) return;
        var mb = new MeshBuilder();
        mb.AddBox(Vector3.Zero, 1, 1, 1);
        _unitBox = mb.ToMeshGeometry3D();
    }

    /// <summary>
    /// Resolves the model path for an item using either the custom resolver or the default Model property.
    /// </summary>
    private string? ResolveModelPathForItem(T item)
    {
        // Use custom resolver if available
        if (_customModelPathResolver != null)
        {
            return _customModelPathResolver(item);
        }

        // Default: use the Model property via reflection
        var modelPath = GetModelPropertyValue(item);
        return ResolveModelPath(modelPath);
    }

    /// <summary>
    /// Gets the model property value from the entity using reflection.
    /// </summary>
    private string? GetModelPropertyValue(T item)
    {
        var modelProperty = typeof(T).GetProperty(_modelPropertyName);
        return modelProperty?.GetValue(item) as string;
    }

    /// <summary>
    /// Resolves a model path to an absolute .mgm file path.
    /// If the path points to a .mdata file, reads it to extract the MGM path.
    /// </summary>
    private string? ResolveModelPath(string? modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath) || modelPath == @".\")
            return null;

        modelPath = modelPath.Replace('/', '\\');

        string? resolvedMdataPath = null;

        // Case 1: .\object\... → relative to MMP directory
        if (modelPath.StartsWith(@".\") && !string.IsNullOrEmpty(_mmpDirectory))
        {
            var relative = modelPath.Substring(2);
            resolvedMdataPath = Path.Combine(_mmpDirectory, relative);
        }
        else
        {
            // Case 2: ..\..\..\something → logical asset-root relative
            while (modelPath.StartsWith(@"..\"))
            {
                modelPath = modelPath.Substring(3);
            }

            if (!string.IsNullOrEmpty(_clientAssetsFolder))
            {
                resolvedMdataPath = Path.Combine(_clientAssetsFolder, modelPath);
            }
        }

        if (string.IsNullOrEmpty(resolvedMdataPath))
            return null;

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
                    return ResolveMgmPathFromMdata(mdataPath, mDataModel.MgmPath);
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
    private string? ResolveMgmPathFromMdata(string mdataFilePath, string mgmPath)
    {
        if (string.IsNullOrWhiteSpace(mgmPath))
            return null;

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

        // If it starts with asset-root relative patterns, resolve against client assets folder
        var tempPath = mgmPath;
        while (tempPath.StartsWith(@"..\"))
        {
            tempPath = tempPath.Substring(3);
        }

        if (!string.IsNullOrEmpty(_clientAssetsFolder))
        {
            return Path.ChangeExtension(Path.Combine(_clientAssetsFolder, tempPath), ".mgm");
        }

        // Last resort: just ensure .mgm extension
        return Path.ChangeExtension(mgmPath, ".mgm");
    }

    /// <summary>
    /// Builds transform for the default box overlay (includes extents scaling).
    /// </summary>
    private static Transform3DGroup BuildBoxTransform(IObbEntity e)
    {
        var tg = new Transform3DGroup();
        tg.Children.Add(new ScaleTransform3D(
            Math.Max(1e-6, e.Extents.X * 2 * e.Scale.X),
            Math.Max(1e-6, e.Extents.Y * 2 * e.Scale.Y),
            Math.Max(1e-6, e.Extents.Z * 2 * e.Scale.Z)));
        var q = new System.Windows.Media.Media3D.Quaternion(e.Rotation.X, e.Rotation.Y, e.Rotation.Z, -e.Rotation.W);
        tg.Children.Add(new RotateTransform3D(new QuaternionRotation3D(q)));
        tg.Children.Add(new TranslateTransform3D(-e.Position.X, e.Position.Y, e.Position.Z));
        return tg;
    }

    /// <summary>
    /// Builds transform for loaded models.
    /// </summary>
    private static Transform3DGroup BuildModelTransform(IObbEntity e)
    {
        var tg = new Transform3DGroup();
        tg.Children.Add(new ScaleTransform3D(e.Scale.X, e.Scale.Y, e.Scale.Z));
        var q = new System.Windows.Media.Media3D.Quaternion(e.Rotation.X, e.Rotation.Y, e.Rotation.Z, -e.Rotation.W);
        tg.Children.Add(new RotateTransform3D(new QuaternionRotation3D(q)));
        tg.Children.Add(new TranslateTransform3D(-e.Position.X, e.Position.Y, e.Position.Z));
        return tg;
    }
}
