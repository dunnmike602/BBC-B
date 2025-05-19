namespace MLDComputing.Emulators.BBCSim._6502.Extensions;

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Assembler.Constants;

public static class StringExtensions
{
    private const string ValueOnlyRegEx = @"[-a-zA-Z0-9]+";
    private const string ExtractNumber = @"\d+";

    public static string PadNumberForBase(this string source, int radix, long maxValue, long minValue)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            source = "0";
        }

        var maxValueString = Convert.ToString(maxValue, radix);
        var minValueString = Convert.ToString(minValue, radix);

        // Limit to the minumim value allowable
        if (Convert.ToInt64(source, radix) < minValue)
        {
            source = minValueString;
        }

        // Limit to the maximum value allowable
        if (Convert.ToInt64(source, radix) > maxValue)
        {
            source = maxValueString;
        }

        return source.PadLeft(maxValueString.Length, '0').ToUpper();
    }

    public static int ExtractFirstNumber(this string? source, string character)
    {
        var location = source!.IndexOf(character, StringComparison.Ordinal) + 1;

        var numberMatch = Regex.Match(source.Substring(location), ExtractNumber);

        return numberMatch.Success ? Convert.ToInt32(numberMatch.Value) : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte? ConvertToByte(this string? valueText)
    {
        var valueOnly = valueText.GetValueOnly();

        byte value = 0;

        var isValid = false;

        // Handle ascii characters
        if (!string.IsNullOrWhiteSpace(valueText) && valueText.Trim()[0] == '"')
        {
            valueText = ((byte)valueText[1]).ToString();
            valueOnly = valueText;
        }

        switch (valueText.GetBase())
        {
            case NumberBases.Hex:
                isValid = byte.TryParse(valueOnly, NumberStyles.HexNumber, CultureInfo.CurrentCulture,
                    out value);
                break;

            case NumberBases.Decimal:
                isValid = byte.TryParse(valueOnly, out value);
                break;

            case NumberBases.Binary:
                try
                {
                    value = Convert.ToByte(valueOnly, 2);
                    isValid = true;
                }
                catch (Exception)
                {
                    isValid = false;
                }

                break;

            case NumberBases.Octal:
                try
                {
                    value = Convert.ToByte(valueOnly, 8);
                    isValid = true;
                }
                catch (Exception)
                {
                    isValid = false;
                }

                break;
        }

        if (isValid)
        {
            return value;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte? ConvertToSByte(this string? valueText)
    {
        var valueOnly = valueText.GetValueOnly();

        sbyte value = 0;

        var isValid = false;

        switch (valueText.GetBase())
        {
            case NumberBases.Hex:
                isValid = sbyte.TryParse(valueOnly, NumberStyles.HexNumber, CultureInfo.CurrentCulture,
                    out value);
                break;

            case NumberBases.Decimal:
                isValid = sbyte.TryParse(valueOnly, out value);
                break;

            case NumberBases.Binary:
                try
                {
                    value = Convert.ToSByte(valueOnly, 2);
                    isValid = true;
                }
                catch (Exception)
                {
                    isValid = false;
                }

                break;

            case NumberBases.Octal:
                try
                {
                    value = Convert.ToSByte(valueOnly, 8);
                    isValid = true;
                }
                catch (Exception)
                {
                    isValid = false;
                }

                break;
        }

        if (isValid)
        {
            return value;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int? ConvertToInt(this string? valueText)
    {
        var valueOnly = valueText.GetValueOnly()!.Trim();

        var value = 0;

        var isValid = false;

        // Pre-process for characters
        if (valueText!.StartsWith("\""))
        {
            valueOnly = ((int)valueText.Substring(1, 1)[0]).ToString();
        }

        switch (valueText.GetBase())
        {
            case NumberBases.Hex:
                isValid = int.TryParse(valueOnly, NumberStyles.HexNumber, CultureInfo.CurrentCulture,
                    out value);
                break;

            case NumberBases.Decimal:
                isValid = int.TryParse(valueOnly, out value);
                break;

            case NumberBases.Binary:
                try
                {
                    value = Convert.ToInt32(valueOnly, 2);
                    isValid = true;
                }
                catch (Exception)
                {
                    isValid = false;
                }

                break;

            case NumberBases.Octal:
                try
                {
                    value = Convert.ToInt32(valueOnly, 8);
                    isValid = true;
                }
                catch (Exception)
                {
                    isValid = false;
                }

                break;
        }

        if (isValid)
        {
            return value;
        }

        return null;
    }

    /// <summary>
    ///     Strips spaces and any indexing from the argument (i.e ,X or ,Y)
    /// </summary>
    public static string? GetValueOnly(this string? argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            return string.Empty;
        }

        return Regex.Match(argument, ValueOnlyRegEx).Value.Trim();
    }

    public static NumberBases GetBase(this string? argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            return NumberBases.Undefined;
        }

        if (argument.Contains(TokenConstants.BinChar))
        {
            return NumberBases.Binary;
        }

        if (argument.Contains(TokenConstants.HexChar))
        {
            return NumberBases.Hex;
        }

        if (argument.Contains(TokenConstants.OctalChar))
        {
            return NumberBases.Octal;
        }

        return NumberBases.Decimal;
    }
}