namespace MLDComputing.Emulators.BBCSim.Beeb.Hardware;

using _6502.Engine;

public class KeyboardMatrix
{
    private const int Rows = 8;
    private const int Columns = 16;

    private readonly KeyState[,] _keyStates = new KeyState[Rows, Columns];

    public KeyboardMatrix()
    {
        for (var row = 0; row < Rows; row++)
        {
            for (var col = 0; col < Columns; col++)
            {
                _keyStates[row, col] = new KeyState();
            }
        }
    }

    public void ReleaseAllKeys()
    {
        for (var row = 0; row < Rows; row++)
        for (var col = 0; col < Columns; col++)
        {
            var state = _keyStates[row, col];
            state.IsPressed = false;
            state.Latched = false;
            state.LastChangeCycle = 0;
            state.LastScanCycle = 0;
        }
    }

    public void PressKey(int row, int col)
    {
        var state = _keyStates[row, col];
        state.IsPressed = true;
        state.Latched = true;
        state.LastChangeCycle = Cpu6502.TotalCyclesExecuted;
    }

    public void ReleaseKey(int row, int col)
    {
        var state = _keyStates[row, col];
        state.IsPressed = false;
        state.LastChangeCycle = Cpu6502.TotalCyclesExecuted;
    }

    public void OnKeyScan(int row, int col)
    {
        _keyStates[row, col].LastScanCycle = Cpu6502.TotalCyclesExecuted;
    }

    public void MarkFullScan()
    {
        // Full matrix scan mode: mark all active keys as scanned
        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 16; col++)
            {
                if (IsKeyActive(row, col))
                {
                    OnKeyScan(row, col);
                }
            }
        }
    }

    public byte GetColumnByte(int selectedRow)
    {
        byte columnBits = 0xFF; // all high

        for (var col = 0; col < Columns; col++)
        {
            if (_keyStates[selectedRow, col].IsPressed || _keyStates[selectedRow, col].Latched)
            {
                columnBits &= (byte)~(1 << col); // active low
                OnKeyScan(selectedRow, col);
            }
        }

        return columnBits;
    }

    public bool IsKeyActive(int row, int col)
    {
        var state = _keyStates[row, col];
        return state.IsPressed || state.Latched;
    }

    public bool AnyKeyActive()
    {
        for (var row = 0; row < Rows; row++)
        for (var col = 0; col < Columns; col++)
        {
            if (_keyStates[row, col].IsPressed || _keyStates[row, col].Latched)
            {
                return true;
            }
        }

        return false;
    }

    public void ClearReleasedLatches()
    {
        const ulong latchHoldCycles = 10000; // ~5ms at 1MHz

        for (var col = 0; col < 16; col++)
        {
            for (var row = 0; row < 8; row++)
            {
                var state = _keyStates[row, col];
                if (state is { IsPressed: false, Latched: true })
                {
                    var delayPassed = Cpu6502.TotalCyclesExecuted - state.LastChangeCycle > latchHoldCycles;
                    var scannedSinceRelease = state.LastScanCycle > state.LastChangeCycle;

                    if (delayPassed && scannedSinceRelease)
                    {
                        state.Latched = false;
                    }
                }
            }
        }
    }
}