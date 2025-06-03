namespace MLDComputing.Emulators.BBCSim.Beeb.Hardware;

using _6502.Engine.Communication;
using Enums;

public class Keyboard
{
    private static readonly Dictionary<Key, (byte Row, byte Column)> PCToBbcKeyMap = new()
    {
        // Row 0 (Top row: 1–0)
        [Key.D1] = (0, 0),
        [Key.D2] = (0, 1),
        [Key.D3] = (0, 2),
        [Key.D4] = (0, 3),
        [Key.D5] = (0, 4),
        [Key.D6] = (0, 5),
        [Key.D7] = (0, 6),
        [Key.D8] = (0, 7),

        // Row 1 (Q–P)
        [Key.Q] = (1, 0),
        [Key.W] = (1, 1),
        [Key.E] = (1, 2),
        [Key.R] = (1, 3),
        [Key.T] = (1, 4),
        [Key.Y] = (1, 5),
        [Key.U] = (1, 6),
        [Key.I] = (1, 7),

        // Row 2 (A–L)
        [Key.A] = (2, 0),
        [Key.S] = (2, 1),
        [Key.D] = (2, 2),
        [Key.F] = (2, 3),
        [Key.G] = (2, 4),
        [Key.H] = (2, 5),
        [Key.J] = (2, 6),
        [Key.K] = (2, 7),

        // Row 3 (Z–M)
        [Key.Z] = (3, 0),
        [Key.X] = (3, 1),
        [Key.C] = (3, 2),
        [Key.V] = (3, 3),
        [Key.B] = (3, 4),
        [Key.N] = (3, 5),
        [Key.M] = (3, 6),
        [Key.OemComma] = (3, 7),

        // Row 4 (Space, Return, Shift, Control, Symbol keys)
        [Key.Space] = (4, 0),
        [Key.Return] = (4, 1),
        [Key.LeftShift] = (4, 2),
        [Key.RightShift] = (4, 2),
        [Key.LeftCtrl] = (4, 3),
        [Key.RightCtrl] = (4, 3),
        [Key.Back] = (4, 4), // Delete
        [Key.Escape] = (4, 5),
        [Key.Tab] = (4, 6),
        [Key.OemQuestion] = (4, 7), // / or ?

        // Example Function row (row 5)
        [Key.F1] = (5, 0),
        [Key.F2] = (5, 1),
        [Key.F3] = (5, 2),
        [Key.F4] = (5, 3),
        [Key.F5] = (5, 4),
        [Key.F6] = (5, 5),
        [Key.F7] = (5, 6),
        [Key.F8] = (5, 7),
        [Key.F9] = (5, 8),
        [Key.F10] = (5, 9)
    };

    private readonly bool[,] _keyLatched = new bool[8, 10];

    private readonly bool[,] _keyMatrix = new bool[8, 10];

    private bool _capsLockOn;

    private int? _latchedKeyNumber;

    private byte _rowMask;

    public bool HasLatchedKey => _latchedKeyNumber.HasValue;

    public int LatchedKeyNumber => _latchedKeyNumber ?? 0;

    /// <summary>
    ///     Controls whether the hardware Caps-Lock LED is lit.
    /// </summary>
    public bool CapsLockOn
    {
        get => _capsLockOn;
        set
        {
            _capsLockOn = value;
            UpdateCapsLockLed(value);
        }
    }

    /// <summary>
    ///     If true, the VIA-driven auto-scan routine will step through the rows.
    /// </summary>
    public bool AutoScanEnabled { get; set; } = true;

    public void ClearLatchedKeyNumber()
    {
        _latchedKeyNumber = null;
    }

    public event CapLockChangedHandler? CapsLockChanged;

    public void SetRowMask(byte mask)
    {
        _rowMask = mask;
        if (!AutoScanEnabled)
        {
            CaptureKeyRow(_rowMask);
        }
    }

    public void TickAutoScan()
    {
        if (!AutoScanEnabled)
        {
            return;
        }

        _rowMask = (byte)((_rowMask << 1) | (_rowMask >> 7));
        CaptureKeyRow(_rowMask);
    }

    public byte ReadColumnBits()
    {
        byte result = 0xFF;

        for (var row = 0; row < 8; row++)
        {
            if ((_rowMask & (1 << row)) != 0)
            {
                for (var col = 0; col < 8; col++)
                {
                    if (_keyLatched[row, col])
                    {
                        result &= (byte)~(1 << col); // clear bit if key is pressed
                    }
                }
            }
        }

        return result;
    }

    private void CaptureKeyRow(byte rowMask)
    {
        for (var row = 0; row < 8; row++)
        {
            if ((rowMask & (1 << row)) != 0)
            {
                for (var col = 0; col < 8; col++)
                {
                    if (_keyLatched[row, col])
                    {
                        // Simulate key being read
                        Console.WriteLine($"Scanned: key pressed at row {row}, col {col}");

                        _keyLatched[row, col] = false;
                    }
                }
            }
        }
    }

    private void UpdateCapsLockLed(bool on)
    {
        CapsLockChanged?.Invoke(this,
            new CapsChangedLockEventArgs
            {
                IsOn = on
            });
    }

    private static int GetBbcKeyNumber(int row, int column)
    {
        return row * 10 + column;
    }

    public bool TryMapKey(int key, out (byte Row, byte Column) pos)
    {
        return PCToBbcKeyMap.TryGetValue((Key)key, out pos);
    }

    public void SetKeyState(int row, int col, bool pressed)
    {
        _keyMatrix[row, col] = pressed;

        if (pressed)
        {
            _keyLatched[row, col] = true;

            // Only latch new key if nothing is already latched
            if (!_latchedKeyNumber.HasValue)
            {
                _latchedKeyNumber = GetBbcKeyNumber(row, col);
            }
        }
        else
        {
            if (_latchedKeyNumber == GetBbcKeyNumber(row, col))
            {
                _latchedKeyNumber = null;
            }

            _keyLatched[row, col] = false;
        }
    }

    public bool IsRowActive(byte mask)
    {
        for (var row = 0; row < 8; row++)
        {
            if ((mask & (1 << row)) != 0)
            {
                for (var col = 0; col < 8; col++)
                {
                    if (_keyLatched[row, col])
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public void ClearLatchedKeys()
    {
        Array.Clear(_keyLatched, 0, _keyLatched.Length);
    }

    public bool IsKeyStillHeld(int keyNumber)
    {
        var row = keyNumber / 10;
        var col = keyNumber % 10;

        return _keyMatrix[row, col]; // true if still held
    }
}