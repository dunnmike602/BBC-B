namespace MLDComputing.Emulators.BBCSim._6502.Extensions;

using System.Runtime.CompilerServices;
using Storage;
using Byte = Storage.Byte;

public static class ByteExtensions
{
    public static byte LowNibble(this byte number)
    {
        return (byte)(number & 0xF);
    }

    public static byte HighNibble(this byte number)
    {
        return (byte)((number & 0xF0) >> 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte SetBit(this byte source, Byte bitToSet, Bit value)
    {
        if (value == Bit.Zero)
        {
            return (byte)(source & ~(1 << (byte)bitToSet));
        }

        return (byte)(source | (1 << (byte)bitToSet));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsZero(this byte source)
    {
        return source == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNegative(this byte source)
    {
        return GetBit(source, Byte.Seven) == Bit.One;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bit GetBit(this byte source, Byte bitToGet)
    {
        return (source & (1 << (int)bitToGet)) == 1 << (int)bitToGet ? Bit.One : Bit.Zero;
    }

    public static string ConvertToBase(this byte source, int radix)
    {
        var valueString = Convert.ToString(source, radix);
        var maxValueString = Convert.ToString(byte.MaxValue, radix);

        return valueString.PadLeft(maxValueString.Length, '0').ToUpper();
    }

    public static string ConvertToBaseWithPrefix(this byte source, int radix)
    {
        var labels = new Dictionary<int, string> { { 2, "b" }, { 8, "o" }, { 10, string.Empty }, { 16, "$" } };

        return labels[radix] + source.ConvertToBase(radix);
    }
}