using System.Numerics;

namespace RHToolkit.Models.Model3D.Animation;

#region Data Models
public sealed class DSFile
{
    public string BaseDirectory { get; set; } = string.Empty;
    public List<DSAnimation> Animations { get; } = [];
}

public sealed class DSAnimation
{
    public string Name { get; set; } = string.Empty;
    public List<DSTrack> Tracks { get; } = [];
}

public sealed class DSTrack
{
    public string BoneName { get; set; } = string.Empty;
    public List<DSFrame> Frames { get; } = [];
}

public sealed class DSFrame
{
    public float Time;     // absolute seconds
    public Matrix4x4 M;    // column-major
}

#endregion

/// <summary>
/// Reader for .ds (dummy bone animation) files.
/// </summary>
public class DSReader
{
    public async Task<DSFile> ReadAsync(string path, CancellationToken ct = default)
    {
        await using var fs = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);

        using var br = new BinaryReader(fs, Encoding.ASCII, leaveOpen: false);
        return await Task.Run(() => ReadDS(br), ct);
    }

    public static DSFile ReadDS(BinaryReader br)
    {
        long start = br.BaseStream.Position;

        var file = new DSFile();

        uint animCount = br.ReadUInt32();

        for (int a = 0; a < animCount; a++)
        {
            string name = BinaryReaderExtensions.ReadUnicode256Count(br);
            uint blockSize = br.ReadUInt32(); // size of this animation block in bytes
            uint boneCount = br.ReadUInt32();

            var anim = new DSAnimation { Name = name};

            for (int b = 0; b < boneCount; b++)
            {
                string partName = BinaryReaderExtensions.ReadUnicode256Count(br);
                uint frameCount = br.ReadUInt32();

                var bone = new DSTrack { BoneName = partName };

                for (int f = 0; f < frameCount; f++)
                {
                    // absolute time (seconds)
                    float time = br.ReadSingle();
                    // column-major 4x4 matrix
                    var m = BinaryReaderExtensions.ReadMatrix4x4(br, true);

                    bone.Frames.Add(new DSFrame { Time = time, M = m });
                }

                anim.Tracks.Add(bone);
            }

            file.Animations.Add(anim);
        }

        int consumed = (int)(br.BaseStream.Position - start);
        int remain = (int)Math.Max(0, br.BaseStream.Position - consumed);

        return file;
    }

}
