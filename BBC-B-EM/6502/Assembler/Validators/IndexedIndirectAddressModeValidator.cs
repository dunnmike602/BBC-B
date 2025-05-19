namespace MLDComputing.Emulators.BBCSim._6502.Assembler.Validators;

using Extensions;

public class IndexedIndirectAddressModeValidator : AddressModeValidator
{
    private static bool CheckForLabel(Operation operation)
    {
        var returnValue = false;

        // Handle labels can't work out the address reference yet this will be done later
        if (operation.ArgumentIsIndexedIndirectLabel())
        {
            returnValue = true;

            operation.ActualOpCode = operation.AddressModeOpCode(AddressingModes.IndexedIndirect);
            operation.ActualAddressingMode = AddressingModes.IndexedIndirect;
            operation.ArgumentContainsLabel = true;
            operation.Parameters = new byte[1];
        }

        return returnValue;
    }


    public override void Validate(Operation operation)
    {
        if (operation.ArgumentIsIndexedIndirectLabel() || operation.ArgumentIsIndexedIndirectValue())
        {
            operation.HasBeenValidated = true;

            // 1. Label is allowed
            if (CheckForLabel(operation))
            {
                return;
            }

            // 2. Zero  Page address is allowed
            var parsedValue = operation.Argument.ConvertToInt();

            if (parsedValue.HasValue && parsedValue.Value <= byte.MaxValue)
            {
                operation.ActualOpCode = operation.AddressModeOpCode(AddressingModes.IndexedIndirect);
                operation.ActualAddressingMode = AddressingModes.IndexedIndirect;
                operation.ArgumentContainsLabel = false;
                operation.Parameters = new byte[1];
                operation.Parameters[0] = (byte)parsedValue.Value.LowWord();
            }
            else if (parsedValue.HasValue && parsedValue.Value > byte.MaxValue)
            {
                operation.SetOutOfRange();
            }
            else
            {
                operation.SetInvalidAddressMode();
            }
        }
    }
}