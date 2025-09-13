using HelixToolkit.Wpf.SharpDX;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Media.Media3D;
using SDX = SharpDX;
using Material = HelixToolkit.Wpf.SharpDX.Material;
using MeshGeometry3D = HelixToolkit.Wpf.SharpDX.MeshGeometry3D;

namespace RHToolkit.Models.WDATA;

/// <summary>
/// Manages a set of overlay OBBs in a Helix 3D scene, based on a collection of IObbEntity items.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ObbOverlayManager<T> where T : class, IObbEntity
{
    private readonly GroupModel3D _root;
    private readonly Material _material;
    private readonly Dictionary<T, MeshGeometryModel3D> _nodes = [];
    private readonly HashSet<INotifyCollectionChanged> _boundCollections = [];
    private MeshGeometry3D? _unitBox;

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

        foreach (var c in _boundCollections.ToList())
            UnbindCollection(c);
    }

    public void BindCollection(INotifyCollectionChanged collection)
    {
        if (!_boundCollections.Add(collection))
            return;

        collection.CollectionChanged += OnCollectionChanged;

        if (collection is IEnumerable<T> seq)
            foreach (var item in seq) AttachItem(item);
    }

    public void UnbindCollection(INotifyCollectionChanged collection)
    {
        if (!_boundCollections.Remove(collection))
            return;

        collection.CollectionChanged -= OnCollectionChanged;

        if (collection is IEnumerable<T> seq)
            foreach (var item in seq) DetachItem(item);
    }

    private void OnCollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null) foreach (T item in e.OldItems) DetachItem(item);
        if (e.NewItems != null) foreach (T item in e.NewItems) AttachItem(item);
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
                node.Transform = BuildTransform(item);
            });
        }
    }

    private void CreateNode(T item)
    {
        EnsureUnitBox();

        Application.Current.Dispatcher.Invoke(() =>
        {
            var node = new MeshGeometryModel3D
            {
                Geometry = _unitBox!,
                Material = _material,
                Tag = item.Name,
                Transform = BuildTransform(item)
            };
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
        mb.AddBox(SDX.Vector3.Zero, 1, 1, 1);
        _unitBox = mb.ToMeshGeometry3D();
    }

    private static Transform3DGroup BuildTransform(IObbEntity e)
    {
        var tg = new Transform3DGroup();
        tg.Children.Add(new ScaleTransform3D(
            Math.Max(1e-6, e.Extents.X * 2 * e.Scale.X),
            Math.Max(1e-6, e.Extents.Y * 2 * e.Scale.Y),
            Math.Max(1e-6, e.Extents.Z * 2 * e.Scale.Z)));
        var q = new Quaternion(e.Rotation.X, e.Rotation.Y, e.Rotation.Z, e.Rotation.W);
        tg.Children.Add(new RotateTransform3D(new QuaternionRotation3D(q)));
        tg.Children.Add(new TranslateTransform3D(e.Position.X, e.Position.Y, e.Position.Z));
        tg.Children.Add(new ScaleTransform3D(-1, 1, 1));
        return tg;
    }
}
