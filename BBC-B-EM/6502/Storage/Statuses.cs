namespace MLDComputing.Emulators.BBCSim._6502.Storage;

public enum Statuses : byte
{
    CarryFlag = 0,
    ZeroFlag,
    InterruptDisable,
    DecimalMode,
    BreakCommand,
    Unused,
    OverflowFlag,
    NegativeFlag
}