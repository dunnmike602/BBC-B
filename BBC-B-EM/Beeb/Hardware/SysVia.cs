namespace MLDComputing.Emulators.BBCSim.Beeb.Hardware;

using _6502.Constants;

public class SysVia
{
    private readonly bool[] _joystickButton = [false, false];
    private readonly bool[,] _keyboardState = new bool[16, 8];
    private readonly Via6522 _via;

    private Action<bool>? _autoScanHandler;

    private bool _ca1Line = true;

    // Fire button for joystick 1 and 2, false=not pressed, true=pressed
    private int _ca1ReleaseDelay;
    private Action? _cancelIRQ;
    private Action<bool>? _capsLockHandler;

    private int _kbdCol, _kbdRow;
    private bool _keyUpHappened;

    private Action? _raiseIRQ;
    private Action<bool>? _shiftLockHandler;

    // Last value written to the slow data bus - sound reads it later
    private byte _slowDataBusWriteValue;
    private Action<bool>? _soundHandler;
    private Action<bool>? _speechHandler;
    private ushort _videoBase;
    private Action<ushort>? _videoBaseHandler;

    public SysVia(Via6522 via)
    {
        _via = via;
        _via.Reset();
    }

    private int? LatchedKeycode { get; set; }

    private int KeysDown { get; set; }

    private void SlowDataBusWrite(byte value)
    {
        _slowDataBusWriteValue = value;

        if ((_via.IC32State & MachineConstants.Ic32Constants.Ic32KeyboardWrite) == 0)
        {
            // kbd write
            _kbdRow = (value >> 4) & 7;
            _kbdCol = value & 0xf;
            DoKbdIntCheck(); /* Should really only if write enable on KBD changes */
        }

        if ((_via.IC32State & MachineConstants.Ic32Constants.Ic32SoundWrite) == 0)
        {
            // TODO Sound implementation
            //Sound_RegWrite(_slowDataBusWriteValue);
        }
    }

    public void Reset()
    {
        _via.Reset();
        ReleaseAllKeys();
        LatchedKeycode = null;
    }

    public void Tick(uint ncycles)
    {
        _via.Timer1Counter -= (int)ncycles;

        if ((_via.ACR & 0x20) == 0)
        {
            _via.Timer2Counter -= (int)ncycles;
        }

        if (_via.Timer1Counter < 0 || _via.Timer2Counter < 0)
        {
            TickReal();
        }

        DoKbdIntCheck();
    }

    private void TickReal()
    {
        var t1int = false;

        if (_ca1ReleaseDelay > 0)
        {
            _ca1ReleaseDelay--;
            if (_ca1ReleaseDelay == 0)
            {
                SetCA1Line(true); // restore CA1 to high
            }
        }

        if (_via.Timer1Counter < -2 && !t1int)
        {
            t1int = true;

            if (!_via.Timer1HasFinished || (_via.ACR & 0x40) != 0)
            {
                _via.IFR |= 0x40; // Timer 1 interrupt
                UpdateIFRTopBit();

                if ((_via.ACR & 0x80) != 0)
                {
                    _via.ORB ^= 0x80; // Toggle PB7
                    _via.IRB ^= 0x80; // Toggle PB7
                }

                _via.Timer1HasFinished = true;
            }
        }

        if (_via.Timer1Counter < -3)
        {
            _via.Timer1Counter += _via.Timer1Latch * 2 + 4;
            t1int = false;
        }

        if (_via.Timer2Counter < -2)
        {
            if (!_via.Timer2HasFinished)
            {
                // DebugTrace("SysVia timer2 int at %d\n", TotalCycles);
                _via.IFR |= 0x20; // Timer 2 interrupt
                UpdateIFRTopBit();


                _via.Timer2HasFinished = true;
            }
        }

        if (_via.Timer2Counter < -3)
        {
            _via.Timer2Counter += 0x20000; // Do not reload latches for T2
        }
    }

