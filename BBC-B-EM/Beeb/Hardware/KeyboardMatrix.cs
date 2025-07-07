namespace MLDComputing.Emulators.BBCSim.Beeb.Hardware;

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
        {
            for (var col = 0; col < Columns; col++)
            {
                var state = _keyStates[row, col];
                state.IsPressed = false;
            }
        }
    }

    public void PressKey(int row, int col)
    {
        var state = _keyStates[row, col];
        state.IsPressed = true;
    }

    public void ReleaseKey(int row, int col)
    {
        var state = _keyStates[row, col];
        state.IsPressed = false;
    }


    public byte GetColumnByte(int selectedRow)
    {
        byte columnBits = 0xFF; // all high

        for (var col = 0; col < Columns; col++)
        {
            if (_keyStates[selectedRow, col].IsPressed)
            {
                columnBits &= (byte)~(1 << col); // active low
            }
        }

        return columnBits;
    }

    public bool IsKeyActive(int row, int col)
    {
        var state = _keyStates[row, col];
        return state.IsPressed;
    }

    public bool AnyKeyActive()
    {
        for (var row = 0; row < Rows; row++)
        for (var col = 0; col < Columns; col++)
        {
            if (_keyStates[row, col].IsPressed)
            {
                return true;
            }
        }

        return false;
    }
}