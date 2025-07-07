namespace BeeBoxSDL._6502.Extensions;

public static class Int16Extensions
{
    public static byte LowByte(this short number)
    {
        return (byte)(number & 0x00FF);
    }

    public static byte HighByte(this short number)
    {
        return (byte)((number & 0xFF00) >> 8);
    }

    public static byte LowByte(this ushort number)
    {
        return (byte)(number & 0x00FF);
    }

    public static byte HighByte(this ushort number)
    {
        return (byte)((number & 0xFF00) >> 8);
    }
}