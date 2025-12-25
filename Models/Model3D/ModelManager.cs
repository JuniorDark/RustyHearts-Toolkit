using RHToolkit.Models.Model3D.Map;
using RHToolkit.Models.Model3D.MGM;
using System.Collections.Concurrent;

namespace RHToolkit.Models.Model3D;

public sealed record SkippedFile(string File, string Reason);

public sealed class ExportSummary(int total)
{
    public int Total { get; } = total;
    public int Exported => _exported;
    private int _exported;
    public int Skipped => SkippedFiles.Count;
    public ConcurrentBag<SkippedFile> SkippedFiles { get; } = [];

    public void AddExported() => Interlocked.Increment(ref _exported);
    public void AddSkipped(string file, string reason) => SkippedFiles.Add(new(file, reason));
}

public enum ModelKind { MMP, MGM }

public static class ModelManager
{
    /// <summary>
    /// Enumerates .mmp and .mgm files in the specified directory.
    /// </summary>
    /// <param name="baseDir"></param>
    /// <param name="ct"></param>
    /// <returns>
    /// A sorted dictionary where the key is the relative file path and the value is (full path, kind).
    /// </returns>
    public static async Task<SortedDictionary<string, (string FullPath, ModelKind Kind)>> EnumerateFilesToExportAsync(
    string baseDir,
    CancellationToken ct)
    {
        var dictionary = new ConcurrentDictionary<string, (string FullPath, ModelKind Kind)>(StringComparer.OrdinalIgnoreCase);

        var files = Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories)
                             .Where(file =>
                                 file.EndsWith(".mmp", StringComparison.OrdinalIgnoreCase) ||
                                 file.EndsWith(".mgm", StringComparison.OrdinalIgnoreCase));

        await Parallel.ForEachAsync(
            files,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct
            },
            (file, token) =>
            {
                try
                {
                    var info = new FileInfo(file);
                    if (info.Length == 0)
                        return ValueTask.CompletedTask;

                    var rel = Path.GetRelativePath(baseDir, file);
                    var kind = GetKindFromExtension(file);

                    dictionary[rel] = (file, kind);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }

                return ValueTask.CompletedTask;
            });

        return new SortedDictionary<string, (string FullPath, ModelKind Kind)>(dictionary, StringComparer.OrdinalIgnoreCase);
    }

    private static ModelKind GetKindFromExtension(string path)
        => Path.GetExtension(path).Equals(".mmp", StringComparison.OrdinalIgnoreCase) ? ModelKind.MMP : ModelKind.MGM;

    /// <summary>
    /// Exports the specified files to FBX format, placing outputs into MMP/ and MGM/ subfolders.
    /// </summary>
    /// <param name="outputDirectory">The output directory for exported files.</param>
    /// <param name="filesToExportDict">Dictionary of files to export.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="embedTextures">Whether to embed textures in the FBX.</param>
    /// <param name="copyTextures">Whether to copy textures alongside the FBX.</param>
    /// <param name="exportSeparateObjects">For MMP files, whether to export each object as a separate FBX.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<ExportSummary> ExportFilesAsync(
    string outputDirectory,
    SortedDictionary<string, (string FullPath, ModelKind Kind)> filesToExportDict,
    IProgress<(string file, int pos, int count)> progress,
    bool embedTextures, bool copyTextures, bool exportSeparateObjects,
    CancellationToken ct)
    {
        var summary = new ExportSummary(filesToExportDict.Count);
        var count = filesToExportDict.Count;
        var pos = 0;

        // Cache subfolder paths to reduce string concatenations
        string mmpPath = Path.Combine(outputDirectory, "MMP");
        string mgmPath = Path.Combine(outputDirectory, "MGM");

        try
        {
            await Parallel.ForEachAsync(filesToExportDict,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = ct
                },
                async (kvp, token) =>
                {
                    ct.ThrowIfCancellationRequested();

                    var relPath = kvp.Key;
                    var (fullPath, kind) = kvp.Value;

                    progress?.Report((relPath, Interlocked.Increment(ref pos), count));

                    try
                    {
                        string outputRel = Path.ChangeExtension(relPath, ".fbx");
                        string subfolderPath = kind == ModelKind.MMP ? mmpPath : mgmPath;
                        string outputFile = Path.Combine(subfolderPath, outputRel);

                        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

                        switch (kind)
                        {
                            case ModelKind.MMP:
                                {
                                    var mmpModel = await MMPReader.ReadAsync(fullPath, token).ConfigureAwait(false);
                                    await MMPExporter.ExportMmpToFbx(mmpModel, outputFile, embedTextures, copyTextures, exportSeparateObjects, token)
                                                     .ConfigureAwait(false);
                                    break;
                                }
                            case ModelKind.MGM:
                                {
                                    var mgmModel = await MGMReader.ReadAsync(fullPath, token).ConfigureAwait(false);
                                    await MGMExporter.ExportMgmToFbx(mgmModel, outputFile, embedTextures, exportAnimation: false, copyTextures, token)
                                                     .ConfigureAwait(false);
                                    break;
                                }
                            default:
                                throw new NotSupportedException($"Unsupported file for '{relPath}'.");
                        }
                        summary.AddExported();
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        summary.AddSkipped(relPath, ex.Message);
                    }
                });
        }
        catch (OperationCanceledException)
        {
            // Return partial summary
        }

        return summary;
    }

}
