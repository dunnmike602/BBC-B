namespace MLDComputing.Emulators.BBCSim._6502.Assembler;

using Validators;
using Validators.Interfaces;

public static class Data
{
    public const string ADC = "ADC";
    public const string AND = "AND";
    public const string ASL = "ASL";
    public const string BCC = "BCC";
    public const string BCS = "BCS";
    public const string BEQ = "BEQ";
    public const string BIT = "BIT";
    public const string BMI = "BMI";
    public const string BNE = "BNE";
    public const string BPL = "BPL";
    public const string BRK = "BRK";
    public const string BVC = "BVC";
    public const string BVS = "BVS";
    public const string CLC = "CLC";
    public const string CLD = "CLD";
    public const string CLI = "CLI";
    public const string CLV = "CLV";
    public const string CMP = "CMP";
    public const string CPX = "CPX";
    public const string CPY = "CPY";
    public const string DEC = "DEC";
    public const string DEX = "DEX";
    public const string DEY = "DEY";
    public const string EOR = "EOR";
    public const string INC = "INC";
    public const string INX = "INX";
    public const string INY = "INY";
    public const string JMP = "JMP";
    public const string JSR = "JSR";
    public const string LDA = "LDA";
    public const string LDX = "LDX";
    public const string LDY = "LDY";
    public const string LSR = "LSR";
    public const string NOP = "NOP";
    public const string ORA = "ORA";
    public const string PHA = "PHA";
    public const string PHP = "PHP";
    public const string PLA = "PLA";
    public const string PLP = "PLP";
    public const string ROL = "ROL";
    public const string ROR = "ROR";
    public const string RTI = "RTI";
    public const string RTS = "RTS";
    public const string SBC = "SBC";
    public const string SEC = "SEC";
    public const string SED = "SED";
    public const string SEI = "SEI";
    public const string STA = "STA";
    public const string STX = "STX";
    public const string STY = "STY";
    public const string TAX = "TAX";
    public const string TAY = "TAY";
    public const string TSX = "TSX";
    public const string TXA = "TXA";
    public const string TXS = "TXS";
    public const string TYA = "TYA";
    public const string BYTE = ".BYTE";
    public const string ORG = ".ORG";
    public const string KIL = "KIL";
    public const string VAR = ".VAR";
    public const string DBG = "DBG";

    public static readonly List<OperationDefinition> Definitions;
    private static readonly List<AddressModeValidator> AddressModeValidator;

    static Data()
    {
        Definitions = GetDefinitions();

        AddressModeValidator = new List<AddressModeValidator>
        {
            new VariablePseudoOperationValidator(),
            new AddressPseudoOperationValidator(),
            new BytePsuedoOpValidator(),
            new BrkImmediateValidator(),
            new JmpValidator(),
            new ImpliedAddressModeValidator(),
            new RelativeAddressModeValidator(),
            new AccumulatorAddressModeValidator(),
            new ImmediateAddressModeValidator(),
            new IndexedIndirectAddressModeValidator(),
            new IndirectIndexedAddressModeValidator(),
            new ZeroPageAndAbsoluteAddressModeValidator()
        };
    }

    public static Instruction[] GetInstructions()
    {
        var instructions = new Instruction[byte.MaxValue];

        var nopInstruction = Definitions.SelectMany(definition => definition.Instructions)
            .First(instruction => instruction.Mnemonic == NOP);

        // Preset all instructions to NOP
        for (var index = 0; index < instructions.Length; index++)
        {
            instructions[index] = nopInstruction;
        }

        foreach (var instruction in Definitions.SelectMany(definition => definition.Instructions)
                     .Where(instruction => !string.IsNullOrWhiteSpace(instruction.Mnemonic)))
        {
            instructions[instruction.Code] = instruction;
        }


        return instructions;
    }

    public static void ValidateParameter(Operation operation)
    {
        foreach (IAddressModeValidator validators in AddressModeValidator)
        {
            validators.Validate(operation);

            if (operation.HasBeenValidated)
            {
                break;
            }
        }

        if (!operation.HasBeenValidated)
        {
            operation.SetInvalidAddressMode();
        }
    }

    public static Operation? MapOpCode(int opCode)
    {
        var mappedInstruction = (from definition in Definitions
            from instruction in definition.Instructions
            where instruction.Code == opCode
            select instruction).FirstOrDefault();

        var mappedDefinition = (from definitions in Definitions
            from instructions in definitions.Instructions
            where instructions.Code == opCode
            select definitions).FirstOrDefault();

        if (mappedDefinition == null || mappedInstruction.IsNotFound())
        {
            return null;
        }

        return new Operation
        {
            ActualAddressingMode = mappedInstruction.AddressingMode,
            Definition = mappedDefinition
        };
    }

