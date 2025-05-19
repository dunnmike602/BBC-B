namespace MLDComputing.Emulators.BBCSim.Beeb;

public class IoDevices
{
    public byte Read(ushort address)
    {
        throw new NotImplementedException();
    }

    public void Write(ushort address, byte value)
    {
        throw new NotImplementedException();
    }
}

public class RomBank
{
    public byte Read(ushort address)
    {
        throw new NotImplementedException();
    }
}

public class MemoryMap
{
    private readonly IoDevices? _io;

    private readonly OsRom? _osRom = new();

    private readonly byte[] _ram = new byte[0x8000]; // 32 KB

    private readonly RomBank? _romBank;

    public byte ReadByte(ushort address)
    {
        return address switch
        {
            < 0x8000 => _ram[address],
            < 0xC000 => _romBank!.Read(address),
            < 0xFC00 => _osRom!.Read(address),
            < 0xFE00 => _io!.Read(address), // I/O region: FC00–FDFF (some overlap by device design)
            _ => _osRom!.Read(address) // FFxx vectors etc.
        };
    }

    public void WriteByte(ushort address, byte value)
    {
        switch (address)
        {
            case < 0x8000:
                _ram[address] = value;
                break;
            case >= 0xFE00:
                _io!.Write(address, value);
                break;
            // ROM and OS ROM are read-only
        }
    }
}