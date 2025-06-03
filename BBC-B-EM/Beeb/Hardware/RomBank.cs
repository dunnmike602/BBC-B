namespace MLDComputing.Emulators.BBCSim.Beeb.Hardware;

public class RomBank
{
    private readonly byte[]?[] _romSlots = new byte[16][];
    private byte _currentSlot;

    public void LoadRom(byte[] romData, int slot)
    {
        if (romData.Length != 0x4000)
        {
            throw new ArgumentException("ROM must be 16KB");
        }

        _romSlots[slot] = romData;
    }

    public void LoadRomFromFile(string path, int slot)
    {
        var data = File.ReadAllBytes(path);
        LoadRom(data, slot);
    }

    public void SelectSlot(byte slot)
    {
        _currentSlot = (byte)(slot & 0x0F);
    }

    public byte Read(ushort address)
    {
        var rom = _romSlots[_currentSlot];
        return rom != null
            ? rom[address - 0x8000]
            : (byte)
            // empty slot → open‐bus behavior (usually 0xFF)
            0xFF;
    }
}