namespace BBC_B_Tests.TestDoubles;

public class TestMemoryMap
{
    private readonly byte[] _ram = new byte[0x10000]; // 64 KB


    public byte ReadByte(ushort address)
    {
        return _ram[address];
    }

    public void WriteByte(ushort address, byte value)
    {
        _ram[address] = value;
    }
}