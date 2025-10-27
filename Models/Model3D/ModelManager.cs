using RHToolkit.Models.Model3D.Map;
using RHToolkit.Models.Model3D.MGM;
using System.Collections.Concurrent;

namespace RHToolkit.Models.Model3D;

/// <summary>
/// Summary of the export operation.
/// </summary>
/// <param name="total"></param>
public sealed class ExportSummary(int total)
{
    public int Total { get; } = total;
    public int Exported { get; private set; }
    public int Skipped => SkippedFiles.Count;
    public List<(string File, string Reason)> SkippedFiles { get; } = [];

    public void AddExported() => Exported++;
    public void AddSkipped(string file, string reason) => SkippedFiles.Add((file, reason));
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

        var files = await Task.Run(() =>
        {
            return Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    return file.EndsWith(".mmp", StringComparison.OrdinalIgnoreCase) ||
                           file.EndsWith(".mgm", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
        }, ct).ConfigureAwait(false);

        await Parallel.ForEachAsync(files, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = ct
        }, (file, token) =>
        {
            try
            {
                var info = new FileInfo(file);
                if (info.Length == 0)
                    return new ValueTask();

                var rel = Path.GetRelativePath(baseDir, file);
                var kind = GetKindFromExtension(file);

                dictionary[rel] = (file, kind);
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            return new ValueTask();
        });

        return new SortedDictionary<string, (string FullPath, ModelKind Kind)>(dictionary, StringComparer.OrdinalIgnoreCase);
    }

    private static ModelKind GetKindFromExtension(string path)
        => Path.GetExtension(path).Equals(".mmp", StringComparison.OrdinalIgnoreCase) ? ModelKind.MMP : ModelKind.MGM;

    /// <summary>
    /// Exports the specified files to FBX format, placing outputs into MMP/ and MGM/ subfolders.
    /// </summary>
    /// <param name="outputDirectory"></param>
    /// <param name="filesToExportDict">Key: relative input path. Value: (full path, kind).</param>
    /// <param name="progress"></param>
    /// <param name="ct"></param>
    public static async Task<ExportSummary> ExportFilesAsync(
        string outputDirectory,
        SortedDictionary<string, (string FullPath, ModelKind Kind)> filesToExportDict,
        IProgress<(string file, int pos, int count)> progress, bool embedTextures,
        CancellationToken ct)
    {
        var summary = new ExportSummary(filesToExportDict.Count);

        try
        {
            await Task.Run(async () =>
            {
                int pos = 0, count = filesToExportDict.Count;

                foreach (var (relPath, entry) in filesToExportDict)
                {
                    ct.ThrowIfCancellationRequested();
                    pos++;
                    progress.Report((relPath, pos, count));

                    try
                    {
                        // Choose reader + output subfolder based on kind
                        string subfolder;
                        string outputRel = Path.ChangeExtension(relPath, ".fbx");

                        switch (entry.Kind)
                        {
                            case ModelKind.MMP:
                                {
                                    var mmpModel = await MMPReader.ReadAsync(entry.FullPath).ConfigureAwait(false);
                                    subfolder = "MMP";
                                    string outputFile = Path.Combine(outputDirectory, subfolder, outputRel);
                                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

                                    var exporter = new MMPExporterAspose();
                                    await exporter.ExportMmpToFbx(mmpModel, outputFile, embedTextures).ConfigureAwait(false);
                                    break;
                                }
                            case ModelKind.MGM:
                                {
                                    var mgmModel = await MGMReader.ReadAsync(entry.FullPath).ConfigureAwait(false);
                                    subfolder = "MGM";
                                    string outputFile = Path.Combine(outputDirectory, subfolder, outputRel);
                                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

                                    var exporter = new MGMExporterAspose();
                                    await exporter.ExportMgmToFbx(mgmModel, outputFile, embedTextures).ConfigureAwait(false);
                                    break;
                                }

                            default:
                                throw new NotSupportedException($"Unsupported file for '{relPath}'.");
                        }

                        summary.AddExported();
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        summary.AddSkipped(relPath, ex.Message);
                    }
                }
            }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }

        return summary;
    }
}
