namespace BeeBoxSDL._6502.Assembler.Interfaces;

public interface IAssembler
{
    void Assemble(Operation[] operations, ushort startAddress, bool addPaddingByteForBRK = false);
}