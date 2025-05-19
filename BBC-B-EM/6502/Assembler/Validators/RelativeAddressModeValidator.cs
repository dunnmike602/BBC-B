namespace MLDComputing.Emulators.BBCSim._6502.Assembler.Validators;

using Extensions;

public class RelativeAddressModeValidator : AddressModeValidator
{
    private readonly List<string> _relativeNmemonics = new();

    public RelativeAddressModeValidator()
    {
        _relativeNmemonics.Add("BCC");
        _relativeNmemonics.Add("BCS");
        _relativeNmemonics.Add("BEQ");
        _relativeNmemonics.Add("BMI");
        _relativeNmemonics.Add("BNE");
        _relativeNmemonics.Add("BPL");
        _relativeNmemonics.Add("BVC");
        _relativeNmemonics.Add("BVS");
    }

    private static bool CheckForLabel(Operation operation)
    {
        var returnValue = false;

        // Handle labels can't work out the address reference yet this will be done later
        if (operation.ArgumentIsLabel())
        {
            returnValue = true;

            operation.ActualOpCode = operation.AddressModeOpCode(AddressingModes.Relative);
            operation.ActualAddressingMode = AddressingModes.Relative;
            operation.ArgumentContainsLabel = true;
            operation.Parameters = new byte[1];
        }

        return returnValue;
    }


    public override void Validate(Operation operation)
    {
        if (_relativeNmemonics.Count(m => m == operation.Mnemonic) > 0)
        {
            operation.HasBeenValidated = true;

            // These instructions only allow relative addressing so validate the parameter accordingly

            // 1. Label is allowed
            if (CheckForLabel(operation))
            {
                return;
            }

            // 2. Hex or decimal 8 bit value
            var parsedValue = operation.Argument.ConvertToByte();

            if (parsedValue.HasValue)
            {
                operation.ActualOpCode = operation.AddressModeOpCode(AddressingModes.Relative);
                operation.ActualAddressingMode = AddressingModes.Relative;
                operation.ArgumentContainsLabel = false;
                operation.Parameters = new byte[1];
                operation.Parameters[0] = parsedValue.Value;
                return;
            }

            // 3. Allow a + or - offset
            if (operation.ArgumentIsExplictOffset())
            {
                var parsedOffsetValue = operation.Argument.ConvertToSByte();

                if (parsedOffsetValue.HasValue)
                {
                    operation.ActualOpCode = operation.AddressModeOpCode(AddressingModes.Relative);
                    operation.ActualAddressingMode = AddressingModes.Relative;
                    operation.ArgumentContainsLabel = false;
                    operation.Parameters = new byte[1];
                    operation.Parameters[0] = (byte)parsedOffsetValue.Value;
                    return;
                }
            }

            operation.SetInvalidAddressMode();
        }
    }
}