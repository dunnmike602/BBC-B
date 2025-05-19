namespace MLDComputing.Emulators.BBCSim._6502.Extensions;

public static class UShortExtensions
{
    public static string ConvertToBase(this ushort source, int radix)
    {
        var valueString = Convert.ToString(source, radix);
        var maxValueString = Convert.ToString(ushort.MaxValue, radix);

        return valueString.PadLeft(maxValueString.Length, '0').ToUpper();
    }

    public static string ConvertToBaseWithPrefix(this ushort source, int radix)
    {
        var labels = new Dictionary<int, string> { { 2, "b" }, { 8, "o" }, { 10, string.Empty }, { 16, "$" } };

        return labels[radix] + source.ConvertToBase(radix);
    }
}