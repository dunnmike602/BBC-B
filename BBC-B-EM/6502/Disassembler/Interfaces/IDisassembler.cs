namespace MLDComputing.Emulators.BBCSim._6502.Disassembler.Interfaces;

using Assembler;

public interface IDisassembler
{
    List<Operation> Disassemble(Func<ushort, byte> readByte, ushort startAddress, ushort endAddress, int radix);
}