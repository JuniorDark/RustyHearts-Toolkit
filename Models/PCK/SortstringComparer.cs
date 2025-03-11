namespace RHToolkit.Models.PCK;

/// <summary>
/// tree sorting
/// </summary>
public class SortstringComparer : IComparer<string>
{
    // Compares by Height, Length, and Width.
    public int Compare(string? x, string? y)
    {
        if (x == null || y == null)
        {
            throw new ArgumentNullException(x == null ? nameof(x) : nameof(y));
        }

        int x1 = x.IndexOf('.');
        int y1 = y.IndexOf('.');

        if (x1 >= 0 && y1 < 0) return 1;
        if (x1 < 0 && y1 >= 0) return -1;

        int c = x.CompareTo(y);
        return c;
    }
}
