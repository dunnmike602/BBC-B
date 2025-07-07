namespace BeeBoxSDL._6502.Assembler.Validators;

using System.Text.RegularExpressions;
using Constants;
using Extensions;

public class ImmediateAddressModeValidator : AddressModeValidator
{
    public const char ImmediateModeIdentifier = '#';

    private static bool CheckForVariable(Operation operation, byte opCode)
    {
        var returnValue = false;

        var match = Regex.Match(operation.Argument!, RegularExpressionConstants.VariableRegEx).Value;

        if (ImmediateModeIdentifier + match == operation.Argument)
        {
            operation.ActualOpCode = opCode;
            operation.ActualAddressingMode = AddressingModes.Immediate;
            operation.ArgumentContainsVariable = true;
            operation.VariableName = match;
            operation.Parameters = new byte[1];

            returnValue = true;
        }

        return returnValue;
    }

    public override void Validate(Operation operation)
    {
        if (!string.IsNullOrWhiteSpace(operation.Argument) && operation.Argument[0] == ImmediateModeIdentifier)
        {
            operation.HasBeenValidated = true;

            var opCode = operation.AddressModeOpCode(AddressingModes.Immediate);

            if (opCode != 0)
            {
                if (CheckForVariable(operation, opCode))
                {
                    return;
                }

                var parsedValue = operation.Argument.ConvertToByte();

                if (parsedValue.HasValue)
                {
                    operation.ActualOpCode = opCode;
                    operation.ActualAddressingMode = AddressingModes.Immediate;
                    operation.Parameters = new byte[1];
                    operation.Parameters[0] = parsedValue.Value;
                }
                else
                {
                    operation.SetOutOfRange();
                }
            }
            else
            {
                operation.SetInvalidAddressMode();
            }
        }
    }
}