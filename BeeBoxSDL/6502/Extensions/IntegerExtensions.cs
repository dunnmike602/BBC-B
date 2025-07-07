namespace BeeBoxSDL._6502.Extensions;

public static class IntegerExtensions
{
    public static string ConvertToBase(this int source, int radix, int maxValue)
    {
        var valueString = Convert.ToString(source, radix);
        var maxValueString = Convert.ToString(maxValue, radix);

        return valueString.PadLeft(maxValueString.Length, '0').ToUpper();
    }

    public static string? ConvertToBaseWithPrefix(this int source, int radix, int maxValue)
    {
        var labels = new Dictionary<int, string> { { 2, "b" }, { 8, "o" }, { 10, string.Empty }, { 16, "$" } };

        return labels[radix] + source.ConvertToBase(radix, maxValue);
    }

    public static int LowWord(this int number)
    {
        return number & 0x000000FF;
    }

    public static int LowWord(this int number, int newValue)
    {
        return (int)((number & 0xFFFFFF00) + (newValue & 0x000000FF));
    }

    public static int HighWord(this int number)
    {
        return (number & 0x0000FF00) >> 8;
    }

    public static int HighWord(this int number, int newValue)
    {
        return (number & 0x000000FF) + (newValue << 8);
    }
}