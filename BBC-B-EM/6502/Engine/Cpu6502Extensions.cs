namespace MLDComputing.Emulators.BBCSim._6502.Engine;

public partial class Cpu6502
{
    public byte[] DumpMemory(ushort start, ushort length)
    {
        var mem = new byte[length];

        for (var i = start; i < start + length; i++)
        {
            mem[i - start] = readByte(i);
        }

        return mem;
    }

    public string DumpStatusRegister()
    {
        // Symbols in bit-7 … bit-0 order
        var symbols = new[] { 'N', 'V', '-', 'B', 'D', 'I', 'Z', 'C' };

        // Build the header line: "N V 1 B D I Z C"
        var header = string.Join(" ", symbols);

        // Underline it with the same width
        var separator = new string('-', header.Length);

        // Build the bit-value line by testing each flag bit
        var bits = symbols
            .Select((_, i) =>
            {
                var bitIndex = 7 - i; // symbols[0] → bit 7 (Negative), symbols[7] → bit 0 (Carry)
                return (Status & (1 << bitIndex)) != 0 ? '1' : '0';
            })
            .ToArray();
        var values = string.Join(" ", bits);

        // Combine into three lines
        return $"{header}{Environment.NewLine}{separator}{Environment.NewLine}{values}";
    }
}