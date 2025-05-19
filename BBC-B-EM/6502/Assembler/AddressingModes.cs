namespace MLDComputing.Emulators.BBCSim._6502.Assembler;

public enum AddressingModes
{
    Immediate = 0,
    ZeroPage,
    ZeroPageX,
    Absolute,
    AbsoluteX,
    AbsoluteY,
    IndexedIndirect,
    IndirectIndexed,
    Accumulator,
    Relative,
    Implied,
    Indirect,
    ZeroPageY,
    MaxValue,
    AssemblerDirective
}