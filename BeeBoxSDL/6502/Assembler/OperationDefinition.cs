namespace BeeBoxSDL._6502.Assembler;

public class OperationDefinition
{
    public OperationDefinition()
    {
        const int opCodeCount = (int)AddressingModes.MaxValue;

        Instructions = new Instruction[opCodeCount];

        for (var index = 0; index < Instructions.Length; index++)
        {
            Instructions[index] = new Instruction();
        }
    }

    public string? Mnemonic { get; set; }

    public string? Description { get; set; }

    public Instruction[] Instructions { get; set; }

    public string? Sample { get; set; }

    public bool AccumulatorParameterRequired { get; set; } = true;
}