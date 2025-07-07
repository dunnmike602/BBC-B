namespace BeeBoxSDL.Hardware;

public class SysVia
{
    private byte _orb;

    public Action<int>? OnRomBankChange;

    public void Write(byte register, byte value)
    {
        switch (register)
        {
            case 0x00: // Port B (ORB)
                _orb = value;
                OnRomBankChange?.Invoke(_orb & 0x0F); // bits 0–3 select ROM bank
                break;

            // handle other registers...
        }
    }

    public byte Read(byte register)
    {
        switch (register)
        {
            case 0x00: return _orb;
            // return other registers as needed
        }

        return 0xFF;
    }
}