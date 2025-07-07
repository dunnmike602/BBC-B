namespace BeeBoxSDL._6502.Assembler.Validators;

using Interfaces;

public abstract class AddressModeValidator : IAddressModeValidator
{
    public abstract void Validate(Operation operation);
}