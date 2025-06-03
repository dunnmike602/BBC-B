namespace MLDComputing.Emulators.BBCSim._6502.Storage;

public enum Statuses : byte
{
    Carry = 0,
    Zero,
    InterruptDisable,
    DecimalMode,
    BreakCommand,
    Unused,
    Overflow,
    Negative
}