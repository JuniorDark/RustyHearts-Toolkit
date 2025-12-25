using RHToolkit.Models.Model3D.Map;
using RHToolkit.Models.Model3D.MGM;
using RHToolkit.Models.WDATA;
using static RHToolkit.Models.Model3D.Map.MMP;
using static RHToolkit.Models.Model3D.Model.MDataModelPathReader;

namespace RHToolkit.Models;

/// <summary>
/// Enumeration of supported model formats.
/// </summary>
public enum ModelFormat
{
    MMP,
    MGM,
    NAVI,
    MDATA,
    WDATA
}

/// <summary>
/// Represents a model type that can hold different file model formats.
/// </summary>
public class ModelType
{
    public string FilePath { get; set; } = string.Empty;
    public ModelFormat Format { get; set; }
    public MmpModel? Mmp { get; set; }
    public MgmModel? Mgm { get; set; }
    public NaviMeshFile? Navi { get; set; }
    public MDataModel? MData { get; set; }
    public WData? WData { get; set; }
}
