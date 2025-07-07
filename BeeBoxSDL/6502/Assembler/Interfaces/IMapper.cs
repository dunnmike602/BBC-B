namespace BeeBoxSDL._6502.Assembler.Interfaces;

public interface IMapper
{
    void MapAndValidate(Operation[] operations);
}