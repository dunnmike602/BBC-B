namespace BeeBoxSDL._6502.Assembler.Validators;

public class BrkImmediateValidator : AddressModeValidator
{
    public override void Validate(Operation operation)
    {
        var opCode = operation.AddressModeOpCode(AddressingModes.Implied);

        if (operation.Mnemonic == Data.BRK && !operation.HasArguments())
        {
            operation.HasBeenValidated = true;
            operation.ActualOpCode = opCode;
            operation.ActualAddressingMode = AddressingModes.Implied;
        }
        else if (operation.Mnemonic == Data.BRK && operation.HasArguments())
        {
            operation.HasBeenValidated = true;
            operation.SetInvalidAddressMode();
        }
    }
}