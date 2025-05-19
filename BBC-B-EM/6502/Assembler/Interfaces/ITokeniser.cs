namespace MLDComputing.Emulators.BBCSim._6502.Assembler.Interfaces;

public interface ITokeniser
{
    Operation[] Parse(string program);
}