    public byte Read(int offset)
    {
        byte returnValue = 0xff;

        switch (offset)
        {
            case 0: // IRB read
                // Clear bit 4 of IFR from AtoD Conversion
                _via.IFR &= MachineConstants.BitPatterns.ClearBit4; // Also clears bit 4

                returnValue = (byte)(_via.ORB & _via.DDRB);

                if (!_joystickButton[1])
                {
                    returnValue |= 0x20;
                }

                if (!_joystickButton[0])
                {
                    returnValue |= 0x10;
                }

                // TODO If Speech Enabled code required here
                UpdateIFRTopBit();
                break;

            case 2:
                returnValue = _via.DDRB;
                break;

            case 3:
                returnValue = _via.DDRA;
                break;

            case 4: // Timer 1 lo counter
                if (_via.Timer1Counter < 0)
                {
                    returnValue = 0xff;
                }
                else
                {
                    returnValue = (byte)((_via.Timer1Counter / 2) & 0xff);
                }

                _via.IFR &= 0xbf; // Clear bit 6 - timer 1
                UpdateIFRTopBit();
                break;

            case 5: // Timer 1 hi counter
                returnValue = (byte)((_via.Timer1Counter >> 9) & 0xff);
                break;

            case 6: // Timer 1 lo latch
                returnValue = (byte)(_via.Timer1Latch & 0xff);
                break;

            case 7: // Timer 1 hi latch
                returnValue = (byte)((_via.Timer1Latch >> 8) & 0xff);
                break;

            case 8: // Timer 2 lo counter
                if (_via.Timer2Counter < 0) // Adjust for dividing -ve count by 2
                {
                    returnValue = (byte)(((_via.Timer2Counter - 1) / 2) & 0xff);
                }
                else
                {
                    returnValue = (byte)((_via.Timer2Counter / 2) & 0xff);
                }

                _via.IFR &= 0xdf; // Clear bit 5 - timer 2
                UpdateIFRTopBit();
                break;

            case 9: // Timer 2 hi counter
                returnValue = (byte)((_via.Timer2Counter >> 9) & 0xff);
                break;

            case 10:
                //returnValue = SRData;
                // TODO Serial interface
                returnValue = 0;
                break;

            case 11:
                returnValue = _via.ACR;
                break;

            case 12:
                returnValue = _via.PCR;
                break;

            case 13:
                UpdateIFRTopBit();

                returnValue = _via.IFR;
                break;

            case 14:
                returnValue = (byte)(_via.IER | 0x80);
                break;

            case 1:
                _via.IFR &= 0xfc;
                UpdateIFRTopBit();
                returnValue = SlowDataBusRead();
                break;
            case 15:
                // slow data bus read
                returnValue = SlowDataBusRead();
                break;
        }

        return returnValue;
    }

    private bool KbdOP()
    {
        // Check range validity
        if (_kbdCol > 14 || _kbdRow > 7)
        {
            return false; // Key not down if overrange - perhaps we should do something more?
        }

        return _keyboardState[_kbdCol, _kbdRow];
    }

    private byte SlowDataBusRead()
    {
        var result = (byte)(_via.ORA & _via.DDRA);

        if ((_via.IC32State & MachineConstants.Ic32Constants.Ic32KeyboardWrite) == 0)
        {
            if (LatchedKeycode.HasValue)
            {
                result = (byte)(0x80 | (LatchedKeycode.Value & 0x7F));
                ClearLatchedKeyIfNotHeld(); // ✅ safe latch clearing
            }
            else
            {
                result &= 0x7F; // no key latched
            }
        }

        // TODO MOre speech stuff here

        return result;
    }

