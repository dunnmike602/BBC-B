namespace BeeBoxSDL._6502.Disassembler;

using Assembler;
using Interfaces;

public class Disassembler(IAddressArgumentProcessor addressArgumentProcessor) : IDisassembler
{
    public List<Operation> Disassemble(Func<ushort, byte> readByte, ushort startAddress, ushort endAddress, int radix)
    {
        var labelMap = Helper.GetCodeLabelMap();

        var operations = new List<Operation>(0);

        for (var programCounter = startAddress; programCounter <= endAddress; programCounter++)
        {
            var operation = Data.MapOpCode(readByte(programCounter));

            if (operation == null)
            {
                continue;
            }

            if (labelMap.TryGetValue(programCounter, out var value))
            {
                operation!.OSLabel = value.Name;
            }

            operation.ActualOpCode = readByte(programCounter);

            var parameterLength = operation.GetCurrentInstruction().Bytes - 1;

            operation.Parameters = new byte[parameterLength];

            for (var i = 0; i < parameterLength; i++)
            {
                operation.Parameters[i] = readByte((ushort)(programCounter + 1 + i));
            }

            operation.MemoryAddress = programCounter;
            programCounter += (ushort)parameterLength;

            operation.Argument = addressArgumentProcessor.MapToString(operation.ActualAddressingMode!.Value,
                operation.Parameters, radix);

            operations.Add(operation);
        }

        return operations;
    }

    public static IDisassembler Build()
    {
        var dis = new Disassembler(new AddressArgumentProcessor());

        return dis;
    }
}