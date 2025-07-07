namespace BeeBoxSDL._6502.Assembler.Validators;

using Extensions;

public class JmpValidator : AddressModeValidator
{
    private static bool CheckForLabel(Operation operation)
    {
        var returnValue = false;

        // Handle labels can't work out the address reference yet this will be done later
        if (operation.ArgumentIsIndirectLabel())
        {
            returnValue = true;

            operation.ActualOpCode = operation.AddressModeOpCode(AddressingModes.Indirect);
            operation.ActualAddressingMode = AddressingModes.Indirect;
            operation.ArgumentContainsLabel = true;
            operation.Parameters = new byte[2];
        }
        else if (operation.ArgumentIsLabel())
        {
            returnValue = true;

            operation.ActualOpCode = operation.AddressModeOpCode(AddressingModes.Absolute);
            operation.ActualAddressingMode = AddressingModes.Absolute;
            operation.ArgumentContainsLabel = true;
            operation.Parameters = new byte[2];
        }

        return returnValue;
    }


    public override void Validate(Operation operation)
    {
        if (operation.Mnemonic == "JMP")
        {
            operation.HasBeenValidated = true;

            // 1. Label is allowed
            if (CheckForLabel(operation))
            {
                return;
            }

            // 16 bit hex or dec value is allowed.
            if (operation.ArgumentIsIndirectValue())
            {
                var parsedValue = operation.Argument.ConvertToInt();

                if (parsedValue.HasValue)
                {
                    operation.ActualOpCode = operation.AddressModeOpCode(AddressingModes.Indirect);
                    operation.ActualAddressingMode = AddressingModes.Indirect;
                    operation.ArgumentContainsLabel = false;
                    operation.Parameters = new byte[2];
                    operation.Parameters[0] = (byte)parsedValue.Value.LowWord();
                    operation.Parameters[1] = (byte)parsedValue.Value.HighWord();
                }

                return;
            }

            // Absolute address mode    
            var parsedAbsoluteValue = operation.Argument.ConvertToInt();

            if (parsedAbsoluteValue.HasValue)
            {
                if (parsedAbsoluteValue > ushort.MaxValue)
                {
                    operation.SetOutOfRange();
                    return;
                }

                var addressMode = operation.GetAbsoluteAddressCode(parsedAbsoluteValue.Value);

                if (addressMode.Item1 != 0)
                {
                    operation.ActualOpCode = addressMode.Item1;
                    operation.ActualAddressingMode = addressMode.Item2;
                    operation.ArgumentContainsLabel = false;
                    operation.Parameters = new byte[2];
                    operation.Parameters[0] = (byte)parsedAbsoluteValue.Value.LowWord();
                    operation.Parameters[1] = (byte)parsedAbsoluteValue.Value.HighWord();
                    return;
                }

                operation.SetInvalidAddressMode();
                return;
            }

            operation.SetInvalidAddressMode();
        }
    }
}