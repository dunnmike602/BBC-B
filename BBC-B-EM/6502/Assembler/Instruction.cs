namespace MLDComputing.Emulators.BBCSim._6502.Assembler;

public struct Instruction
{
    public byte Code { get; set; }

    public int Bytes { get; set; }

    public int Cycles { get; set; }

    public string? Mnemonic { get; set; }

    public AddressingModes AddressingMode { get; set; }

    public string? Description { get; set; }

    public bool IsImplemented { get; set; }

    public string Sample { get; set; }

    public string DisplaySample { get; set; }

    public bool IsNotFound()
    {
        return string.IsNullOrWhiteSpace(Mnemonic);
    }
}