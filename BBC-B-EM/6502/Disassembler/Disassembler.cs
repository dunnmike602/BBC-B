namespace MLDComputing.Emulators.BBCSim._6502.Disassembler;

using Assembler;
using Interfaces;

public class Disassembler(IAddressArgumentProcessor addressArgumentProcessor) : IDisassembler
{
    public List<Operation> Disassemble(byte[] memory, int start, int end, int radix)
    {
        var operations = new List<Operation>(0);

        // Process at least 3 more bytes than end to ensure partial instructions are not mapped
        var programCounter = start - 1;

        var endValue = Math.Min(end + 3, ushort.MaxValue);

        while (programCounter < endValue)
        {
            programCounter++;

            var operation = Data.MapOpCode(memory[programCounter]);

            if (operation == null)
            {
                continue;
            }

            operation.ActualOpCode = memory[programCounter];

            var parameterLength = operation.GetCurrentInstruction().Bytes - 1;

            operation.Parameters = new byte[parameterLength];

            if (programCounter >= ushort.MaxValue)
            {
                continue;
            }

            Array.Copy(memory, programCounter + 1, operation.Parameters, 0, parameterLength);

            operation.MemoryAddress = programCounter;
            programCounter += parameterLength;

            operation.Argument = addressArgumentProcessor.MapToString(operation.ActualAddressingMode!.Value,
                operation.Parameters, radix);

            operations.Add(operation);
        }

        return operations;
    }
}