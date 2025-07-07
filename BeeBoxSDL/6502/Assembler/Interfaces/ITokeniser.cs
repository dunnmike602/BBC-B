namespace BeeBoxSDL._6502.Assembler.Interfaces;

public interface ITokeniser
{
    Operation[] Parse(string program);
}