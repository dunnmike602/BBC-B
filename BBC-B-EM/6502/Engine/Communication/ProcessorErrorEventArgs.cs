namespace MLDComputing.Emulators.BBCSim._6502.Engine.Communication;

using Assembler;

public class ProcessorErrorEventArgs : EventArgs
{
    public Instruction Instruction { get; set; }

    public Registers? Registers { get; set; }

    public string? ErrorMessage { get; set; }
}