namespace MLDComputing.Emulators.BBCSim._6502.Assembler.Validators;

public class ImpliedAddressModeValidator : AddressModeValidator
{
    public override void Validate(Operation operation)
    {
        var opCode = operation.AddressModeOpCode(AddressingModes.Implied);

        if (opCode > 0 && !operation.HasArguments())
        {
            operation.HasBeenValidated = true;
            operation.ActualOpCode = opCode;
            operation.ActualAddressingMode = AddressingModes.Implied;
        }
        else if (opCode > 0 && operation.HasArguments())
        {
            operation.HasBeenValidated = true;
            operation.SetInvalidAddressMode();
        }
    }
}