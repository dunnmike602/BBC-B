namespace MLDComputing.Emulators.BBCSim.Beeb.Hardware;

using _6502.Constants;

public class Via6522(
    Keyboard? keyboard,
    Action<bool> capsLockHandler,
    Action<ushort> videoBaseHandler,
    Action<bool> autoScanHandler,
    Action<bool> speechHandler,
    Action<bool> soundHandler)
{
    private const byte Timer1InterruptBit = 0x40;

    private byte _acr;
    private Action? _clearIrq;
    private byte _ddra, _ddrb;

    private byte _ifr, _ier;
    private byte _ora, _orb;
    private byte _pcr;

    private Action? _raiseIrq;
    private bool _timer1Continuous;

    private ushort _timer1Counter;
    private bool _timer1Enabled;
    private ushort _timer1Latch;

    public ushort VideoBase { get; private set; } = 0xA000;

    public void SetInterruptHandlers(Action raiseIrq, Action clearIrq)
    {
        _raiseIrq = raiseIrq;
        _clearIrq = clearIrq;
    }

    public void Tick()
    {
        if (!_timer1Enabled)
        {
            return;
        }

        // Decrement timer first
        _timer1Counter--;

        // Check for underflow
        if (_timer1Counter == 0xFFFF) // 16-bit wrap-around after hitting zero
        {
            // Simulate keyboard auto-scan on timer interrupt
            keyboard?.TickAutoScan();

            // Set Timer 1 interrupt flag in IFR (bit 6)
            _ifr = (byte)(_ifr | (Timer1InterruptBit & 0x7F));

            // Raise IRQ if enabled in IER
            if ((_ier & Timer1InterruptBit) != 0)
            {
                _raiseIrq?.Invoke();
            }

            // Reload timer if continuous mode is set (ACR bit 6)
            if (_timer1Continuous)
            {
                _timer1Counter = _timer1Latch;
            }
            else
            {
                _timer1Enabled = false;
            }
        }
    }


    private void CheckInterrupts()
    {
        var active = (_ifr & _ier & 0x7F) != 0;
        if (active)
        {
            _raiseIrq?.Invoke();
        }
        else
        {
            _clearIrq?.Invoke();
        }
    }

    public byte Read(byte reg)
    {
        reg &= 0x0F;
        switch (reg)
        {
            // Port B - not used for keyboard scanning; return output value with unused bits high
            case ProcessorConstants.ViaRegisters.PortBRegisterOffset00:
                // Ensure only input bits can be forced high by the emulator
                var result = (byte)(_orb | ~_ddrb);

                // Force bit 2 high to indicate teletext hardware is present
                result |= 0x04;

                return result;

            case ProcessorConstants.ViaRegisters.PortARegisterNoHandshakeOffset0F:
            {
                if (keyboard!.HasLatchedKey)
                {
                    var latchedKey = keyboard.LatchedKeyNumber;

                    // Check if that key is still physically held down
                    if (!keyboard.IsKeyStillHeld(latchedKey))
                    {
                        keyboard.ClearLatchedKeyNumber(); // clear once key is no longer held
                        return 0x00;
                    }

                    return (byte)(0x80 | (latchedKey & 0x7F));
                }

                return 0x00;
            }

            case ProcessorConstants.ViaRegisters.PortARegisterOffset01:
                return 0xFF; // Or optionally duplicate logic if needed

            case ProcessorConstants.ViaRegisters.DataDirectionRegisterBOffset02:
                return _ddrb;

            case ProcessorConstants.ViaRegisters.DataDirectionRegisterAOffset03:
                return _ddra;

            case ProcessorConstants.ViaRegisters.Timer1LowCounterOffset04:
                _clearIrq?.Invoke();
                _ifr = (byte)(_ifr & ~Timer1InterruptBit & 0xFF); // Clear bit 6 in IFR (Timer 1 interrupt flag)
                return (byte)(_timer1Latch & 0xFF);

            case ProcessorConstants.ViaRegisters.Timer1HighCounterOffset05:
                _clearIrq?.Invoke(); // Drop IRQ line
                _ifr = (byte)(_ifr & ~Timer1InterruptBit & 0xFF); // Clear bit 6 in IFR (Timer 1 interrupt flag)
                return (byte)(_timer1Latch >> 8);

            case ProcessorConstants.ViaRegisters.AuxiliaryControlRegisterOffset06:
                return _acr;

            case ProcessorConstants.ViaRegisters.InterruptFlagRegisterOffset0D:
                return (byte)(_ifr | ((_ifr & _ier & 0x7F) != 0 ? 0x80 : 0x00));

            case ProcessorConstants.ViaRegisters.InterruptEnableRegisterOffset0E:
                return _ier;


            default:
                return 0xFF;
        }
    }

    public void Write(byte reg, byte value)
    {
        reg &= 0x0F;
        switch (reg)
        {
            case ProcessorConstants.ViaRegisters.Timer1LowCounterOffset04:
                _timer1Counter = (ushort)((_timer1Counter & 0xFF00) | value);
                break;

            // Timer 1 High Counter (FE45) - Starts timer
            case ProcessorConstants.ViaRegisters.Timer1HighCounterOffset05:
                _timer1Counter = (ushort)((value << 8) | (_timer1Counter & 0x00FF));
                _timer1Enabled = true;
                _timer1Continuous = (_acr & 0x40) != 0;
                _ifr = (byte)(_ifr & ~Timer1InterruptBit & 0xFF); // Clear bit 6 in IFR (Timer 1 interrupt flag)
                _clearIrq?.Invoke(); // Drop IRQ line
                break;

            // Timer 1 Latch Low (FE46)
            case ProcessorConstants.ViaRegisters.Timer1LatchLowOffset06:
                _timer1Latch = (ushort)((_timer1Latch & 0xFF00) | value);
                break;

            // Timer 1 Latch High (FE47) — optionally reload
            case ProcessorConstants.ViaRegisters.Timer1LatchHighOffset07:
                _timer1Latch = (ushort)((value << 8) | (_timer1Latch & 0x00FF));

                // Optional reload on full latch write
                if (!_timer1Enabled)
                {
                    _timer1Counter = _timer1Latch;
                }

                break;

            case ProcessorConstants.ViaRegisters.PortBRegisterOffset00:
                _orb = value;
                HandlePortB();
                break;

            case ProcessorConstants.ViaRegisters.PortARegisterOffset01:
            case ProcessorConstants.ViaRegisters.PortARegisterNoHandshakeOffset0F:
                _ora = value;
                keyboard?.SetRowMask(value);

                break;

            case ProcessorConstants.ViaRegisters.AuxiliaryControlRegisterOffset06:
                _acr = value;
                _timer1Continuous = (value & 0x40) != 0;
                break;

            case ProcessorConstants.ViaRegisters.DataDirectionRegisterBOffset02:
                _ddrb = value;
                HandlePortB();
                break;

            case ProcessorConstants.ViaRegisters.DataDirectionRegisterAOffset03:
                _ddra = value;
                break;

            case ProcessorConstants.ViaRegisters.InterruptEnableRegisterOffset0E:
                if ((value & 0x80) != 0)
                {
                    _ier |= (byte)(value & 0x7F);
                }
                else
                {
                    _ier &= (byte)~(value & 0x7F);
                }

                CheckInterrupts();
                break;

            case ProcessorConstants.ViaRegisters.PeripheralControlRegisterOffset0C:
                _pcr = value;
                // Optional: parse bits and set internal flags if you emulate handshake timing
                break;
        }
    }

    private void HandlePortB()
    {
        var portB = (byte)(_orb | ~_ddrb);

        capsLockHandler((portB & 0x01) != 0);

        var page1 = (portB & 0x02) != 0;
        VideoBase = page1 ? (ushort)0x3000 : (ushort)0xA000;
        videoBaseHandler(VideoBase);

        autoScanHandler((portB & 0x04) != 0);

        var speechOn = (portB & 0x08) != 0;
        speechHandler(speechOn);
        soundHandler(!speechOn);
    }

    public void Reset()
    {
        _ora = _orb = 0;
        _ddra = _ddrb = 0;
        _timer1Latch = _timer1Counter = 0;
        _timer1Enabled = false;
        _timer1Continuous = false;
        _acr = 0;
        _ifr = 0;
        _ier = 0;
    }
}