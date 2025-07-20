﻿using Microsoft.Win32;
using RHToolkit.Models.Editor;
using RHToolkit.Models.MessageBox;
using System.Collections;
using static RHToolkit.Models.ObservablePrimitives;

namespace RHToolkit.Models.WDATA;

/// <summary>
/// Manages the WData editor, including file operations, undo/redo functionality, and collection management.
/// </summary>
public partial class WDataManager : ObservableObject
{
    private readonly CollectionHistory _history = new();
    private const string Filter = "Rusty Hearts World Data File (*.wdata)|*.wdata|All Files (*.*)|*.*";


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
    /// Opens a file dialog to load a WData file and reads its contents.
    /// </summary>
    /// <returns>True if the file was loaded successfully, otherwise false.</returns>
    public async Task<bool> LoadFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = Filter,
            FilterIndex = 1
        };

        if (dlg.ShowDialog() != true) return false;

        try
        {
            await CloseFile();

            CurrentFile = dlg.FileName;
            CurrentFileName = Path.GetFileName(dlg.FileName);

            await LoadWData(dlg.FileName);

            return true;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage(
                $"{Resources.Error}: {ex.Message}", Resources.Error);
            return false;
        }
    }

    /// <summary>
    /// Loads the specified WData object into the editor and attaches necessary handlers.
    /// </summary>
    /// <param name="wData">The WData to load.</param>
    private async Task LoadWData(string file)
    {
        var wData = new WData();

        wData = await WDataReader.ReadAsync(file, wData);

        WData = wData;
        AttachWDataHandlers(WData);
        HasChanges = false;
        OnCanExecuteFileCommandChanged();
    }

    /// <summary>
    /// Updates the editor state to indicate that a WData file has been loaded.
    /// </summary>
    public void IsLoaded()
    {
        Title = string.Format(Resources.EditorTitleFileName, "WData", CurrentFileName);
        IsMessageVisible = Visibility.Hidden;
        IsVisible = Visibility.Visible;
        OnCanExecuteFileCommandChanged();
    }

    /// <summary>
    /// Updates the execution state of file commands.
    /// </summary>
    private void OnCanExecuteFileCommandChanged()
    {
        SaveFileCommand.NotifyCanExecuteChanged();
        SaveFileAsCommand.NotifyCanExecuteChanged();
        SaveFileAsMIPCommand.NotifyCanExecuteChanged();
        AddNewRowCommand.NotifyCanExecuteChanged();
        UndoChangesCommand.NotifyCanExecuteChanged();
        RedoChangesCommand.NotifyCanExecuteChanged();
        CloseFileCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Determines if file commands can be executed.
    /// </summary>
    /// <returns>True if file commands can be executed, otherwise false.</returns>
    private bool CanExecuteFileCommand()
    {
        return WData is not null;
    }
    #endregion

    #region Save

    /// <summary>
    /// Saves the current Wdata to the current file.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasChanges))]
    public async Task SaveFile()
    {
        if (WData == null || CurrentFile == null) return;

        try
        {
            byte[] data = WDataWriter.Write(WData);
            await File.WriteAllBytesAsync(CurrentFile, data);

            HasChanges = false;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.DataTableManagerSaveFileErrorMessage}: {ex.Message}", Resources.Error);
        }
    }

    /// <summary>
    /// Saves the current Wdata to a new file.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task SaveFileAs()
    {
        if (WData == null || CurrentFileName == null) return;

        var dlg = new SaveFileDialog
        {
            Filter = Filter,
            FilterIndex = 1,
            FileName = CurrentFileName
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            string file = dlg.FileName;

            byte[] data = WDataWriter.Write(WData);
            await File.WriteAllBytesAsync(file, data);

            FileManager.ClearTempFile(CurrentFileName);

            HasChanges = false;
            CurrentFile = file;
            CurrentFileName = Path.GetFileName(file);
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage(
                $"{Resources.DataTableManagerSaveFileErrorMessage}: {ex.Message}",
                Resources.Error);
        }
    }

    /// <summary>
    /// Saves the current Wdata as a MIP file.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task SaveFileAsMIP()
    {
        if (WData == null || CurrentFileName == null) return;

        SaveFileDialog saveFileDialog = new()
        {
            Filter = "Rusty Hearts Patch File (*.mip)|*.mip|All Files (*.*)|*.*",
            FilterIndex = 1,
            FileName = CurrentFileName
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                string file = saveFileDialog.FileName;
                string? directory = Path.GetDirectoryName(file);
                byte[] data = WDataWriter.Write(WData);
                await FileManager.CompressFileToMipAsync(data, file);

            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage($"{Resources.DataTableManagerSaveFileErrorMessage}: {ex.Message}", Resources.Error);
            }
        }
    }

    #endregion

    #region Close File
    /// <summary>
    /// Closes the current file, prompting the user to save changes if necessary.
    /// </summary>
    /// <returns>True if the file was closed successfully, otherwise false.</returns>
    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    public async Task<bool> CloseFile()
    {
        if (WData == null) return true;

        if (HasChanges)
        {
            string message = $"{Resources.DataTableManagerSaveFileMessage} '{CurrentFileName}' ?";

            var result = RHMessageBoxHelper.ConfirmMessageYesNoCancel(message);

            if (result == MessageBoxResult.Yes)
            {
                await SaveFile();
                ClearFile();
                return true;
            }
            else if (result == MessageBoxResult.No)
            {
                ClearFile();
                return true;
            }
            else if (result == MessageBoxResult.Cancel)
            {
                return false;
            }
        }
        else
        {
            ClearFile();
            return true;
        }

        return true;
    }

    /// <summary>
    /// Clears the current file and resets the Wdata and related properties.
    /// </summary>
    public void ClearFile()
    {
        DetachWDataHandlers();
        WData = null;
        CurrentFile = null;
        CurrentFileName = null;
        HasChanges = false;
        IsVisible = Visibility.Hidden;
        IsMessageVisible = Visibility.Visible;
        Title = string.Format(Resources.EditorTitle, "WData");
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

    #region Handlers

    /// <summary>
    /// Attaches event handlers to the WData object to track changes and history.
    /// </summary>
    /// <param name="data"></param>
    private void AttachWDataHandlers(WData data)
    {
        data.PropertyChanged += (_, __) => HasChanges = true;

        _history.Attach(data.ItemBoxes);
        _history.Attach(data.Gimmicks);
        _history.Attach(data.AniBGs);
        _history.Attach(data.Triggers);
        _history.Attach(data.Scenes);
        _history.Attach(data.SceneResources);

        // nested EventBoxGroup.Boxes
        foreach (var g in data.EventBoxGroups)
            _history.Attach(g.Boxes);
        data.EventBoxGroups.CollectionChanged += (_, e) =>
        {
            if (e.NewItems is not null)
                foreach (EventBoxGroup g in e.NewItems)
                    _history.Attach(g.Boxes);
        };

        _history.Changed += HistoryChanged;
    }

    private void DetachWDataHandlers()
    {
        _history.Changed -= HistoryChanged;
        _history.Dispose();
    }

    private void HistoryChanged()
    {
        HasChanges = true;
        UndoChangesCommand.NotifyCanExecuteChanged();
        RedoChangesCommand.NotifyCanExecuteChanged();
    }
    #endregion

    #region Undo/Redo
    private bool CanUndoChanges() => _history.CanUndo;
    private bool CanRedoChanges() => _history.CanRedo;

    [RelayCommand(CanExecute = nameof(CanUndoChanges))]
    private void UndoChanges() => _history.Undo();

    [RelayCommand(CanExecute = nameof(CanRedoChanges))]
    private void RedoChanges() => _history.Redo();

    #endregion

    #region Row

    /// <summary>
    /// Adds a new row to the specified collection object.
    /// </summary>
    /// <param name="collectionObj"></param>
    [RelayCommand(CanExecute = nameof(CanExecuteFileCommand))]
    private void AddNewRow(object? collectionObj)
    {
        if (collectionObj is not IList list) return;

        // 1) Is this one of the EventBoxGroup→Boxes collections?
        var group = WData!.EventBoxGroups.FirstOrDefault(g => ReferenceEquals(g.Boxes, list));

        if (group != null)
        {
            // a) Create the correct derived box for this tab
            var newBox = CreateEventBox(group.Type);
            newBox.Type = group.Type;
            list.Add(newBox);
            SelectedItem = newBox;
        }
        else
        {
            // b) Fallback: non‑EventBox lists (ItemBoxes, Gimmicks, …)
            var itemType = list.GetType().GetGenericArguments()[0];
            var newItem = Activator.CreateInstance(itemType)!;
            list.Add(newItem);
            SelectedItem = newItem;
        }
    }

    /// <summary>
    /// Duplicates the selected row in the specified collection object.
    /// </summary>
    /// <param name="collectionObj"></param>
    [RelayCommand(CanExecute = nameof(CanExecuteSelectedRowCommand))]
    private void DuplicateSelectedRow(object? collectionObj)
    {
        if (collectionObj is IList list && SelectedItem is not null)
        {
            var clone = DeepClone(SelectedItem);
            list.Add(clone);
            SelectedItem = clone;
        }
    }

    /// <summary>
    /// deletes the selected row from the specified collection object.
    /// </summary>
    /// <param name="collectionObj"></param>
    [RelayCommand(CanExecute = nameof(CanExecuteSelectedRowCommand))]
    private void DeleteSelectedRow(object? collectionObj)
    {
        if (collectionObj is IList list && SelectedItem is not null)
        {
            int idx = list.IndexOf(SelectedItem);
            if (idx >= 0) list.RemoveAt(idx);
            SelectedItem = null;
        }
    }

    private bool CanExecuteSelectedRowCommand() => SelectedItem is not null;

    /// <summary>
    /// Creates a deep clone of the specified object using JSON serialization.
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    private static object DeepClone(object src)
    {
        var t = src.GetType();
        var json = System.Text.Json.JsonSerializer.Serialize(src, t);
        return System.Text.Json.JsonSerializer.Deserialize(json, t)!;
    }

    /// <summary>
    /// Initializes lists in the given object that are null.
    /// </summary>
    /// <param name="obj"></param>
    private static void InitLists(object obj)
    {
        foreach (var p in obj.GetType().GetProperties(
                         BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead || !p.CanWrite) continue;
            if (p.GetValue(obj) is not null) continue;

            var t = p.PropertyType;

            if (typeof(IList).IsAssignableFrom(t))
            {
                Type element = t.IsGenericType
                    ? t.GetGenericArguments()[0]
                    : typeof(object);

                var list = Activator.CreateInstance(
                    typeof(ObservableCollection<>).MakeGenericType(element))!;
                p.SetValue(obj, list);
            }
        }
    }

    /// <summary>
    /// Initializes a CameraBox with default values for its properties.
    /// </summary>
    /// <param name="box"></param>
    /// <returns> A CameraBox with initialized properties.</returns>
    private static CameraBox InitCameraBox(CameraBox box)
    {
        for (int i = 0; i < 4; i++)
            box.CameraInfos.Add(new CameraInfo
            {
                CameraPos = new Vector3(),
                CameraRot = new Vector3(),
                Frustum = [],
                PVS_BG = [],
                PVS_EventBox = [],
                PVS_EventEntries = [],
                RenderBg = [],
                RenderAniBg = [],
                RenderItem = [],
                RenderGimmick = [],
                NoRenderBg = [],
                NoRenderAniBg = [],
                NoRenderItem = [],
                NoRenderGimmick = [],
                BuildPos = []
            });

        return box;
    }

    /// <summary>
    /// Creates a new EventBox of the specified type.
    /// </summary>
    /// <param name="t"></param>
    /// <returns> A new instance of EventBox or its derived type.</returns>
    private static EventBox CreateEventBox(EventBoxType t)
    {
        EventBox eb = t switch
        {
            EventBoxType.CameraBox => InitCameraBox(new CameraBox()),
            EventBoxType.RespawnBox => new RespawnBox(),
            EventBoxType.StartPointBox => new StartPointBox(),
            EventBoxType.TriggerBox => new TriggerBox(),
            EventBoxType.SkidBox => new SkidBox(),
            EventBoxType.EventHitBox => new EventHitBox(),
            EventBoxType.NpcBox => new NpcBox(),
            EventBoxType.PortalBox => new PortalBox(),
            EventBoxType.SelectMapPortalBox => new SelectMapPortalBox(),
            EventBoxType.InAreaBox => new InAreaBox(),
            EventBoxType.EtcBox => new EtcBox(),
            EventBoxType.CameraBlockBox => new CameraBlockBox(),
            EventBoxType.CutoffBox => new CutoffBox(),
            EventBoxType.CameraTargetBox => new CameraTargetBox(),
            EventBoxType.MiniMapIconBox => new MiniMapIconBox(),
            EventBoxType.EnvironmentReverbBox => new EnvironmentReverbBox(),
            EventBoxType.WaypointBox => new WaypointBox(),
            EventBoxType.ObstacleBox => new ObstacleBox(),
            _ => new EventBox()
        };

        eb.Type = t;
        InitLists(eb);
        return eb;
    }

    #endregion

    #endregion

    #region Properties

    [ObservableProperty]
    private string _title = string.Format(Resources.EditorTitle, "WData");

    [ObservableProperty]
    private Visibility _isVisible = Visibility.Hidden;

    [ObservableProperty]
    private Visibility _isMessageVisible = Visibility.Visible;

    [ObservableProperty]
    private WData? _wData;
    partial void OnWDataChanged(WData? value)
    {
        OnCanExecuteFileCommandChanged();
    }

    [ObservableProperty]
    private string? _currentFile;

    [ObservableProperty]
    private string? _currentFileName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveFileCommand))]
    private bool _hasChanges = false;

    [ObservableProperty]
    private object? _selectedItem;
    partial void OnSelectedItemChanged(object? value)
    {
        CanExecuteSelectedRowCommand();
    }

    #endregion
}