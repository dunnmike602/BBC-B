namespace MLDComputing.Emulators.BBCSim._6502.Assembler.Interfaces;

public interface IMapper
{
    void MapAndValidate(Operation[] operations);
}