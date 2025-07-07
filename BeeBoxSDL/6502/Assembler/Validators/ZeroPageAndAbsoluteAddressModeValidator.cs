namespace BeeBoxSDL._6502.Assembler.Validators;

using System.Text.RegularExpressions;
using Constants;
using Extensions;

public class ZeroPageAndAbsoluteAddressModeValidator : AddressModeValidator
{
    private static bool CheckForVariable(Operation operation)
    {
        var returnValue = false;

        var labelMatch = Regex.Match(operation.Argument!, RegularExpressionConstants.LabelRegEx).Value;

        var match = Regex.Match(operation.Argument!, RegularExpressionConstants.VariableRegEx).Value;

        if (string.IsNullOrWhiteSpace(labelMatch) && (match == operation.Argument ||
                                                      (operation.Argument!.Contains(",") && match ==
                                                          operation.Argument.Substring(0, match.Length))))
        {
            var addressCode = operation.GetAbsoluteAddressCode(ushort.MaxValue);

            if (addressCode.Item1 != 0)
            {
                operation.HasBeenValidated = true;
                returnValue = true;

                operation.ActualOpCode = addressCode.Item1;
                operation.ActualAddressingMode = addressCode.Item2;
                operation.ArgumentContainsVariable = true;
                operation.VariableName = match;
                operation.Parameters = new byte[2];
            }
            else
            {
                operation.SetInvalidAddressMode();
            }
        }

        return returnValue;
    }

    private static bool CheckForLabel(Operation operation)
    {
        const int dummyValue = 256;

        var returnValue = false;

        // Handle labels can't work out the address reference yet this will be done later
        if (operation.ArgumentIsLabel())
        {
            operation.HasBeenValidated = true;
            returnValue = true;

            var addressCode = operation.GetAbsoluteAddressCode(dummyValue);

            if (addressCode.Item1 != 0)
            {
                operation.ActualOpCode = addressCode.Item1;
                operation.ActualAddressingMode = addressCode.Item2;
                operation.ArgumentContainsLabel = true;
                operation.Parameters = new byte[2];
            }
            else
            {
                operation.SetInvalidAddressMode();
            }
        }

        return returnValue;
    }

    public override void Validate(Operation operation)
    {
        if (operation.Argument!.Contains("(") || operation.Argument.Contains(')'))
        {
            return;
        }

        if (CheckForVariable(operation))
        {
            return;
        }

        if (CheckForLabel(operation))
        {
            return;
        }

        var parsedValue = operation.Argument.ConvertToInt();

        if (parsedValue.HasValue)
        {
            operation.HasBeenValidated = true;

            if (parsedValue > ushort.MaxValue)
            {
                operation.SetOutOfRange();
                return;
            }

            var addressCode = operation.GetAbsoluteAddressCode(parsedValue.Value);

            if (addressCode.Item1 != 0)
            {
                operation.ActualOpCode = addressCode.Item1;
                operation.ActualAddressingMode = addressCode.Item2;
                operation.ArgumentContainsLabel = false;
                operation.Parameters = operation.GetAbsoluteAddressingModeParameters(parsedValue.Value,
                    operation.ActualAddressingMode.Value);
            }
            else
            {
                operation.SetInvalidAddressMode();
            }
        }
    }
}