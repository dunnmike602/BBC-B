namespace BeeBoxSDL._6502.Assembler.Validators;

public class AccumulatorAddressModeValidator : AddressModeValidator
{
    public const string? AccumlatorAddressIdentifier = "A";

    public override void Validate(Operation operation)
    {
        if (operation.Argument == AccumlatorAddressIdentifier ||
            (!operation.Definition!.AccumulatorParameterRequired && !operation.HasArguments()))
        {
            operation.HasBeenValidated = true;

            operation.ActualOpCode = operation.AddressModeOpCode(AddressingModes.Accumulator);

            if (operation.ActualOpCode != 0)
            {
                operation.ActualAddressingMode = AddressingModes.Accumulator;
            }
            else
            {
                operation.SetInvalidAddressMode();
            }
        }
    }
}