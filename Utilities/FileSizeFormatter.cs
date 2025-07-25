namespace RHToolkit.Utilities;

public class FileSizeFormatter
{
    public static string FormatFileSize(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        if (bytes >= GB)
            return $"{(bytes / (double)GB):F2} GB";
        if (bytes >= MB)
            return $"{(bytes / (double)MB):F2} MB";
        if (bytes >= KB)
            return $"{(bytes / (double)KB):F2} KB";
        return $"{bytes} B";
    }
}
