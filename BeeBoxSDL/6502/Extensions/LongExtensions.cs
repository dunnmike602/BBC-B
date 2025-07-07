namespace BeeBoxSDL._6502.Extensions;

public static class LongExtensions
{
    public static long RoundToSignificantDigits(this long d, int digits)
    {
        var scale = Math.Pow(10, Math.Floor(Math.Log10(d)) + 1);
        return (long)(scale * Math.Round(d / scale, digits));
    }
}