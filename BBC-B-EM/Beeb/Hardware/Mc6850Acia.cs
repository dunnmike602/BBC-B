namespace MLDComputing.Emulators.BBCSim.Beeb.Hardware;

public class Mc6850Acia
{
    // Constants for the ACIA status bits (typical values)
    private const byte Status_TDRE = 0x10; // Transmit Data Register Empty
    private const byte Status_RDRF = 0x02; // Receive Data Register Full
    private byte _control;
    private byte _rxData;
    private byte _status;
    private byte _txData;

    public Mc6850Acia()
    {
        _control = 0;
        _status = Status_TDRE; // Pretend transmitter is always ready
        _txData = 0;
        _rxData = 0xFF; // No input
    }

    public byte Read(byte offset)
    {
        switch (offset & 0x01) // Only two registers: 0 or 1
        {
            case 0x00: return _status; // Status register (FE08)
            case 0x01: return _rxData; // Receive data register (FE09)
            default: return 0xFF; // Not reachable, but safe
        }
    }

    public void Write(byte offset, byte value)
    {
        switch (offset & 0x01)
        {
            case 0x00:
                _control = value;
                if ((_control & 0x80) != 0)
                {
                    _status = 0x10; // Reset status (TDRE set)
                    _rxData = 0xFF; // No input
                }

                break;

            case 0x01:
                _txData = value;
                break;
        }
    }
}