namespace MLDComputing.Emulators.BBCSim._6502.Assembler.Validators;

using System.Text.RegularExpressions;
using Constants;
using Extensions;

public class IndirectIndexedAddressModeValidator : AddressModeValidator
{
    private static bool CheckForLabel(Operation operation)
    {
        var returnValue = false;

        // Handle labels can't work out the address reference yet this will be done later
        if (operation.ArgumentIsIndirectIndexedLabel())
        {
            returnValue = true;

            operation.ActualOpCode = operation.AddressModeOpCode(AddressingModes.IndirectIndexed);
            operation.ActualAddressingMode = AddressingModes.IndirectIndexed;
            operation.ArgumentContainsLabel = true;
            operation.Parameters = new byte[1];
        }

        return returnValue;
    }

    private static bool CheckForVariable(Operation operation)
    {
        var returnValue = false;

        var match = Regex.Match(operation.Argument!, RegularExpressionConstants.VariableNameRegEx).Value;

        if ("(" + match + "),Y" == operation.Argument)
        {
            operation.HasBeenValidated = true;
            returnValue = true;

            operation.ActualOpCode = operation.AddressModeOpCode(AddressingModes.IndirectIndexed);
            operation.ActualAddressingMode = AddressingModes.IndirectIndexed;
            operation.ArgumentContainsVariable = true;
            operation.VariableName = match;
            operation.Parameters = new byte[1];
        }

        return returnValue;
    }


    public override void Validate(Operation operation)
    {
        if (operation.ArgumentIsIndirectIndexedLabel() || operation.ArgumentIsIndirectIndexedValue())
        {
            operation.HasBeenValidated = true;

            // 1. Label is allowed
            if (CheckForLabel(operation))
            {
                return;
            }

            // 2. Variable is allowed
            if (CheckForVariable(operation))
            {
                return;
            }

            // 3. Zero  Page address is allowed
            var parsedValue = operation.Argument.ConvertToInt();

            if (parsedValue.HasValue && parsedValue <= byte.MaxValue)
            {
                operation.ActualOpCode = operation.AddressModeOpCode(AddressingModes.IndirectIndexed);
                operation.ActualAddressingMode = AddressingModes.IndirectIndexed;
                operation.ArgumentContainsLabel = false;
                operation.Parameters = new byte[1];
                operation.Parameters[0] = (byte)parsedValue.Value.LowWord();
            }
            else if (parsedValue.HasValue && parsedValue > byte.MaxValue)
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