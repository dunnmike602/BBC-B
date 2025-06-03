namespace MLDComputing.Emulators.BBCSim.Beeb.Hardware;

public class IoDevices(Via6522 via, RomBank romBank, Mc6850Acia acia, SerialULA serialUla)
{
    public byte Read(ushort address)
    {
        if (address >= 0xFE40 && address <= 0xFE4F)
        {
            return via.Read((byte)(address & 0x0F));
        }

        if (address >= 0xFE10 && address <= 0xFE17)
        {
            return serialUla.Read(address);
        }

        if (address == 0xFE08 || address == 0xFE09)
        {
            return acia.Read((byte)(address & 0x0F));
        }

        return 0xFF;
    }

    public void Write(ushort address, byte value)
    {
        if (address >= 0xFE40 && address <= 0xFE4F)
        {
            via.Write((byte)(address & 0x0F), value);
            return;
        }

        if (address == 0xFE30)
        {
            romBank.SelectSlot(value);
            return;
        }

        if (address >= 0xFE10 && address <= 0xFE17)
        {
            serialUla.Write(address, value);
            return;
        }

        if (address == 0xFE08 || address == 0xFE09)
        {
            acia.Write((byte)(address & 0x0F), value);
        }
    }
}