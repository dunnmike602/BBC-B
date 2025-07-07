namespace BeeBoxSDL._6502.Disassembler;

using Assembler;
using Interfaces;

public class StackDisassembler : IDisassembler
{
    public List<Operation> Disassemble(Func<ushort, byte> readByte, ushort startAddress, ushort endAddress, int radix)
    {
        var labelMap = Helper.GetStackMap();

        var operations = new List<Operation>(0);

        for (int programCounter = startAddress; programCounter >= endAddress; programCounter--)
        {
            var operation = new Operation
            {
                MemoryAddress = programCounter,
                ActualOpCode = readByte((ushort)programCounter)
            };

            if (labelMap.TryGetValue((ushort)programCounter, out var value))
            {
                operation!.OSLabel = value.Name;
            }

            operations.Add(operation);
        }

        return operations;
    }

    public static IDisassembler Build()
    {
        var dis = new StackDisassembler();

        return dis;
    }
}