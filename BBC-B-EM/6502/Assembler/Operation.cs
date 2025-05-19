namespace MLDComputing.Emulators.BBCSim._6502.Assembler;

using System.Text.RegularExpressions;
using Constants;
using Extensions;
using Utilities;

public class Operation
{
    private string? _argument;
    private string? _labelName;
    private string? _variableName;

    public Operation()
    {
        Comment = string.Empty;
        Mnemonic = string.Empty;
        ErrorMessage = string.Empty;
        LabelName = string.Empty;
        HasBeenValidated = false;
        ArgumentContainsLabel = false;
        Parameters = [];
        VariableName = string.Empty;
        IsVariable = false;
        IsAddressPseudoOperation = false;
        ArgumentContainsVariable = false;
    }

    public string Mnemonic { get; set; }

    public string? Argument
    {
        get => _argument;
        set
        {
            _argument = value!.ToUpper().Trim();

            // Strip spaces out unless this is a literal
            if (_argument.Length > 0 && _argument[0] != '"')
            {
                _argument = _argument.Replace(" ", string.Empty);
            }
        }
    }

    public int InstructionNumber { get; set; }
    public string? OriginalSource { get; set; }
    public int MemoryAddress { get; set; }
    public bool ArgumentContainsLabel { get; set; }
    public bool ArgumentContainsVariable { get; set; }
    public bool IsAddressPseudoOperation { get; set; }
    public bool IsVariable { get; set; }

    public string? VariableName
    {
        get => _variableName;
        set => _variableName = value!.ToUpper();
    }

    public bool HasBeenValidated { get; set; }
    public string Comment { get; set; }
    public string ErrorMessage { get; set; }

    public string? LabelName
    {
        get => _labelName;
        set
        {
            if (value != null)
            {
                _labelName = value.ToUpper();
            }
        }
    }

    public OperationDefinition? Definition { get; set; }
    public AddressingModes? ActualAddressingMode { get; set; }
    public byte ActualOpCode { get; set; }
    public byte[] Parameters { get; set; }

    public byte[] GetEntireInstruction()
    {
        var entireInstruction = new byte[1 + Parameters.Length];

        entireInstruction[0] = ActualOpCode;

        if (Parameters.Length > 0)
        {
            Parameters.CopyTo(entireInstruction, 1);
        }

        return entireInstruction;
    }

    public byte AddressModeOpCode(AddressingModes mode)
    {
        Check.Require(Definition != null, "This Operation is not mapped to a valid code.");

        return Definition!.Instructions[(int)mode].Code;
    }

    public bool OperationIsOpCode()
    {
        return (Definition != null && !IsVariable) || IsAddressPseudoOperation;
    }

    public bool IsValid()
    {
        return string.IsNullOrWhiteSpace(ErrorMessage);
    }

    public bool ArgumentIsLabel()
    {
        return !string.IsNullOrWhiteSpace(Argument) && Regex.Match(Argument, @"^[^ ()]*:").Success;
    }

    public bool ArgumentIsExplictOffset()
    {
        return !string.IsNullOrWhiteSpace(Argument) && (Argument.Contains(TokenConstants.OffsetPlusMarker) ||
                                                        Argument.Contains(TokenConstants.OffsetMinusMarker));
    }

    public string? GetDescription()
    {
        if (!ActualAddressingMode.HasValue)
        {
            return Definition!.Description;
        }

        return GetCurrentInstruction().Description;
    }

    /// <summary>
    ///     Strips spaces and any indexing from the argument (i.e ,X or ,Y)
    /// </summary>
    public string ParseArgumentCandidateFromArgument()
    {
        if (string.IsNullOrWhiteSpace(Argument))
        {
            return string.Empty;
        }

        return Regex.Match(Argument, RegularExpressionConstants.ValueOnlyRegEx).Value;
    }

    /// <summary>
    ///     Used in Zero Page,X (Y) and Absolute, X (Y) addressing
    /// </summary>
    public bool ShouldAddRegister(string register)
    {
        return Argument!.Replace(" ", "").Contains("," + register.ToUpper());
    }

    public bool HasArguments()
    {
        return !string.IsNullOrWhiteSpace(Argument);
    }

    public void SetInvalidAddressMode()
    {
        ErrorMessage = "Invalid address mode for " + Mnemonic + ".";
    }

    public void SetOutOfRange()
    {
        ErrorMessage = "Supplied argument is invalid or too large for the addressing mode.";
    }

    public void SetVariableOutOfRange()
    {
        ErrorMessage = "Variable value is greater than maximum allowed.";
    }

    public void SetVariableFormatError()
    {
        ErrorMessage = Argument + " is incorrect form for variable declaration, must be NAME VALUE.";
    }

