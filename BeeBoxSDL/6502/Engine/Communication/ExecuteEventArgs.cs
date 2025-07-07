namespace BeeBoxSDL._6502.Engine.Communication;

using Assembler;

public class ExecuteEventArgs : EventArgs
{
    public Instruction Instruction { get; set; }

    public Registers? Registers { get; set; }

    public ushort Address { get; set; }

    public IEnumerable<string> InstructionText { get; set; } = new List<string>();
}