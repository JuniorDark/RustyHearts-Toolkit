using System.Collections;
using System.ComponentModel;

namespace RHToolkit.Models.Editor;

/// <summary>One reversible change.</summary>
public interface IChange
{
    void Undo();
    void Redo();
}

/// <summary>
/// Represents a change to a row in a collection.
/// </summary>
internal sealed class RowChange : IChange
{
    private readonly IList _target;
    private readonly int _index;
    private readonly object _item;
    private readonly bool _isInsert;

    internal RowChange(IList target, int index, object item, bool isInsert)
    {
        _target = target; _index = index;
        _item = item; _isInsert = isInsert;
    }
    public void Undo()
    {
        if (_isInsert)
            _target.RemoveAt(_index);
        else
            _target.Insert(_index, CollectionHistory.DeepClone(_item));
    }

    public void Redo()
    {
        if (_isInsert)
            _target.Insert(_index, CollectionHistory.DeepClone(_item));
        else
            _target.RemoveAt(_index);
    }
}

/// <summary>
/// Represents a change to a property of an item in a collection.
/// </summary>
internal sealed class CellChange : IChange
{
    private readonly object _item;
    private readonly PropertyInfo _prop;
    private readonly object? _before, _after;

    internal CellChange(object item, PropertyInfo prop, object? before, object? after)
    { _item = item; _prop = prop; _before = before; _after = after; }

    public void Undo() => _prop.SetValue(_item, _before);
    public void Redo() => _prop.SetValue(_item, _after);
}

/// <summary>
/// Tracks changes to an ObservableCollection and allows undo/redo operations.
/// </summary>
public sealed class CollectionHistory : IDisposable
{
    private readonly Stack<IChange> _undo = new();
    private readonly Stack<IChange> _redo = new();
    private readonly HashSet<object> _observed = [];
    private readonly Dictionary<INotifyPropertyChanged, Dictionary<string, object?>> _snapshots = [];
    private bool _replaying;

    public IReadOnlyCollection<IChange> UndoStack => _undo;
    public IReadOnlyCollection<IChange> RedoStack => _redo;

    public event Action? Changed;

    private void Push(IChange c)
    {
        _undo.Push(c);
        _redo.Clear();
        Changed?.Invoke();
    }

    /// <summary>
    /// Attaches to an ObservableCollection of items that implement INotifyPropertyChanged.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="col"></param>
    public void Attach<T>(ObservableCollection<T> col)
    where T : INotifyPropertyChanged
    {
        foreach (var it in col) Hook(it);

        col.CollectionChanged += (s, e) =>
        {
            if (e.NewItems is not null)
                foreach (INotifyPropertyChanged n in e.NewItems)
                    Hook(n);

            if (e.OldItems is not null)
                foreach (INotifyPropertyChanged n in e.OldItems)
                    Unhook(n);

            if (_replaying) return;

            if (e.NewItems is not null)
            {
                int idx = ((IList)col).IndexOf(e.NewItems[0]);
                Push(new RowChange(col, idx, DeepClone(e.NewItems[0])!, isInsert: true));
            }
            else if (e.OldItems is not null)
            {
                int idx = e.OldStartingIndex;
                Push(new RowChange(col, idx, DeepClone(e.OldItems[0])!, isInsert: false));
            }
        };

    }

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    /// <summary>
    /// Undoes the last change made to the collection.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;
        _replaying = true;
        var c = _undo.Pop(); c.Undo();
        _redo.Push(c);
        _replaying = false;
        Changed?.Invoke();
    }

    /// <summary>
    /// Redoes the last undone change made to the collection.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;
        _replaying = true;
        var c = _redo.Pop(); c.Redo();
        _undo.Push(c);
        _replaying = false;
        Changed?.Invoke();
    }

    /// <summary>
    /// Disposes of the CollectionHistory, clearing all tracked changes and unhooking from items.
    /// </summary>
    public void Dispose()
    {
        _undo.Clear(); _redo.Clear(); _observed.Clear(); _snapshots.Clear();
    }

    /// <summary>
    /// Hooks into an INotifyPropertyChanged object to track its property changes.
    /// </summary>
    /// <param name="obj"></param>
    private void Hook(INotifyPropertyChanged obj)
    {
        if (!_observed.Add(obj)) return;

        Snapshot(obj);
        obj.PropertyChanged += OnItemChanged;

        foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite) continue;
            if (prop.GetIndexParameters().Length > 0) continue;
            if (ShouldIgnoreProperty(prop)) continue;

            var value = prop.GetValue(obj);
            if (value is INotifyPropertyChanged nested && !_observed.Contains(nested))
                Hook(nested);
        }
    }


    private void Unhook(INotifyPropertyChanged obj)
    {
        if (!_observed.Remove(obj)) return;
        obj.PropertyChanged -= OnItemChanged;
        _snapshots.Remove(obj);
    }

    /// <summary>
    /// Handles property changes for an INotifyPropertyChanged object, tracking changes to its properties.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_replaying) return;
        if (sender is not INotifyPropertyChanged npc) return;
        if (e.PropertyName is null) return;

        if (ShouldIgnoreProperty(e.PropertyName)) return;

        if (!_snapshots.TryGetValue(npc, out var map)) return;

        var pi = sender.GetType().GetProperty(e.PropertyName);
        if (pi is null || !pi.CanRead || !pi.CanWrite) return;

        var now = pi.GetValue(sender);
        map.TryGetValue(e.PropertyName, out var before);
        if (Equals(now, before)) return;

        Push(new CellChange(sender, pi, before, now));
        map[e.PropertyName] = DeepClone(now);
    }


    /// <summary>
    /// Creates a deep clone of an object using JSON serialization.
    /// </summary>
    /// <param name="src"></param>
    /// <returns> A deep clone of the source object, or null if the source is null.</returns>
    internal static object? DeepClone(object? src)
    {
        if (src is null) return null;
        var t = src.GetType();
        return System.Text.Json.JsonSerializer.Deserialize(
               System.Text.Json.JsonSerializer.Serialize(src, t), t);
    }

    /// <summary>
    /// Takes a snapshot of the current state of an INotifyPropertyChanged object.
    /// </summary>
    /// <param name="obj"></param>
    private void Snapshot(INotifyPropertyChanged obj)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var p in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead || !p.CanWrite) continue;
            if (p.GetIndexParameters().Length > 0) continue;
            if (ShouldIgnoreProperty(p)) continue;

            dict[p.Name] = DeepClone(p.GetValue(obj));
        }
        _snapshots[obj] = dict;
    }

    private static bool ShouldIgnoreProperty(string name)
    => string.Equals(name, "IsVisible", StringComparison.Ordinal);

    private static bool ShouldIgnoreProperty(PropertyInfo pi)
        => ShouldIgnoreProperty(pi.Name);
}
