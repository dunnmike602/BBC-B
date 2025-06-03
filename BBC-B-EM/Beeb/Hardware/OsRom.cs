namespace MLDComputing.Emulators.BBCSim.Beeb.Hardware;

public class OsRom
{
    private byte[] _data = new byte[0x4000]; // 16KB = $C000–$FFFF

    public void LoadFromFile(string path)
    {
        var data = File.ReadAllBytes(path);

        if (data.Length != 0x4000)
        {
            throw new ArgumentException("OS ROM must be exactly 16KB (0x4000 bytes)");
        }

        _data = data;
    }

    public byte Read(ushort address)
    {
        var returnValue = address < 0xC000
            ? (byte)0xFF
            : // Out of range: unmapped open bus
            _data[address - 0xC000];

        return returnValue;
    }
}