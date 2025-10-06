using RHToolkit.Models.Model3D.MMP;
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

public class ModelManager
{
    /// <summary>
    /// Enumerates files in the specified directory.
    /// </summary>
    /// <param name="baseDir"></param>
    /// <param name="pckMap"></param>
    /// <param name="ct"></param>
    /// <returns>A sorted dictionary where the key is the relative file path and the value is the full file path.</returns>
    public static async Task<SortedDictionary<string, string>> EnumerateFilesToExportAsync(
    string baseDir,
    CancellationToken ct)
    {
        var dictionary = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var files = await Task.Run(() =>
        {
            return Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    return file.EndsWith(".mmp", StringComparison.OrdinalIgnoreCase);
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
                var rel = Path.GetRelativePath(baseDir, file);
                var info = new FileInfo(file);

                if (info.Length == 0)
                    return new ValueTask();

                dictionary[rel] = file;
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            return new ValueTask();
        });

        return new SortedDictionary<string, string>(dictionary, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Exports the specified files to FBX format.
    /// </summary>
    /// <param name="outputDirectory"></param>
    /// <param name="filesToExportDict"></param>
    /// <param name="generateTangents"></param>
    /// <param name="embedTextures"></param>
    /// <param name="progress"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<ExportSummary> ExportFilesAsync(
    string outputDirectory,
    SortedDictionary<string, string> filesToExportDict,
    IProgress<(string file, int pos, int count)> progress,
    CancellationToken ct)
    {
        var summary = new ExportSummary(filesToExportDict.Count);

        try
        {
            await Task.Run(async () =>
            {
                int pos = 0, count = filesToExportDict.Count;

                foreach (var (relPath, fullPath) in filesToExportDict)
                {
                    ct.ThrowIfCancellationRequested();
                    pos++;
                    progress.Report((relPath, pos, count));

                    try
                    {
                        var mmpModel = await MMPReader.ReadAsync(fullPath).ConfigureAwait(false);

                        if (mmpModel.Version < 6)
                        {
                            summary.AddSkipped(relPath, $"Unsupported version ({mmpModel.Version})");
                            continue;
                        }

                        // Ensure subfolders exist
                        string fileName = Path.ChangeExtension(relPath, ".fbx");
                        string outputFile = Path.Combine(outputDirectory, fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

                        MMPExporterAspose.ExportMmpToFbx(mmpModel, outputFile);
                        summary.AddExported();
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        // Keep going; record the failure
                        summary.SkippedFiles.Add((relPath, ex.Message));
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