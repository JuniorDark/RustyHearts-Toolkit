using Pfim;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RHToolkit.Models.Model3D;

/// <summary>
/// Provides methods for converting textures between different formats.
/// </summary>
public static class TextureConverter
{
    /// <summary>
    /// Converts a DDS file to PNG format.
    /// </summary>
    /// <param name="ddsPath">The path to the DDS file.</param>
    /// <param name="pngPath">The path where the PNG file will be saved.</param>
    /// <returns>True if the conversion was successful, otherwise false.</returns>
    public static bool ConvertDdsToPng(string ddsPath, string pngPath)
    {
        try
        {
            using var image = Pfimage.FromFile(ddsPath);
            return ConvertDdsToPng(image, pngPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts DDS data to PNG format.
    /// </summary>
    /// <param name="ddsData">The DDS data as a byte array.</param>
    /// <param name="pngPath">The path where the PNG file will be saved.</param>
    /// <returns>True if the conversion was successful, otherwise false.</returns>
    public static bool ConvertDdsToPng(byte[] ddsData, string pngPath)
    {
        try
        {
            using var image = Pfimage.FromStream(new MemoryStream(ddsData));
            return ConvertDdsToPng(image, pngPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts DDS data to PNG format and returns the PNG data.
    /// </summary>
    /// <param name="ddsData">The DDS data as a byte array.</param>
    /// <returns>The PNG data as a byte array, or null if conversion failed.</returns>
    public static byte[]? ConvertDdsToPngData(byte[] ddsData)
    {
        try
        {
            using var image = Pfimage.FromStream(new MemoryStream(ddsData));
            return ConvertDdsToPngData(image);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts a Pfim image to PNG format and saves it to a file.
    /// </summary>
    /// <param name="image">The Pfim image to convert.</param>
    /// <param name="pngPath">The path where the PNG file will be saved.</param>
    /// <returns>True if the conversion was successful, otherwise false.</returns>
    private static bool ConvertDdsToPng(IImage image, string pngPath)
    {
        try
        {
            var pngData = ConvertDdsToPngData(image);
            if (pngData != null)
            {
                File.WriteAllBytes(pngPath, pngData);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a Pfim image to PNG format and returns the PNG data.
    /// </summary>
    /// <param name="image">The Pfim image to convert.</param>
    /// <returns>The PNG data as a byte array, or null if conversion failed.</returns>
    private static byte[]? ConvertDdsToPngData(IImage image)
    {
        try
        {
            var data = image.Data;
            var stride = image.Stride;
            var width = image.Width;
            var height = image.Height;

            using var ms = new MemoryStream();

            // Handle different pixel formats
            switch (image.Format)
            {
                case ImageFormat.Rgba32:
                    {
                        using var img = Image.LoadPixelData<Bgra32>(data, width, height);
                        img.SaveAsPng(ms);
                        return ms.ToArray();
                    }
                case ImageFormat.Rgb24:
                    {
                        using var img = Image.LoadPixelData<Bgr24>(data, width, height);
                        img.SaveAsPng(ms);
                        return ms.ToArray();
                    }
                case ImageFormat.Rgb8:
                    {
                        // Convert 8-bit to 24-bit RGB
                        var rgb24Data = new byte[width * height * 3];
                        for (int i = 0; i < width * height; i++)
                        {
                            rgb24Data[i * 3] = data[i];
                            rgb24Data[i * 3 + 1] = data[i];
                            rgb24Data[i * 3 + 2] = data[i];
                        }
                        using var img = Image.LoadPixelData<Bgr24>(rgb24Data, width, height);
                        img.SaveAsPng(ms);
                        return ms.ToArray();
                    }
                default:
                    // Try to handle as RGBA32 by default
                    {
                        using var img = Image.LoadPixelData<Bgra32>(data, width, height);
                        img.SaveAsPng(ms);
                        return ms.ToArray();
                    }
            }
        }
        catch
        {
            return null;
        }
    }
}
