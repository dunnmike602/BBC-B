namespace MLDComputing.Emulators.BeebBox.Extensions;

public static class ByteExtensions
{
    /// <summary>
    ///     Returns true if this byte is in the ASCII printable range (0x20–0x7E).
    /// </summary>
    public static bool IsAsciiPrintable(this byte b)
    {
        return b is >= 0x20 and <= 0x7E;
    }
}