    public void Write(int offset, byte value)
    {
        switch (offset)
        {
            case 0: // ORB (Output Register B last value written to port b
                // Clear bit 4 of IFR from AtoD Conversion
                _via.IFR &= MachineConstants.BitPatterns.ClearBit4; // Also clears bit 4

                _via.ORB = value;

                // The bottom 4 bits of ORB connect to the IC32 latch.
                IC32Write(value);

                if ((_via.IFR & 8) == 1 && (_via.PCR & 0x20) == 0)
                {
                    _via.IFR &= 0xf7;
                    UpdateIFRTopBit();
                }

                _via.IFR &= MachineConstants.BitPatterns.ClearBit4;
                UpdateIFRTopBit();
                break;

            case 1: // ORA
                _via.ORA = value;
                SlowDataBusWrite(value);
                _via.IFR &= 0xfc;
                UpdateIFRTopBit();
                break;

            case 2:
                _via.DDRB = value;
                break;

            case 3:
                _via.DDRA = value;
                break;

            case 4:
            case 6:
                _via.Timer1Latch &= 0xff00;
                _via.Timer1Latch |= value;
                break;

            case 5:
                _via.Timer1Latch &= 0xff;
                _via.Timer1Latch |= value << 8;
                _via.Timer1Counter = _via.Timer1Latch * 2 + 1;
                _via.IFR &= 0xbf; // clear timer 1 ifr
                // If PB7 toggling enabled, then lower PB7 now
                if ((_via.ACR & 0x80) != 0)
                {
                    _via.ORB &= 0x7f;
                    _via.IRB &= 0x7f;
                }

                UpdateIFRTopBit();
                _via.Timer1HasFinished = false;
                break;

            case 7:
                _via.Timer1Latch &= 0xff;
                _via.Timer1Latch |= value << 8;
                _via.IFR &= 0xbf; // clear timer 1 ifr (this is what Model-B does)
                UpdateIFRTopBit();
                break;

            case 8:
                _via.Timer2Latch &= 0xff00;
                _via.Timer2Latch |= value;
                break;

            case 9:
                _via.Timer2Latch &= 0xff;
                _via.Timer2Latch |= value << 8;
                _via.Timer2Counter = _via.Timer2Latch * 2 + 1;
                if (_via.Timer2Counter == 0)
                {
                    _via.Timer2Counter = 0x20000;
                }

                _via.IFR &= 0xdf; // Clear timer 2 IFR
                UpdateIFRTopBit();
                _via.Timer2HasFinished = false;
                break;

            case 10:
                // TODO Used by serial protocols
                //SRData = Value;
                break;

            case 11:
                _via.ACR = value;
                // TODO Used by serial protocols
                //SRMode = (Value >> 2) & 7;
                break;

            case 12:
                _via.PCR = value;

                if ((value & MachineConstants.PcrControl.PcrCA2Control) == MachineConstants.PcrControl.PcrCA2OutputHigh)
                {
                    _via.CA2 = true;
                }
                else if ((value & MachineConstants.PcrControl.PcrCA2Control) ==
                         MachineConstants.PcrControl.PcrCA2OutputLow)
                {
                    _via.CA2 = false;
                }

                if ((value & MachineConstants.PcrControl.PcrCb2Control) == MachineConstants.PcrControl.PcrCb2OutputHigh)
                {
                    if (!_via.CB2)
                    {
                        // TODO Light pen strobe on CB2 low -> high transition
                        //VideoLightPenStrobe();
                    }

                    _via.CB2 = true;
                }
                else if ((value & MachineConstants.PcrControl.PcrCb2Control) ==
                         MachineConstants.PcrControl.PcrCb2OutputLow)
                {
                    _via.CB2 = false;
                }

                break;

            case 13:
                _via.IFR &= (byte)~value;
                UpdateIFRTopBit();
                break;

            case 14:

                if ((value & 0x80) != 0)
                {
                    _via.IER |= value;
                }
                else
                {
                    _via.IER &= (byte)~value;
                }

                _via.IER &= 0x7f;
                UpdateIFRTopBit();
                break;

            case 15:
                _via.ORA = value;
                SlowDataBusWrite(value);
                break;
        }
    }

    public void SetCA1Line(bool level)
    {
        if (_ca1Line && !level) // falling edge
        {
            // PCR bit 0 = edge selection (0 = falling edge, 1 = rising edge)
            if ((_via.PCR & 0x01) == 0 && (_via.IER & 0x01) != 0)
            {
                _via.IFR |= 0x01; // CA1 = bit 0
                UpdateIFRTopBit();
            }
        }

        _ca1Line = level;
    }

    public void KeyDown(int row, int col)
    {
        if (!_keyboardState[col, row] && row != 0)
        {
            KeysDown++;
        }

        _keyboardState[col, row] = true;

        // Latch key if not already latched
        LatchedKeycode ??= (col & 0x0F) | ((row & 0x07) << 4);

        DoKbdIntCheck();
    }

    public void KeyUp(int row, int col)
    {
        if (row < 0 || col < 0)
        {
            return;
        }

        _keyUpHappened = true;
    }

    public void ClearLatchedKeyIfNotHeld()
    {
        if (LatchedKeycode.HasValue)
        {
            var code = LatchedKeycode.Value;
            var row = (code >> 4) & 0x07;
            var col = code & 0x0F;

            if (!_keyboardState[col, row])
            {
                LatchedKeycode = null;
            }
        }
    }