    public Tuple<byte, AddressingModes> GetAbsoluteAddressCode(int value)
    {
        // Check to see if a zero page value can be used.
        var returnValue = new Tuple<byte, AddressingModes>(0, AddressingModes.Absolute);

        if (value <= byte.MaxValue)
        {
            if (!ShouldAddRegister("X") && !ShouldAddRegister("Y"))
            {
                returnValue = new Tuple<byte, AddressingModes>(AddressModeOpCode(AddressingModes.ZeroPage),
                    AddressingModes.ZeroPage);
            }
            else if (ShouldAddRegister("X"))
            {
                returnValue = new Tuple<byte, AddressingModes>(AddressModeOpCode(AddressingModes.ZeroPageX),
                    AddressingModes.ZeroPageX);
            }
            else
            {
                returnValue = new Tuple<byte, AddressingModes>(AddressModeOpCode(AddressingModes.ZeroPageY),
                    AddressingModes.ZeroPageY);
            }
        }

        if (returnValue.Item1 != 0)
        {
            return returnValue;
        }

        if (!ShouldAddRegister("X") && !ShouldAddRegister("Y"))
        {
            returnValue = new Tuple<byte, AddressingModes>(AddressModeOpCode(AddressingModes.Absolute),
                AddressingModes.Absolute);
        }
        else if (ShouldAddRegister("X"))
        {
            returnValue = new Tuple<byte, AddressingModes>(AddressModeOpCode(AddressingModes.AbsoluteX),
                AddressingModes.AbsoluteX);
        }
        else
        {
            returnValue = new Tuple<byte, AddressingModes>(AddressModeOpCode(AddressingModes.AbsoluteY),
                AddressingModes.AbsoluteY);
        }

        return returnValue;
    }

    public byte[] GetAbsoluteAddressingModeParameters(int value, AddressingModes addressingMode)
    {
        if (addressingMode == AddressingModes.ZeroPage || addressingMode == AddressingModes.ZeroPageX
                                                       || addressingMode == AddressingModes.ZeroPageY)
        {
            var parameters = new byte[1];
            parameters[0] = (byte)value.LowWord();

            return parameters;
        }
        else
        {
            var parameters = new byte[2];
            parameters[0] = (byte)value.LowWord();
            parameters[1] = (byte)value.HighWord();

            return parameters;
        }
    }

    public bool OperationIsLabel()
    {
        return !string.IsNullOrWhiteSpace(LabelName);
    }

    public bool OperationIsComment()
    {
        return !string.IsNullOrWhiteSpace(Comment) && string.IsNullOrWhiteSpace(Mnemonic);
    }

    public bool OperationIsCommentOrLabel()
    {
        return (OperationIsComment() || OperationIsLabel()) & string.IsNullOrEmpty(Mnemonic);
    }

    public bool ArgumentIsIndirectValue()
    {
        return !string.IsNullOrWhiteSpace(Argument) &&
               Regex.Match(Argument, RegularExpressionConstants.IndirectValueRegEx).Success;
    }

    public bool ArgumentIsIndirectLabel()
    {
        return !string.IsNullOrWhiteSpace(Argument) &&
               Regex.Match(Argument, RegularExpressionConstants.IndirectLabelRegEx).Success;
    }

    public bool ArgumentIsIndexedIndirectValue()
    {
        return !string.IsNullOrWhiteSpace(Argument) &&
               Regex.Match(Argument, RegularExpressionConstants.IndexedIndirectValueRegEx).Success;
    }

    public bool ArgumentIsIndexedIndirectLabel()
    {
        return !string.IsNullOrWhiteSpace(Argument) &&
               Regex.Match(Argument, RegularExpressionConstants.IndexedIndirectLabelRegEx).Success;
    }

    public string GetParsedLabelName()
    {
        return Regex.Match(Argument!, RegularExpressionConstants.LabelRegEx).Value;
    }

    public bool ArgumentIsIndirectIndexedLabel()
    {
        return !string.IsNullOrWhiteSpace(Argument) &&
               Regex.Match(Argument, RegularExpressionConstants.IndirectIndexedLabelRegEx).Success;
    }

    public bool ArgumentIsIndirectIndexedValue()
    {
        return !string.IsNullOrWhiteSpace(Argument) &&
               Regex.Match(Argument, RegularExpressionConstants.IndirectIndexedValueRegEx).Success;
    }

    /// <summary>
    ///     Gets the current instruction based on the addressing mode of this Operation.
    /// </summary>
    public Instruction GetCurrentInstruction()
    {
        return Definition!.Instructions[(int)ActualAddressingMode!];
    }

    /// <summary>
    ///     Sets up addressing mode based on the size of the value supplied
    /// </summary>
    /// <param name="variableValue">Integer containining the variable value</param>
    public void SetAddressModeFromValue(int variableValue)
    {
        var actualAddressingMode = GetAbsoluteAddressCode(variableValue);

        ActualOpCode = actualAddressingMode.Item1;
        ActualAddressingMode = actualAddressingMode.Item2;

        if (ActualAddressingMode == AddressingModes.Absolute || ActualAddressingMode == AddressingModes.AbsoluteX
                                                             || ActualAddressingMode == AddressingModes.AbsoluteY)
        {
            Parameters = new byte[2];
            Parameters[0] = (byte)variableValue.LowWord();
            Parameters[1] = (byte)variableValue.HighWord();
        }
        else
        {
            Parameters = new byte[1];
            Parameters[0] = (byte)variableValue;
        }
    }
}