namespace BeeBoxSDL._6502.Assembler.Constants;

public static class TokenConstants
{
    public const string CommentStartChar = ";";
    public const string LabelEndChar = ":";
    public const string HexChar = "$";
    public const string BinChar = "%";
    public const string OctalChar = "@";
    public const string OffsetPlusMarker = "*+";
    public const string OffsetMinusMarker = "*-";
    public const int OpCodeSize = 3;
}