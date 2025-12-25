using Microsoft.Win32;
using RHToolkit.Messages;
using RHToolkit.Models.MessageBox;
using RHToolkit.Models.Model3D.Map;
using RHToolkit.Models.Model3D.MGM;
using RHToolkit.Models.Model3D.Model;
using RHToolkit.Models.UISettings;
using RHToolkit.Models.WDATA;
using RHToolkit.Services;
using static RHToolkit.Models.Model3D.Map.MMP;
namespace RHToolkit.Models.Model3D;

/// <summary>
/// Manages the Model View Window and its functionalities.
/// </summary>
public partial class ModelViewManager : ObservableObject, IRecipient<ModelMessage>
{
    private const string FileDialogFilter =
    "Rusty Hearts Models (*.WDATA;*.MMP;*.MGM;*.NAVI)|*.wdata;*.mmp;*.mgm;*.navi|Map Data Models (*.WDATA)|*.wdata|Map Models (*.MMP)|*.mmp|MGM Models (*.MGM)|*.mgm|Navigation Mesh (*.NAVI)|*.navi|All Files (*.*)|*.*";
    
    private readonly IGMDatabaseService _gmDatabaseService;

    /// <summary>
    /// Parameterless constructor that resolves IGMDatabaseService from App.Services.
    /// </summary>
    public ModelViewManager()
        : this(App.Services.GetRequiredService<IGMDatabaseService>())
    {
    }

    public ModelViewManager(IGMDatabaseService gmDatabaseService)
    {
        _gmDatabaseService = gmDatabaseService;
        WeakReferenceMessenger.Default.Register(this);
    }

    #region Message Receiver

    /// <summary>
    /// Receives and processes ModelMessage messages.
    /// </summary>
    /// <param name="message"></param>
    public async void Receive(ModelMessage message)
    {
        if (message.Recipient == "ModelViewWindow" && message.Token == Token)
        {
            var modelData = message.Value;

            if (modelData.FilePath is not null)
            {
                switch (modelData.Format)
                {
                    case ModelFormat.MMP:
                        await LoadMMPAsync(modelData);
                        break;
                        case ModelFormat.MGM:
                        await LoadMGMAsync(modelData);
                        break;
                    case ModelFormat.MDATA:
                        await LoadMDataAsync(modelData);
                        break;
                    case ModelFormat.WDATA:
                        await LoadWDataAsync(modelData);
                        break;
                        default: return;
                }
            }

        }
    }

