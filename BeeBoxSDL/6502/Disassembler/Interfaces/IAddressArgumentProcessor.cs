namespace BeeBoxSDL._6502.Disassembler.Interfaces;

using Assembler;

public interface IAddressArgumentProcessor
{
    string? MapToString(AddressingModes addressingMode, byte[] operands, int radix);
}