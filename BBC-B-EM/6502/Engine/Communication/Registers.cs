namespace MLDComputing.Emulators.BBCSim._6502.Engine.Communication;

using System.Text;

public class Registers
{
    public byte StackPointer { get; set; }

    public byte Accumulator { get; set; }

    public byte IX { get; set; }

    public byte IY { get; set; }

    public byte Status { get; set; }

    public ushort ProgramCounter { get; set; }

    public override string ToString()
    {
        // Decode the status flags
        var n = (Status & 0x80) != 0; // Negative
        var v = (Status & 0x40) != 0; // Overflow
        // bit 5 is unused
        var b = (Status & 0x10) != 0; // Break
        var d = (Status & 0x08) != 0; // Decimal
        var i = (Status & 0x04) != 0; // Interrupt Disable
        var z = (Status & 0x02) != 0; // Zero
        var c = (Status & 0x01) != 0; // Carry

        // Build a compact flag string, e.g. "N V - B D I Z C"
        var flags = new StringBuilder();
        flags.Append(n ? 'N' : '-').Append(' ');
        flags.Append(v ? 'V' : '-').Append(' ');
        flags.Append("- "); // unused
        flags.Append(b ? 'B' : '-').Append(' ');
        flags.Append(d ? 'D' : '-').Append(' ');
        flags.Append(i ? 'I' : '-').Append(' ');
        flags.Append(z ? 'Z' : '-').Append(' ');
        flags.Append(c ? 'C' : '-');

        return
            $"PC=0x{ProgramCounter:X4}  " +
            $"A=0x{Accumulator:X2}  " +
            $"X=0x{IX:X2}  " +
            $"Y=0x{IY:X2}  " +
            // show SP and then the flags on the status register, annotated
            $"SP=0x{StackPointer:X2}  " +
            $"SR=0x{Status:X2} [{flags}]";
    }
}