    public void ReleaseAllKeys()
    {
        for (var row = 0; row < 8; row++)
        for (var col = 0; col < 16; col++)
        {
            _keyboardState[col, row] = false;
        }

        KeysDown = 0;
        //      LatchedKeycode = null;
    }

    public void AttachIrq(Action? raiseIRQ, Action? cancelIRQ)
    {
        _raiseIRQ = raiseIRQ;
        _cancelIRQ = cancelIRQ;
    }

    public void AttachPeripheralHandlers(
        Action<bool>? capsLock = null,
        Action<bool>? shiftLock = null,
        Action<ushort>? videoBase = null,
        Action<bool>? autoScan = null,
        Action<bool>? speech = null,
        Action<bool>? sound = null)
    {
        _shiftLockHandler = capsLock;
        _capsLockHandler = shiftLock;
        _videoBaseHandler = videoBase;
        _autoScanHandler = autoScan;
        _speechHandler = speech;
        _soundHandler = sound;
    }

    public void UpdateIFRTopBit()
    {
        if ((_via.IFR & _via.IER & 0x7F) != 0)
        {
            _via.IFR |= 0x80;
            _raiseIRQ?.Invoke();
        }
        else
        {
            _via.IFR &= 0x7F;
            _cancelIRQ?.Invoke();
        }
    }

    private void DoKbdIntCheck()
    {
        if (KeysDown > 0 && (_via.PCR & 0x0C) == 0x04)
        {
            if ((_via.IC32State & MachineConstants.Ic32Constants.Ic32KeyboardWrite) != 0)
            {
                _via.IFR |= MachineConstants.InterruptBits.CA2;
                UpdateIFRTopBit();
            }
            else if (_kbdCol < 15)
            {
                for (var row = 1; row < 8; row++)
                {
                    if (_keyboardState[_kbdCol, row])
                    {
                        _via.IFR |= MachineConstants.InterruptBits.CA2;
                        UpdateIFRTopBit();
                        break;
                    }
                }
            }
        }
    }

    private void IC32Write(byte value)
    {
        // Hello. This is Richard Gellman. It is 10:25pm, Friday 2nd February 2001
        // I have to do CMOS RAM now. And I think I'm going slightly potty.
        // Additional, Sunday 4th February 2001. I must have been potty. the line above did read January 2000.

        int prevIC32State = _via.IC32State;

        var bit = value & 7;

        if ((value & 8) != 0)
        {
            _via.IC32State |= (byte)(1 << bit);
        }
        else
        {
            _via.IC32State &= (byte)~(1 << bit);
        }

        _capsLockHandler?.Invoke((_via.IC32State & MachineConstants.Ic32Constants.Ic32CapsLock) == 01);
        _capsLockHandler?.Invoke((_via.IC32State & MachineConstants.Ic32Constants.Ic32ShiftLock) == 01);

        // Must do sound reg access when write line changes
        if ((prevIC32State & MachineConstants.Ic32Constants.Ic32SoundWrite) == 0 &&
            (_via.IC32State & MachineConstants.Ic32Constants.Ic32SoundWrite) == 0)
        {
            // TODO Called in sysvia.cpp when a write is made to the 76489 sound chip
            //Sound_RegWrite(SlowDataBusWriteValue);
        }

        if ((prevIC32State & MachineConstants.Ic32Constants.Ic32SpeechWrite) == 0 &&
            (_via.IC32State & MachineConstants.Ic32Constants.Ic32SpeechWrite) == 0)
        {
            // TODO
            // SpeechWrite(SlowDataBusWriteValue);
        }

        if ((prevIC32State & MachineConstants.Ic32Constants.Ic32SpeechWrite) == 0 &&
            (_via.IC32State & MachineConstants.Ic32Constants.Ic32SpeechWrite) == 0)
        {
            // TODO
            //SpeechReadEnable();
        }


        if ((_via.IC32State & MachineConstants.Ic32Constants.Ic32KeyboardWrite) == 0 &&
            (prevIC32State & MachineConstants.Ic32Constants.Ic32KeyboardWrite) != 0)
        {
            _kbdRow = (_slowDataBusWriteValue >> 4) & 7;
            _kbdCol = _slowDataBusWriteValue & 0xf;
            DoKbdIntCheck(); /* Should really only if write enable on KBD changes */

            // ✅ Trigger CA1 strobe (falling edge)
            SetCA1Line(false);
            _ca1ReleaseDelay = 1; // Let it go high next tick
        }
    }
}