namespace MLDComputing.Emulators.BBCSim._6502.Disassembler.Interfaces;

using Assembler;

public interface IDisassembler
{
    List<Operation> Disassemble(byte[] memory, int start, int end, int radix);
}