    public static void MapParameter(Operation operation)
    {
        var definition = Definitions.FirstOrDefault(m => m.Mnemonic == operation.Mnemonic);

        if (definition != null)
        {
            operation.Definition = definition;
        }
        else
        {
            operation.ErrorMessage = operation.Mnemonic + " is an invalid Opcode Mnemonic.";
        }
    }

    public static void SetOpCode(OperationDefinition operationDefinition, AddressingModes addressingMode,
        byte opCode, int bytes, int cycles, string? description, string sample,
        bool isImplemented = true)
    {
        var mode = (int)addressingMode;
        operationDefinition.Instructions[mode].Code = opCode;
        operationDefinition.Instructions[mode].Bytes = bytes;
        operationDefinition.Instructions[mode].Cycles = cycles;
        operationDefinition.Instructions[mode].Mnemonic = operationDefinition.Mnemonic;
        operationDefinition.Instructions[mode].AddressingMode = addressingMode;
        operationDefinition.Instructions[mode].Description = description;
        operationDefinition.Instructions[mode].IsImplemented = isImplemented;
        operationDefinition.Instructions[mode].Sample = sample;
    }

    public static List<OperationDefinition> GetDefinitions()
    {
        var definitions = new List<OperationDefinition>();

        var operationDefinition = new OperationDefinition { Mnemonic = ADC };
        SetOpCode(operationDefinition, AddressingModes.Immediate, 0x69, 2, 2,
            "Add Accumulator, carry bit and supplied value.", "ADC #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0x65, 2, 3,
            "Add Accumulator and carry bit into memory using Zero Page address.", "ADC $00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0x75, 2, 4,
            "Add Accumulator and carry bit into memory using Zero Page.X address.", "ADC $00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0x6D, 3, 4,
            "Add Accumulator and carry bit into memory using Absolute address.", "ADC $0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0x7D, 3, 4,
            "Add Accumulator and carry bit into memory using Absolute,X address.", "ADC $0000,X");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteY, 0x79, 3, 4,
            "Add Accumulator and carry bit into memory using Absolute,Y address.", "ADC $0000,Y");
        SetOpCode(operationDefinition, AddressingModes.IndexedIndirect, 0x61, 2, 6,
            "Add Accumulator and carry bit into memory using Indexed Indirect address.", "ADC $0000,X)");
        SetOpCode(operationDefinition, AddressingModes.IndirectIndexed, 0x71, 2, 5,
            "Add Accumulator and carry bit into memory using Indirect Indexed address.", "ADC ($0000),Y");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = AND };
        SetOpCode(operationDefinition, AddressingModes.Immediate, 0x29, 2, 2,
            "AND Accumulator with the supplied value.", "AND #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0x25, 2, 3,
            "AND Accumulator with memory using Zero Page address.", "AND $00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0x35, 2, 4,
            "AND Accumulator with memory using Zero Page,X address.", "AND $00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0x2D, 3, 4,
            "AND Accumulator with memory using Absolute address.", "AND $0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0x3D, 3, 4,
            "AND Accumulator with memory using Absolute,X address.", "AND $0000,X");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteY, 0x39, 3, 4,
            "AND Accumulator with memory using Absolute,Y address.", "AND $0000,Y");
        SetOpCode(operationDefinition, AddressingModes.IndexedIndirect, 0x21, 2, 6,
            "AND Accumulator with memory using Indexed Indirect address.", "AND ($0000,X)");
        SetOpCode(operationDefinition, AddressingModes.IndirectIndexed, 0x31, 2, 5,
            "AND Accumulator with memory using Indirect Indexed address.", "AND ($0000),Y");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = ASL };
        SetOpCode(operationDefinition, AddressingModes.Accumulator, 0x0A, 1, 2,
            "Shift bits one to the left in the Accumulator.", "ASL A");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0x06, 2, 5,
            "Shift bits one to the left in the Zero Page memory location.", "ASL $00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0x16, 2, 6,
            "Shift bits one to the left in the Zero Page,X memory location.", "ASL $00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0x0E, 3, 6,
            "Shift bits one to the left in the Absolute memory location.", "ASL $0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0x1E, 3, 7,
            "Shift bits one to the left in the Absolute,X memory location.", "ASL $0000,X");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = BCC };
        SetOpCode(operationDefinition, AddressingModes.Relative, 0x90, 2, 2,
            "Branch if Carry flag is clear, to the relative address.", "BCC LABEL:");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = BCS };
        SetOpCode(operationDefinition, AddressingModes.Relative, 0xB0, 2, 2,
            "Branch if Carry flag set, to the relative address.", "BCS LABEL:");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = BEQ };
        SetOpCode(operationDefinition, AddressingModes.Relative, 0xF0, 2, 2,
            "Branch if Zero flag set (Equal), to the relative address.", "BCS LABEL:");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = BIT };
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0x24, 2, 3,
            "Bit test accumulator and Zero Page memory location.", "BIT $00");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0x2C, 3, 4,
            "Bit test accumulator and Absolute memory location.", "BIT $0000");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = BMI };
        SetOpCode(operationDefinition, AddressingModes.Relative, 0x30, 2, 2,
            "Branch if Negative flag set (Minus), to the relative address.", "BMI LABEL:");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = BNE };
        SetOpCode(operationDefinition, AddressingModes.Relative, 0xD0, 2, 2,
            "Branch if Zero flag clear (Not Equal), to the relative address.", "BNE LABEL:");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = BPL };
        SetOpCode(operationDefinition, AddressingModes.Relative, 0x10, 2, 2,
            "Branch if Negative flag clear (Positive), to the relative address.", "BPL LABEL:");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = BRK };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x00, 2, 7, "Force generation of an interrupt.", "BRK");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = BVC };
        SetOpCode(operationDefinition, AddressingModes.Relative, 0x50, 2, 2,
            "Branch if Overflow flag clear, to the relative address.", "BVC LABEL:");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = BVS };
        SetOpCode(operationDefinition, AddressingModes.Relative, 0x70, 2, 2,
            "Branch if Overflow flag set, to the relative address.", "BVS LABEL:");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = CLC };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x18, 1, 2, "Clear Carry flag.", "CLC");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = CLD };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0xD8, 1, 2, "Clear Decimal mode flag.", "CLD");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = CLI };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x58, 1, 2, "Clear Interrupt Disable flag.", "CLI");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = CLV };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0xB8, 1, 2, "Clear Overflow flag.", "CLV");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = CMP };
        SetOpCode(operationDefinition, AddressingModes.Immediate, 0xC9, 2, 2,
            "Compare the Accumulator and the supplied value.", "CMP #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0xC5, 2, 3,
            "Compare the Accumulator and the Zero Page memory address.", "CMP $00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0xD5, 2, 5,
            "Compare the Accumulator and the Zero Page,X memory address.", "CMP $00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0xCD, 3, 4,
            "Compare the Accumulator and the Absolute memory address.", "CMP $0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0xDD, 3, 4,
            "Compare the Accumulator and the Absolute,X memory address.", "CMP $0000,X");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteY, 0xD9, 3, 4,
            "Compare the Accumulator and the Absolute,Y memory address.", "CMP $0000,Y");
        SetOpCode(operationDefinition, AddressingModes.IndexedIndirect, 0xC1, 2, 6,
            "Compare the Accumulator and the Indexed Indirect memory address.", "CMP ($0000,X)");
        SetOpCode(operationDefinition, AddressingModes.IndirectIndexed, 0xD1, 2, 5,
            "Compare the Accumulator and the Indirect Indexed memory address.", "CMP ($0000),X");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = CPX };
        SetOpCode(operationDefinition, AddressingModes.Immediate, 0xE0, 2, 2,
            "Compare the contents of the X Register with the supplied value.", "CPX #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0xE4, 2, 3,
            "Compare the contents of the X Register with the Zero Page memory location.", "CPX $00");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0xEC, 3, 4,
            "Compare the contents of the X Register with the Absolute memory location.", "CPX $0000");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = CPY };
        SetOpCode(operationDefinition, AddressingModes.Immediate, 0xC0, 2, 2,
            "Compare the contents of the Y Register with the supplied value.", "CPY #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0xC4, 2, 3,
            "Compare the contents of the Y Register with the Zero Page memory location.", "CPY $00");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0xCC, 3, 4,
            "Compare the contents of the Y Register with the Absolute memory location.", "CPY $0000");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = DEC };
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0xC6, 2, 5,
            "Subtract one from the contents of the Zero Page memory address.", "DEC #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0xD6, 2, 6,
            "Subtract one from the contents of the Zero Page,Y memory address.", "DEC $00");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0xCE, 3, 6,
            "Subtract one from the contents of the Absolute memory address.", "DEC $0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0xDE, 3, 7,
            "Subtract one from the contents of the Absolute.X memory address.", "DEC $0000,X");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = DEX };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0xCA, 1, 2, "Subtract one from the X register.", "DEC");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = DEY };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x88, 1, 2, "Subtract one from the Y register.", "DEY");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = EOR };
        SetOpCode(operationDefinition, AddressingModes.Immediate, 0x49, 2, 2,
            "Perform EOR on the accumulator and supplied value.", "EOR #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0x45, 2, 3,
            "Perform EOR on the accumulator and Zero Page memory address.", "EOR $00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0x55, 2, 4,
            "Perform EOR on the accumulator and Zero Page,X memory address.", "EOR $00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0x4D, 3, 4,
            "Perform EOR on the accumulator and Absolute memory address.", "EOR $0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0x5D, 3, 4,
            "Perform EOR on the accumulator and Absolute,X memory address.", "EOR $0000,X");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteY, 0x59, 3, 4,
            "Perform EOR on the accumulator and Absolute,Y memory address.", "EOR $0000,Y");
        SetOpCode(operationDefinition, AddressingModes.IndexedIndirect, 0x41, 2, 6,
            "Perform EOR on the accumulator and Indexed Indirect memory address.", "EOR ($0000,X)");
        SetOpCode(operationDefinition, AddressingModes.IndirectIndexed, 0x51, 2, 5,
            "Perform EOR on the accumulator and Indirect Indexed memory address.", "EOR ($0000),Y");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = INC };
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0xE6, 2, 5,
            "Add one to the Zero Page memory location.", "INC $00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0xF6, 2, 6,
            "Add one to the Zero Page,X memory location.", "INC $00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0xEE, 3, 6, "Add one to the Absolute memory location.",
            "INC $0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0xFE, 3, 7,
            "Add one to the Absolute,X memory location.", "INC $0000,X");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = INX };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0xE8, 1, 2, "Add one to the X Register.", "INX");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = INY };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0xC8, 1, 2, "Add one to the Y Register.", "INY");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = JMP };
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0x4C, 3, 3,
            "Transfer execution to the Absolute address.", "JMP LABEL:");
        SetOpCode(operationDefinition, AddressingModes.Indirect, 0x6C, 3, 5,
            "Transfer execution to the Indirect address.", "JMP (LABEL:)");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = JSR };
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0x20, 3, 6,
            "Jump to the subroutine at the Absolute address.", "JSR LABEL");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = LDA };
        SetOpCode(operationDefinition, AddressingModes.Immediate, 0xA9, 2, 2,
            "Load the Accumulator with the supplied value.", "LDA #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0xA5, 2, 3,
            "Load the Accumulator from the Zero Page memory address.", "LDA $00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0xB5, 2, 4,
            "Load the Accumulator from the Zero Page,X memory address.", "LDA #$00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0xAD, 3, 4,
            "Load the Accumulator from the Absolute memory address.", "LDA #$0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0xBD, 3, 4,
            "Load the Accumulator from the Absolute,X memory address.", "LDA #$0000,X");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteY, 0xB9, 3, 4,
            "Load the Accumulator from the Absolute,Y memory address.", "LDA #$0000,Y");
        SetOpCode(operationDefinition, AddressingModes.IndexedIndirect, 0xA1, 2, 6,
            "Load the Accumulator from the Indexed Indirect memory address.", "LDA (#$0000,X)");
        SetOpCode(operationDefinition, AddressingModes.IndirectIndexed, 0xB1, 2, 5,
            "Load the Accumulator from the Indirect Indexed memory address.", "LDA (#$0000),Y");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = LDX };
        SetOpCode(operationDefinition, AddressingModes.Immediate, 0xA2, 2, 2,
            "Load the X Register with the supplied value.", "LDX #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0xA6, 2, 3,
            "Load the X Register from the Zero Page memory address.", "LDX $00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageY, 0xB6, 2, 4,
            "Load the X Register from the Zero Page,Y memory address.", "LDX #$00,Y");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0xAE, 3, 4,
            "Load the X Register from the Absolute memory address.", "LDX #$0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteY, 0xBE, 3, 4,
            "Load the Accumulator from the Absolute,Y memory address.", "LDX #$0000,Y");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = LDY };
        SetOpCode(operationDefinition, AddressingModes.Immediate, 0xA0, 2, 2,
            "Load the Y Register with the supplied value.", "LDY #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0xA4, 2, 3,
            "Load the Y Register from the Zero Page memory address.", "LDY $00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0xB4, 2, 4,
            "Load the Y Register from the Zero Page,X memory address.", "LDY $00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0xAC, 3, 4,
            "Load the Y Register from the Absolute memory address.", "LDY $0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0xBC, 3, 4,
            "Load the Y Register from the Absolute,X memory address.", "LDY $0000,X");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = LSR };
        SetOpCode(operationDefinition, AddressingModes.Accumulator, 0x4A, 1, 2,
            "Shift bits in accumulator one place to the left.", "LSR A");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0x46, 2, 5,
            "Shift bits in the Zero Page memory address one place to the left.", "LSR $00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0x56, 2, 6,
            "Shift bits in the Zero Page,X memory address one place to the left.", "LSR $00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0x4E, 3, 6,
            "Shift bits in the Absolute memory address one place to the left.", "LSR $0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0x5E, 3, 7,
            "Shift bits in the Absolute,X memory address one place to the left.", "LSR $0000,X");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = NOP };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0xEA, 1, 2, "No Operation.", "NOP");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = ORA };
        SetOpCode(operationDefinition, AddressingModes.Immediate, 0x09, 2, 2, "Perform OR on the Accumulator.",
            "ORA #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0x05, 2, 3,
            "Perform OR on the Zero Page memory location.", "ORA $00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0x15, 2, 4,
            "Perform OR on the Zero Page,X memory location.", "ORA $00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0x0D, 3, 4,
            "Perform OR on the Absolute memory location.", "ORA $0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0x1D, 3, 4,
            "Perform OR on the Absolute,X memory location.", "ORA $0000,X");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteY, 0x19, 3, 4,
            "Perform OR on the Absolute,Y memory location.", "ORA $0000,Y");
        SetOpCode(operationDefinition, AddressingModes.IndexedIndirect, 0x01, 2, 6,
            "Perform OR on the Indexed Indirect memory location.", "ORA ($0000,X)");
        SetOpCode(operationDefinition, AddressingModes.IndirectIndexed, 0x11, 2, 5,
            "Perform OR on the Indirect Indexed memory location.", "ORA ($0000),X");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = PHA };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x48, 1, 3, "Push Accumulator on the stack.", "PHA");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = PHP };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x08, 1, 3,
            "Push Processor Status register on the stack.", "PHP");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = PLA };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x68, 1, 4, "Pull Accumulator from the stack.", "PLA");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = PLP };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x28, 1, 4,
            "Pull Processor Status register from the stack.", "PLP");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = ROL };
        SetOpCode(operationDefinition, AddressingModes.Accumulator, 0x2A, 1, 2,
            "Rotate bits in the Accumulator one place to the left.", "ROL A");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0x26, 2, 5,
            "Rotate bits in Zero Page memory location one place to the left.", "ROL #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0x36, 2, 6,
            "Rotate bits in Zero Page,X memory location one place to the left.", "ROL #$00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0x2E, 3, 6,
            "Rotate bits in Absolute memory location one place to the left.", "ROL #$0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0x3E, 3, 7,
            "Rotate bits in Absolute,X memory location one place to the left.", "ROL #$0000,X");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = ROR };
        SetOpCode(operationDefinition, AddressingModes.Accumulator, 0x6A, 1, 2,
            "Rotate bits in the Accumulator one place to the right.", "ROL A");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0x66, 2, 5,
            "Rotate bits in Zero Page memory location one place to the right.", "ROL $00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0x76, 2, 6,
            "Rotate bits in Zero Page,X memory location one place to the right.", "ROL $00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0x6E, 3, 6,
            "Rotate bits in Absolute memory location one place to the right.", "ROL $0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0x7E, 3, 7,
            "Rotate bits in Absolute,X memory location one place to the right.", "ROL $0000,X");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = RTI };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x40, 1, 6, "Return from interrupt.", "RTI");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = RTS };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x60, 1, 6, "Return from subroutine.", "RTS");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = SBC };
        SetOpCode(operationDefinition, AddressingModes.Immediate, 0xE9, 2, 2,
            "Subtract supplied value and carry bit from the Accumulator.", "SBC #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0xE5, 2, 3,
            "Subtract the Zero Page value and carry bit from the Accumulator.", "SBC $00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0xF5, 2, 4,
            "Subtract the Zero Page,X value and carry bit from the Accumulator.", "SBC $00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0xED, 3, 4,
            "Subtract the Absolute value and carry bit from the Accumulator.", "SBC $0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0xFD, 3, 4,
            "Subtract the Absolute,X value and carry bit from the Accumulator.", "SBC $0000,X");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteY, 0xF9, 3, 4,
            "Subtract the Absolute,Y value and carry bit from the Accumulator.", "SBC $0000,Y");
        SetOpCode(operationDefinition, AddressingModes.IndexedIndirect, 0xE1, 2, 6,
            "Subtract the Indexed Indirect and carry bit value from the Accumulator.", "SBC ($0000,X)");
        SetOpCode(operationDefinition, AddressingModes.IndirectIndexed, 0xF1, 2, 5,
            "Subtract the Indirect Indexed value and carry bit from the Accumulator.", "SBC ($0000),X");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = SEC };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x38, 1, 2, "Set the Carry flag.", "SEC");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = SED };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0xF8, 1, 2, "Set the Decimal flag.", "SED");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = SEI };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x78, 1, 2, "Set Interrupt Disable flag.", "SEI");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = STA };
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0x85, 2, 3,
            "Store the Accumulator in the Zero Page memory location.", "STA #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0x95, 2, 4,
            "Store the Accumulator in the Zero Page,X memory location.", "STA $00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0x8D, 3, 4,
            "Store the Accumulator in the Absolute memory location.", "STA $0000");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteX, 0x9D, 3, 5,
            "Store the Accumulator in the Absolute,X memory location.", "STA $0000,X");
        SetOpCode(operationDefinition, AddressingModes.AbsoluteY, 0x99, 3, 5,
            "Store the Accumulator in the Absolute,Y memory location.", "STA $0000,Y");
        SetOpCode(operationDefinition, AddressingModes.IndexedIndirect, 0x81, 2, 6,
            "Store the Accumulator in the Indexed Indirect memory location.", "STA ($0000,X)");
        SetOpCode(operationDefinition, AddressingModes.IndirectIndexed, 0x91, 2, 6,
            "Store the Accumulator in the Indirect Indexed memory location.", "STA ($0000),Y");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = STX };
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0x86, 2, 3,
            "Store the X Register in the Zero Page memory location.", "STX #$00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageY, 0x96, 2, 4,
            "Store the X Register in the Zero Page,Y memory location.", "STX $00,Y");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0x8E, 3, 4,
            "Store the X Register in the Absolute memory location.", "STX $0000");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = STY };
        SetOpCode(operationDefinition, AddressingModes.ZeroPage, 0x84, 2, 3,
            "Store the Y Register in the Zero Page memory location.", "STY $00");
        SetOpCode(operationDefinition, AddressingModes.ZeroPageX, 0x94, 2, 4,
            "Store the Y Register in the Zero Page,X memory location.", "STY $00,X");
        SetOpCode(operationDefinition, AddressingModes.Absolute, 0x8C, 3, 4,
            "Store the Y Register in the Absolute memory location.", "STY $0000");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = TAX };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0xAA, 1, 2,
            "Transfer the Accumulator to the X Register.", "TAX");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = TAY };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0xA8, 1, 2,
            "Transfer the Accumulator to the Y Register.", "TAY");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = DBG };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0xF2, 1, 0,
            "Pseudo instruction, trigger single step at next statement.", "DBG");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = TSX };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0xBA, 1, 2, "Transfer the Stack Pointer to X.", "TSX");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = TXA };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x8A, 1, 2,
            "Transfer the X Register to the Accumulator.", "TXA");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = TXS };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x9A, 1, 2,
            "Transfer the X Register to the Stack Pointer.", "TXS");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = TYA };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x98, 1, 2,
            "Transfer the Y Register to the Accumulator.", "TYA");
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition
        {
            Mnemonic = BYTE, Sample = ".BYTE $00", Description = "Assembler directive, load data directly into memory."
        };
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition
        {
            Mnemonic = ORG, Sample = ".ORG $0000",
            Description = "Assembler directive, assemble next instruction into this memory location."
        };
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition
            { Mnemonic = VAR, Sample = ".VAR X=$00", Description = "Assembler directive, declare a variable value." };
        definitions.Add(operationDefinition);

        operationDefinition = new OperationDefinition { Mnemonic = KIL };
        SetOpCode(operationDefinition, AddressingModes.Implied, 0x42, 1, 0,
            "Pseudo instruction that immediately terminates the running program.", "KIL");
        definitions.Add(operationDefinition);

        return definitions;
    }
}