namespace BeeBoxSDL._6502.Assembler.Validators;

using Extensions;

public class VariablePseudoOperationValidator : AddressModeValidator
{
    public override void Validate(Operation operation)
    {
        if (operation.Mnemonic.IndexOf(Data.VAR, StringComparison.Ordinal) < 0)
        {
            return;
        }

        operation.HasBeenValidated = true;

        var parts = operation.Argument!.Split('=');

        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
        {
            operation.SetVariableFormatError();
            return;
        }

        var parsedValue = parts[1].ConvertToInt();

        if (!parsedValue.HasValue)
        {
            operation.SetVariableFormatError();
            return;
        }

        if (parsedValue.Value > ushort.MaxValue)
        {
            operation.SetVariableOutOfRange();
            return;
        }

        operation.IsVariable = true;
        operation.VariableName = parts[0];
        operation.Parameters = new[]
        {
            (byte)parsedValue.Value.LowWord(),
            (byte)parsedValue.Value.HighWord()
        };
    }
}