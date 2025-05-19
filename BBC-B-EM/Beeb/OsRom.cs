namespace MLDComputing.Emulators.BBCSim.Beeb;

public class OsRom
{
    private const int DefaultSize = 0x4000; // 16 KB for BBC Model B OS ROM
    private readonly byte[] _rom;

    /// <summary>
    ///     Create an OS ROM with a default 16KB ROM filled with 0x00.
    /// </summary>
    public OsRom() : this(new byte[DefaultSize])
    {
    }

    /// <summary>
    ///     Create an OS ROM from a custom byte array.
    /// </summary>
    /// <param name="rom">ROM contents (usually 16KB for BBC Model B)</param>
    public OsRom(byte[] rom)
    {
        _rom = rom ?? throw new ArgumentNullException(nameof(rom));
    }

    public int Size => _rom.Length;

    /// <summary>
    ///     Create an OS ROM from a ROM file.
    /// </summary>
    /// <param name="filePath">Path to ROM image file</param>
    public static OsRom LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("ROM file not found", filePath);
        }

        var data = File.ReadAllBytes(filePath);
        return new OsRom(data);
    }

    /// <summary>
    ///     Read a byte from OS ROM (valid from 0xC000 to 0xFFFF inclusive).
    /// </summary>
    public byte Read(ushort address)
    {
        if (address < 0xC000 || address > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(address),
                $"Address {address:X4} is outside OS ROM range (C000–FFFF)");
        }

        var offset = address - 0xC000;

        if (offset >= _rom.Length)
        {
            // Handle truncated ROMs gracefully
            return 0x00;
        }

        return _rom[offset];
    }

    /// <summary>
    ///     Attempting to write to ROM throws.
    /// </summary>
    public void Write(ushort address, byte value)
    {
        throw new InvalidOperationException($"Cannot write to OS ROM at address {address:X4}");
    }
}