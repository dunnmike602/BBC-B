namespace BeeBoxSDL._6502.Assembler.Validators;

using Extensions;

public class AddressPseudoOperationValidator : AddressModeValidator
{
    public override void Validate(Operation operation)
    {
        if (operation.Mnemonic.IndexOf(Data.ORG, StringComparison.Ordinal) < 0)
        {
            return;
        }

        operation.HasBeenValidated = true;

        var parsedValue = operation.Argument.ConvertToInt();

        if (!parsedValue.HasValue || parsedValue > ushort.MaxValue)
        {
            operation.SetOutOfRange();
            return;
        }

        operation.IsAddressPseudoOperation = true;

        operation.Parameters = new[]
        {
            (byte)parsedValue.Value.LowWord(),
            (byte)parsedValue.Value.HighWord()
        };
    }
}