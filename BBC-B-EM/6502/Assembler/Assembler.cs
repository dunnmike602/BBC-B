namespace MLDComputing.Emulators.BBCSim._6502.Assembler;

using System.Text.RegularExpressions;
using Constants;
using Engine;
using Extensions;
using Interfaces;

public class Assembler(Action<ushort, byte> writeByte) : IAssembler
{
    public const string HiByteSelector = ">";
    public const string LowByteSelector = "<";


    public void Assemble(Operation[] operations, ushort startAddress, bool addPaddingByteForBRK = false)
    {
        ProcessVariables(operations);

        var programCounter = startAddress;

        var hasSucceeded = ProcessFirstPass(operations, addPaddingByteForBRK, ref programCounter);

        if (hasSucceeded)
        {
            ProcessLabels(operations);

            ProcessSecondPass(operations);
        }
    }

    private void ProcessSecondPass(Operation[] operations)
    {
        foreach (var operation in operations.Where(m => m.ArgumentContainsLabel))
        {
            var argumentLabel = operation.GetParsedLabelName();

            var labelTarget = operations.FirstOrDefault(m => m.LabelName == argumentLabel);

            if (labelTarget == null)
            {
                operation.ErrorMessage = "Label " + argumentLabel + " does not exist.";
                return;
            }

            if (!ReplaceWithLabelAddress(labelTarget, operation))
            {
                return;
            }
        }
    }

    private static void ProcessLabels(Operation[] operations)
    {
        foreach (var operation in operations.Where(m => m.OperationIsLabel()))
        {
            // Address of Label is address of subsequent instruction or last instruction in program
            var nextOp = operations.FirstOrDefault(m => m.InstructionNumber > operation.InstructionNumber &&
                                                        m.OperationIsOpCode());

            if (nextOp != null)
            {
                operation.MemoryAddress = nextOp.MemoryAddress;
            }
            else
            {
                nextOp = operations.LastOrDefault(m => m.OperationIsOpCode());

                if (nextOp != null)
                {
                    operation.MemoryAddress = nextOp.MemoryAddress + nextOp.GetEntireInstruction().Length;
                }
            }
        }
    }

    private bool ProcessFirstPass(IEnumerable<Operation> operations, bool addPaddingByteForBRK,
        ref ushort programCounter)
    {
        var hasSucceded = true;

        foreach (var operation in operations.Where(m => m.OperationIsOpCode()))
        {
            // Process Address Pseudo Operation, moves address to a new location,
            // and also ensure that the .ORG operation is stamped with this memory address
            if (operation.IsAddressPseudoOperation)
            {
                programCounter = (ushort)(operation.Parameters[0] + operation.Parameters[1] *
                    Cpu6502.MsbMultiplier);
                operation.MemoryAddress = programCounter;
                continue;
            }

            var instruction = operation.GetEntireInstruction();
            var newProgramCounter = programCounter + instruction.Length;

            if (newProgramCounter > ushort.MaxValue)
            {
                operation.ErrorMessage = "Instruction cannot be assembled beyond last memory location";
                hasSucceded = false;
                continue;
            }

            operation.MemoryAddress = programCounter;

            for (var i = 0; i < instruction.Length; i++)
            {
                writeByte((ushort)(programCounter + i), instruction[i]);
            }

            programCounter = (ushort)newProgramCounter;

            // BRK is actually a 2 byte instruction, assembly can take place with a dummy byte being
            // added automatically or under programmer control
            if (addPaddingByteForBRK)
            {
                writeByte(programCounter++, Data.GetInstructions().First(m => m.Mnemonic == Data.NOP).Code);
            }
        }

        return hasSucceded;
    }

    private static void ProcessVariables(Operation[] operations)
    {
        foreach (var operation in operations.Where(m => m.ArgumentContainsVariable))
        {
            var innerOperation = operation;
            var variableNameOnly =
                Regex.Match(innerOperation.VariableName!, RegularExpressionConstants.VariableNameRegEx).Value.ToUpper();

            var variableOp = operations.LastOrDefault(m => m.IsVariable &&
                                                           m.VariableName == variableNameOnly);

            if (variableOp != null)
            {
                var variableValue = variableOp.Parameters[0] +
                                    variableOp.Parameters[1] * Cpu6502.MsbMultiplier +
                                    GetOffset(innerOperation.VariableName);

                // Pick Hi or Lo byte if appropriate
                variableValue = GetCorrectByte(variableValue, innerOperation.VariableName);

                if (variableValue > ushort.MaxValue)
                {
                    innerOperation.SetOutOfRange();
                }

                switch (innerOperation.ActualAddressingMode)
                {
                    case AddressingModes.Immediate:
                    case AddressingModes.IndirectIndexed:
                        operation.Parameters[0] = ((ushort)variableValue).LowByte();
                        break;

                    case AddressingModes.Absolute:
                    case AddressingModes.AbsoluteX:
                    case AddressingModes.AbsoluteY:
                        operation.SetAddressModeFromValue(variableValue);
                        break;
                }
            }
            else
            {
                innerOperation.ErrorMessage = "Variable " + innerOperation.VariableName + " is not defined.";
            }
        }
    }

    private static int GetCorrectByte(int variableValue, string? variableName)
    {
        var twoByteValue = Convert.ToUInt16(variableValue);

        if (variableName!.Contains(HiByteSelector))
        {
            return twoByteValue.HighByte();
        }

        if (variableName.Contains(LowByteSelector))
        {
            return twoByteValue.LowByte();
        }

        return variableValue;
    }

    private static int GetOffset(string? variableName)
    {
        const string plus = "+";
        const string negative = "-";

        var offset = 0;

        if (variableName!.Contains(plus))
        {
            offset = variableName.ExtractFirstNumber(plus);
        }
        else if (variableName.Contains(negative))
        {
            offset = variableName.ExtractFirstNumber(negative) * -1;
        }

        return offset;
    }

    private bool ReplaceWithLabelAddress(Operation labelTarget, Operation operation)
    {
        var startLocation = (ushort)(operation.MemoryAddress + 1);

        switch (operation.ActualAddressingMode)
        {
            case AddressingModes.Absolute:
            case AddressingModes.AbsoluteX:
            case AddressingModes.AbsoluteY:
            case AddressingModes.Indirect:
                writeByte(startLocation, (byte)labelTarget.MemoryAddress.LowWord());
                writeByte((ushort)(startLocation + 1), (byte)labelTarget.MemoryAddress.HighWord());
                break;

            case AddressingModes.Relative:
                var offset = labelTarget.MemoryAddress - operation.MemoryAddress;

                if (offset < Cpu6502.RelativeAddressBackwardsLimit || offset > Cpu6502.RelativeAddressForwardsLimit)
                {
                    operation.ErrorMessage = offset + " is an invalid relative address target.";
                    return false;
                }

                writeByte(startLocation, offset <= 0
                    ? (byte)(offset - Cpu6502.ProgramCounterOffset)
                    : (byte)offset);
                break;

            case AddressingModes.ZeroPage:
            case AddressingModes.ZeroPageX:
            case AddressingModes.ZeroPageY:
            case AddressingModes.IndexedIndirect:
            case AddressingModes.IndirectIndexed:
                if (labelTarget.MemoryAddress > byte.MaxValue)
                {
                    operation.ErrorMessage =
                        labelTarget.MemoryAddress + " is an invalid zero page address.";
                    return false;
                }

                writeByte(startLocation, (byte)labelTarget.MemoryAddress.LowWord());
                break;
        }

        return true;
    }
}