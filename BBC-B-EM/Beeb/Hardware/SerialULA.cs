namespace MLDComputing.Emulators.BBCSim.Beeb.Hardware;

public class SerialULA
{
    private readonly byte[] _registers = new byte[8];

    public byte Read(ushort address)
    {
        var reg = address & 0x0007;

        // Stub behavior: return current value in register (default 0x00)
        return _registers[reg];
    }

    public void Write(ushort address, byte value)
    {
        var reg = address & 0x0007;

        // Stub behavior: store value into register
        _registers[reg] = value;
    }
}