    #endregion

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
            FilterIndex = 1,
            DefaultDirectory = RegistrySettingsHelper.GetClientAssetsFolder(),
        };

        if (dlg.ShowDialog() != true) return false;

        try
        {
            await CloseFile();

            CurrentFile = dlg.FileName;
            CurrentFileName = Path.GetFileName(dlg.FileName);

            await LoadModelAsync(dlg.FileName);

            return true;
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage(
                string.Format(Resources.ErrorLoadingFile, CurrentFileName, ex.Message), Resources.Error);
            ClearFile();
            return false;
        }
    }

    /// <summary>
    /// Updates the editor state to indicate that a file has been loaded.
    /// </summary>
    public void IsLoaded()
    {
        Title = string.Format(Resources.ModelViewerTitle, CurrentFileName);
        IsMessageVisible = Visibility.Hidden;
        Message = string.Empty;
        IsVisible = Visibility.Visible;
        Scene3DView.FrameScene(includeBones: true);
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

    private bool CanExecuteExportFileCommand()
    {
        return MmpModel is not null || MgmModel is not null;
    }
    #endregion

    #region Export FBX
    [RelayCommand(CanExecute = nameof(CanExecuteExportFileCommand))]
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
                    RHMessageBoxHelper.ShowOKMessage(string.Format(Resources.ModelViewerExportedMgmToFbx, dlg.FileName), Resources.ModelViewerFbxExporterTitle);
                }
                else if (MmpModel is not null)
                {
                    await MMPExporter.ExportMmpToFbx(MmpModel, dlg.FileName, EmbedTextures, copyTextures: false, ExportSeparateObjects);
                    var message = string.Format(Resources.ModelViewerExportedMmpToFbx, dlg.FileName);
                    if (ExportSeparateObjects)
                    {
                        var objectsDir = Path.Combine(Path.GetDirectoryName(dlg.FileName)!, fileName);
                        message += $"\n\nSeparate objects exported to:\n{objectsDir}";
                    }
                    RHMessageBoxHelper.ShowOKMessage(message, Resources.ModelViewerFbxExporterTitle);
                }
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage(string.Format(Resources.ModelViewerExportError, ex.Message), Resources.Error);
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

        // Check if sidecar JSON exists (primary source for materials)
        var sidecarPath = ModelMaterialSidecar.GetSidecarPath(fbxPath);
        var hasSidecar = File.Exists(sidecarPath);

        // Find the original MMP with same name in the same folder (fallback)
        var mmpPath = Path.Combine(dir, baseName + ".mmp");
        var hasMmp = File.Exists(mmpPath);

        if (!hasSidecar && !hasMmp)
        {
            RHMessageBoxHelper.ShowOKMessage(
                $"No material source found.\n\nExpected either:\n• Sidecar JSON: {sidecarPath}\n• Original MMP: {mmpPath}",
                "Import FBX");
            return;
        }

        // Ask where to save the rebuilt MMP (default: same folder, same name + _new)
        var sfd = new SaveFileDialog
        {
            Filter = "Rusty Hearts Map File (*.mmp)|*.mmp|All Files (*.*)|*.*",
            FilterIndex = 1,
            FileName = baseName + "_new",
            DefaultExt = ".mmp",
            InitialDirectory = dir
        };
        if (sfd.ShowDialog() != true) return;

        var outMmpPath = sfd.FileName;

        try
        {
            // Pass MMP path as fallback (can be null if sidecar exists)
            await MMPWriter.RebuildFromFbx(hasMmp ? mmpPath : null, fbxPath, outMmpPath);

            var source = hasSidecar ? "sidecar JSON" : "original MMP";
            RHMessageBoxHelper.ShowOKMessage($"Exported FBX → MMP (materials from {source}):\n{outMmpPath}", "MMP Exporter");
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"Export failed: {ex.Message}", Resources.Error);
        }
    }

    #endregion

    #region Export Height
    [RelayCommand(CanExecute = nameof(CanExecuteExportHeightCommand))]
    public async Task ExportHeight()
    {
        if (CurrentFile is null) return;

        var fileName = Path.GetFileNameWithoutExtension(CurrentFile);

        var sfd = new SaveFileDialog
        {
            Filter = "Rusty Hearts Navigation Mesh Height Map (*.height)|*.height|All Files (*.*)|*.*",
            FilterIndex = 1,
            FileName = fileName + "_new",
            DefaultExt = ".height",
            AddExtension = true,
            InitialDirectory = Path.GetDirectoryName(CurrentFile)
        };

        if (sfd.ShowDialog() == true)
        {
            var outHeightPath = sfd.FileName;

            try
            {
                var naviPath = Path.ChangeExtension(CurrentFile, ".navi");
                await HeightWriter.BuildFromNaviFileAsync(naviPath, outHeightPath);
                RHMessageBoxHelper.ShowOKMessage($"Exported Height:\n{outHeightPath}", "Success");
            }
            catch (Exception ex)
            {
                RHMessageBoxHelper.ShowOKMessage(
                    string.Format(Resources.ModelViewerExportError, ex.Message),
                    Resources.Error);
            }
        }
    }

    /// <summary>
    /// Updates the execution state of file commands.
    /// </summary>
    private void OnCanExecuteExportHeightCommandChanged()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ExportHeightCommand.NotifyCanExecuteChanged();
        });
    }

    /// <summary>
    /// Determines if file command can be executed.
    /// </summary>
    /// <returns>True if file commands can be executed, otherwise false.</returns>
    private bool CanExecuteExportHeightCommand()
    {
        return NaviModel is not null;
    }
    #endregion

    #region Model Load
    
    private async Task LoadModelAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant();

        switch (ext)
        {
            case ".wdata":
                {
                    var modelType = new ModelType
                    {
                        Format = ModelFormat.WDATA,
                        FilePath = filePath
                    };
                    await LoadWDataAsync(modelType);
                    break;
                }
            case ".mgm":
                {
                    var modelType = new ModelType
                    {
                        Format = ModelFormat.MGM,
                        FilePath = filePath
                    };
                    await LoadMGMAsync(modelType);
                    break;
                }
            case ".mmp":
                {
                    var modelType = new ModelType
                    {
                        Format = ModelFormat.MMP,
                        FilePath = filePath
                    };

                    await LoadMMPAsync(modelType);
                    break;
                }
            case ".navi":
                {
                    var modelType = new ModelType
                    {
                        Format = ModelFormat.NAVI,
                        FilePath = filePath
                    };

                    await LoadNaviAsync(modelType);
                    break;
                }
            default:
                throw new NotSupportedException(string.Format(Resources.ModelViewerUnsupportedExtension, ext));
        }
    }

    #region MMP
    /// <summary>
    /// Loads the MMP file and updates the 3D scene.
    /// </summary>
    /// <param name="wdataPath"></param>
    /// <returns></returns>
    private async Task LoadMMPAsync(ModelType model)
    {
        Scene3DView.ClearScene();
        Message = string.Format(Resources.ModelViewerLoadingModel, "MMP");
        IsPlaneGridVisible = Visibility.Hidden;

        var mmp = await Task.Run(() => MMPReader.ReadAsync(model.FilePath)).ConfigureAwait(true);
        mmp.FilePath = model.FilePath;

        var naviPath = Path.ChangeExtension(model.FilePath, ".navi");
        if (File.Exists(naviPath))
        {
            NaviModel = await Task.Run(() => NaviReader.ReadAsync(naviPath)).ConfigureAwait(true);
        }

        if (mmp is not null)
        {
            MgmModel = null;
            MmpModel = mmp;
            Scene3DView.MmpModel = mmp;
            Version = mmp.Version;
            ObjectCount = mmp.Objects.Count;
            MaterialCount = mmp.Materials.Count;
            BoneCount = 0;
            CurrentFile = mmp.FilePath;
            CurrentFileName = Path.GetFileName(mmp.FilePath);

            foreach (var node in MMPToHelix.CreateMMPNodes(mmp))
            {
                Scene3DView.Scene3D.Add(node);
                Scene3DView.CollectMeshesFrom(node);
            }

            if (NaviModel is not null)
            {
                Scene3DView.NaviModel = NaviModel;
                Scene3DView.LoadNavMesh(ShowNaviWireframe, ShowNaviDebug);
                IsNavMeshControlsVisible = Visibility.Visible;
                Scene3DView.ApplyMeshWireframe(ShowMeshWireframe);
            }

            if (model.WData is not null)
            {
                var mmpDirectory = Path.GetDirectoryName(mmp.FilePath)!;

                // Attach overlays
                Scene3DView.AttachOverlays(model.WData, ClientAssetsFolder, mmpDirectory);

                // Set IsVisible = true for entities that have valid model paths
                // This triggers the overlay manager to load and display the models
                SetVisibleForEntitiesWithModels(model.WData.AniBGs, ClientAssetsFolder, mmpDirectory);
                SetVisibleForEntitiesWithModels(model.WData.Gimmicks, ClientAssetsFolder, mmpDirectory);
                SetVisibleForEntitiesWithModels(model.WData.ItemBoxes, ClientAssetsFolder, mmpDirectory);
                SetVisibleForNpcBoxes(model.WData.EventBoxGroups, ClientAssetsFolder);
            }

            Scene3DView.FrameScene(true);
            IsLoaded();
        }
    }

    /// <summary>
    /// Sets IsVisible = true for entities that have valid model paths.
    /// This triggers the overlay manager to load and display the models.
    /// </summary>
    /// <typeparam name="T">Entity type implementing IObbEntity with a Model property.</typeparam>
    /// <param name="entities">Collection of entities to check.</param>
    /// <param name="clientAssetsFolder">Client assets folder path.</param>
    /// <param name="mmpDirectory">MMP file directory path.</param>
    private static void SetVisibleForEntitiesWithModels<T>(
        IEnumerable<T> entities,
        string clientAssetsFolder,
        string mmpDirectory) where T : IObbEntity
    {
        foreach (var entity in entities)
        {
            // Get the Model property using reflection
            var modelProperty = typeof(T).GetProperty("Model");
            if (modelProperty == null) continue;

            var modelPathValue = modelProperty.GetValue(entity) as string;
            if (string.IsNullOrEmpty(modelPathValue)) continue;

            var modelPath = ResolveModelPath(clientAssetsFolder, mmpDirectory, modelPathValue);

            if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
            {
                // Set IsVisible to true - this triggers the overlay manager to create the node with the model
                var isVisibleProperty = typeof(T).GetProperty("IsVisible");
                isVisibleProperty?.SetValue(entity, true);
            }
        }
    }

    private static string ResolveModelPath(
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
    private static string ResolveMgmPathFromMdata(string clientAssetsFolder, string mdataFilePath, string mgmPath)
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

    /// <summary>
    /// Sets IsVisible = true for NpcBox entities that have valid model paths.
    /// The NpcName is in the format: Character\NPC\npc_gunman01\npc_gunman01.mdata
    /// </summary>
    /// <param name="eventBoxGroups">Collection of event box groups to check.</param>
    /// <param name="clientAssetsFolder">Client assets folder path.</param>
    private void SetVisibleForNpcBoxes(
        IEnumerable<EventBoxGroup> eventBoxGroups,
        string clientAssetsFolder)
    {
        foreach (var group in eventBoxGroups)
        {
            if (group.Type != EventBoxType.NpcBox) continue;

            foreach (var box in group.Boxes)
            {
                if (box is NpcBox npcBox)
                {
                    var modelPath = ResolveNpcModelPath(clientAssetsFolder, npcBox.NpcName);

                    if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
                    {
                        npcBox.IsVisible = true;
                    }
                }
            }
        }
    }

    private string ResolveNpcModelPath(string clientAssetsFolder, string? npcName)
    {
        if (string.IsNullOrWhiteSpace(npcName)) return string.Empty;

        var npcModel = _gmDatabaseService.GetNpcModelByName(npcName);

        return Path.Combine(clientAssetsFolder, npcModel);

    }

    #endregion

    #region MGM

    /// <summary>
    /// Loads the MMP file associated with the given WData file path and updates the 3D scene.
    /// </summary>
    /// <param name="wdataPath"></param>
    /// <returns></returns>
    private async Task LoadMGMAsync(ModelType model)
    {
        Scene3DView.ClearScene();
        Message = string.Format(Resources.ModelViewerLoadingModel, "MGM");
        IsPlaneGridVisible = Visibility.Visible;

        var mgm = await Task.Run(() => MGMReader.ReadAsync(model.FilePath)).ConfigureAwait(true);

        if (mgm is not null)
        {
            MGMReader.ValidateMgmModel(mgm);
            mgm.FilePath = model.FilePath;
            MgmModel = mgm;
            MmpModel = null;
            NaviModel = null;
            Scene3DView.MgmModel = mgm;
            IsNavMeshControlsVisible = Visibility.Collapsed;
            Version = mgm.Version;
            ObjectCount = mgm.Meshes.Count;
            MaterialCount = mgm.Materials.Count;
            BoneCount = mgm.Bones.Count;
            CurrentFile = mgm.FilePath;
            CurrentFileName = Path.GetFileName(mgm.FilePath);

            // Base MGM meshes
            foreach (var node in MGMToHelix.CreateMGMNodes(mgm))
            {
                Scene3DView.Scene3D.Add(node);
                Scene3DView.CollectMeshesFrom(node);
            }

            Scene3DView.BoneModel = null;
            Scene3DView.EnsureBones();
            Scene3DView.ApplyBoneVisibility(ShowBones);
            Scene3DView.ApplyMeshWireframe(ShowMeshWireframe);
            Scene3DView.ApplyMeshSolidVisibility(ShowMeshSolid);
            Scene3DView.FrameScene(true);

            IsLoaded();
        }
    }

    #endregion

    #region Navi
    /// <summary>
    /// Loads the Navi file associated with the and updates the 3D scene.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task LoadNaviAsync(ModelType model)
    {
        Scene3DView.ClearScene();
        Message = string.Format(Resources.ModelViewerLoadingModel, "Navi");

        var navi = await Task.Run(() => NaviReader.ReadAsync(model.FilePath)).ConfigureAwait(true);

        if (navi is not null)
        {
            NaviModel = navi;
            MgmModel = null;
            MmpModel = null;
            Scene3DView.NaviModel = navi;
            Version = navi.Header.Version;
            ObjectCount = navi.Entries.Count;
            MaterialCount = 0;
            BoneCount = 0;
            Scene3DView.LoadNavMesh(ShowNaviWireframe, ShowNaviDebug);
            IsNavMeshControlsVisible = Visibility.Visible;

            IsLoaded();
        }
    }

    #endregion

    #region MData

    /// <summary>
    /// Loads the Wdata file path and updates the 3D scene.
    /// </summary>
    /// <param name="mdataPath"></param>
    /// <returns></returns>
    private async Task LoadMDataAsync(ModelType model)
    {
        Scene3DView.ClearScene();
        Message = string.Format(Resources.ModelViewerLoadingModel, "MData");
        var mdata = await Task.Run(() => MDataModelPathReader.ReadAsync(model.FilePath)).ConfigureAwait(true);

        var mgmPath = mdata.MgmPath;
        var rootDir = Path.GetDirectoryName(model.FilePath) ?? string.Empty;
        mgmPath = Path.Combine(rootDir, Path.GetFileName(mgmPath));

        if (!File.Exists(mgmPath))
        {
            RHMessageBoxHelper.ShowOKMessage(
                string.Format(Resources.FileNotFoundMessage, mgmPath),
                Resources.Error
            );
            return;
        }

        var modelType = new ModelType
        {
            Format = ModelFormat.MGM,
            FilePath = mgmPath
        };

        await LoadMGMAsync(modelType);
    }

    #endregion

    #region WData

    /// <summary>
    /// Loads the Wdata file path and updates the 3D scene.
    /// </summary>
    /// <param name="wdataPath"></param>
    /// <returns></returns>
    private async Task LoadWDataAsync(ModelType model)
    {
        Scene3DView.ClearScene();
        Message = string.Format(Resources.ModelViewerLoadingModel, "WData");
        var wdata = await Task.Run(() => WDataReader.ReadAsync(model.FilePath)).ConfigureAwait(true);

        var mmpPath = wdata.ModelPath;
        var rootDir = Path.GetDirectoryName(model.FilePath) ?? string.Empty;
        mmpPath = Path.Combine(rootDir, Path.GetFileName(mmpPath));

        if (!File.Exists(mmpPath))
        {
            RHMessageBoxHelper.ShowOKMessage(
                string.Format(Resources.FileNotFoundMessage, mmpPath),
                Resources.Error
            );
            return;
        }

        var modelType = new ModelType
        {
            Format = ModelFormat.MMP,
            FilePath = mmpPath,
            WData = wdata
        };

        await LoadMMPAsync(modelType);
    }

    #endregion

    #endregion

    #region Settings
    /// <summary>
    /// Sets the folder path for the client assets using a folder dialog.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteSetFolderCommand))]
    public void SetClientAssetsFolder()
    {
        try
        {
            var openFolderDialog = new OpenFolderDialog();

            if (openFolderDialog.ShowDialog() == true)
            {
                string newFolderPath = openFolderDialog.FolderName;
                RegistrySettingsHelper.SetClientAssetsFolder(newFolderPath);

                RHMessageBoxHelper.ShowOKMessage(string.Format(Resources.DataTableManagerFolderSetMessage, newFolderPath), Resources.Success);
            }
        }
        catch (Exception ex)
        {
            RHMessageBoxHelper.ShowOKMessage($"{Resources.Error}: {ex.Message}", Resources.Error);
        }
    }

    /// <summary>
    /// Checks if the SetFolder command can be executed.
    /// </summary>
    /// <returns>True if the command can be executed, otherwise false.</returns>
    private bool CanExecuteSetFolderCommand()
    {
        return MmpModel is null;
    }
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
    /// Clears the current file and resets the model and related properties.
    /// </summary>
    public void ClearFile()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MmpModel = null;
            MgmModel = null;
            NaviModel = null;
            CurrentFile = null;
            CurrentFileName = null;
            IsVisible = Visibility.Hidden;
            Message = Resources.OpenFile;
            IsMessageVisible = Visibility.Visible;
            Title = Resources.ModelViewerTitleDefault;
            Scene3DView.ClearScene();
            OnCanExecuteFileCommandChanged();
        });
        
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

    [ObservableProperty] private string _clientAssetsFolder = RegistrySettingsHelper.GetClientAssetsFolder();
    [ObservableProperty] private Guid _token;
    [ObservableProperty] private string _title = Resources.ModelViewerTitleDefault;
    [ObservableProperty] private Visibility _isVisible = Visibility.Collapsed;
    [ObservableProperty] private Visibility _isMessageVisible = Visibility.Visible;
    [ObservableProperty] private Visibility _isNavMeshControlsVisible = Visibility.Collapsed;
    [ObservableProperty] private Visibility _isBoneControlsVisible = Visibility.Collapsed;
    [ObservableProperty] private Visibility _isMeshControlsVisible = Visibility.Collapsed;
    [ObservableProperty] private Visibility _isPlaneGridVisible = Visibility.Hidden;

    [ObservableProperty] private MmpModel? _mmpModel;
    partial void OnMmpModelChanged(MmpModel? value)
    {
        OnCanExecuteFileCommandChanged();
    }

    [ObservableProperty] private MgmModel? _mgmModel;
    partial void OnMgmModelChanged(MgmModel? value)
    {
        OnCanExecuteFileCommandChanged();
    }

    [ObservableProperty] private NaviMeshFile? _naviModel;
    partial void OnNaviModelChanged(NaviMeshFile? value)
    {
        OnCanExecuteExportHeightCommandChanged();
    }

    [ObservableProperty] private string? _currentFile;
    [ObservableProperty] private string? _currentFileName;
    [ObservableProperty] private string? _message = Resources.OpenFile;
    [ObservableProperty] private int _version;
    [ObservableProperty] private int _objectCount;
    [ObservableProperty] private int _materialCount;
    [ObservableProperty] private int _boneCount;

    [ObservableProperty] private Scene3DManager _scene3DView = new();

    [ObservableProperty] private bool _showNaviWireframe;
    partial void OnShowNaviWireframeChanged(bool value)
    {
        Scene3DView.EnsureNaviWireframe();
        Scene3DView.ApplyNaviWireframeVisibility(value);
    }

    [ObservableProperty] private bool _showNaviDebug;
    partial void OnShowNaviDebugChanged(bool value)
    {
        Scene3DView.EnsureNaviDebug();
        Scene3DView.ApplyNaviDebugVisibility(value);
    }

    [ObservableProperty] private bool _showBones;
    partial void OnShowBonesChanged(bool value)
    {
        Scene3DView.EnsureBones();
        Scene3DView.ApplyBoneVisibility(value);
    }

    [ObservableProperty]private bool _showMeshWireframe;
    partial void OnShowMeshWireframeChanged(bool value)
    {
        Scene3DView.ApplyMeshWireframe(value);
    }

    [ObservableProperty] private bool _showMeshSolid = true;
    partial void OnShowMeshSolidChanged(bool value)
    {
        Scene3DView.ApplyMeshSolidVisibility(value);
    }

    [ObservableProperty] private bool _embedTextures;
    [ObservableProperty] private bool _exportAnimation = false;
    [ObservableProperty] private bool _exportSeparateObjects = false;
    #endregion
}