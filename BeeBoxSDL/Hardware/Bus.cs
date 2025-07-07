namespace BeeBoxSDL.Hardware;

using Constants;

public class Bus
{
    private readonly byte[] _ram = new byte[GlobalConstants.Memory.DefaultRam]; // 32KB RAM
    private readonly byte[][] _romBanks = new byte[GlobalConstants.Memory.DefaultRomBanks][]; // up to 16 ROM banks
    private int _currentRomBank;

    public SysVia SysVia { get; } = new SysVia();
    public VideoUla VideoUla { get; } = new VideoUla();

    public byte ReadByte(ushort address)
    {
        if (address < GlobalConstants.Memory.DefaultRam)
        {
            return _ram[address];
        }

        if (address < GlobalConstants.Memory.RomEndAddress)
        {
            return _romBanks[_currentRomBank]?[address - GlobalConstants.Memory.DefaultRam] ??
                   GlobalConstants.Memory.FloatingBusValue;
        }

        if (address is >= 0xFC00 and < 0xFE00)
        {
            return GlobalConstants.Memory.FloatingBusValue; // other I/O not yet implemented
        }

        if (address is >= 0xFE00 and < 0xFF00)
        {
            switch (address & 0xFFF0)
            {
                case 0xFE40:
                    return VideoUla.Read((byte)(address & 0x0F));
                case 0xFE00:
                    return SysVia.Read((byte)(address & 0x0F));
            }
        }
        else if (address >= 0xFF00)
        {
            return _romBanks[15]?[address - 0xC000] ?? GlobalConstants.Memory.FloatingBusValue; // OS ROM assumed in bank 15
        }

        return GlobalConstants.Memory.FloatingBusValue;
    }

    public void WriteByte(ushort address, byte value)
    {
        if (address < 0x8000)
        {
            _ram[address] = value;
        }
        else if (address >= 0xFC00 && address < 0xFE00)
        {
            // ignore unimplemented devices
        }
        else if (address >= 0xFE00 && address < 0xFF00)
        {
            if ((address & 0xFFF0) == 0xFE40)
            {
                VideoUla.Write((byte)(address & 0x0F), value);
            }
            else if ((address & 0xFFF0) == 0xFE00)
            {
                SysVia.Write((byte)(address & 0x0F), value);
            }
        }
        // writing to ROM or OS space: ignore
    }

    public void LoadRomBank(int bank, byte[] data)
    {
        _romBanks[bank] = data;
    }

    public void SetCurrentRomBank(int bank)
    {
        _currentRomBank = bank & 0x0F;
    }

    public void Reset()
    {
        Array.Clear(_ram, 0, _ram.Length);
        SysVia.Reset();
        VideoUla.Reset();
    }
}