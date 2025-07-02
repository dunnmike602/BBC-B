namespace MLDComputing.Emulators.BBCSim.Beeb.Hardware;

public class MemoryMap(RomBank romBank, IoDevices io, OsRom osRom)
{
    private readonly byte[] _ram = new byte[0x8000]; // 0x0000–0x7FFF (32KB RAM)

    private readonly byte[] _videoBuffer = new byte[1024]; // 40 x 25 chars

    public byte[] GetVideoBuffer()
    {
        return _videoBuffer;
    }

    public byte ReadByte(ushort address)
    {
        if (address > 0x4000 && address < 0x6000)
        {
            return _ram[address];
        }

        // 0000–7FFF: main RAM
        if (address < 0x8000)
        {
            return _ram[address];
        }

        // 8000–BFFF: sideways paged ROM
        if (address < 0xC000)
        {
            return romBank.Read(address);
        }

        // C000–FBFF: OS ROM (this includes BASIC, MOS, language ROMs, etc.)
        if (address < 0xFC00)
        {
            return osRom.Read(address);
        }

        // FC00–FDFF: I/O region (Video ULA, VIA, disc controller…)
        if (address < 0xFF00)
        {
            return io.Read(address);
        }

        // FE00–FFFF: vectors and the “top” of the OS ROM
        return osRom.Read(address);
    }

    public void WriteByte(ushort address, byte value)
    {
        switch (address)
        {
            case < 0x8000:
                _ram[address] = value;
                break;
            case >= 0xFE00:
                io.Write(address, value);
                break;
            // Writes to ROM and OS ROM are ignored
        }

        // Intercept screen RAM writes
        if (address >= 0x7C00 && address <= 0x7FFF)
        {
            var offset = address - 0x7C00;
            _videoBuffer[offset] = value;
        }
    }
}