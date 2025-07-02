namespace MLDComputing.Emulators.BBCSim.Beeb.Hardware;

public class IoDevices(SysVia sysVia, RomBank romBank, Mc6850Acia acia, SerialULA serialUla)
{
    public byte Read(ushort address)
    {
        if (address >= 0xFE40 && address <= 0xFE4F)
        {
            return sysVia.Read((byte)(address & 0x0F)); // System VIA
        }

        if (address >= 0xFE10 && address <= 0xFE17)
        {
            return serialUla.Read(address); // Serial ULA
        }

        if (address == 0xFE08 || address == 0xFE09)
        {
            return acia.Read((byte)(address & 0x0F)); // ACIA
        }

        return 0xFF;
    }

    public void Write(ushort address, byte value)
    {
        if (address >= 0xFE40 && address <= 0xFE4F)
        {
            sysVia.Write((byte)(address & 0x0F), value); // System VIA
            return;
        }

        if (address == 0xFE30)
        {
            romBank.SelectSlot(value); // ROM Bank switching
            return;
        }

        if (address >= 0xFE10 && address <= 0xFE17)
        {
            serialUla.Write(address, value); // Serial ULA
            return;
        }

        if (address == 0xFE08 || address == 0xFE09)
        {
            acia.Write((byte)(address & 0x0F), value); // ACIA
        }
    }
}