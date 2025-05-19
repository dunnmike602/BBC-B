namespace MLDComputing.Emulators.BBCSim._6502.Assembler;

using System.Text.RegularExpressions;
using Constants;
using Interfaces;

public class Tokeniser : ITokeniser
{
    public Operation[] Parse(string program)
    {
        var operations = new List<Operation>();

        if (string.IsNullOrWhiteSpace(program))
        {
            return operations.ToArray();
        }

        var opStrings = program.Split([Environment.NewLine], StringSplitOptions.None).Select(l => l.Trim());

        var instructionNumber = 0;

        foreach (var opString in opStrings)
        {
            var isBlankLine = GetIsBlankLine(opString);

            if (!isBlankLine)
            {
                var operation = new Operation
                {
                    OriginalSource = opString,
                    Mnemonic = GetMnemonic(opString),
                    Argument = GetArgument(opString),
                    Comment = GetComment(opString),
                    LabelName = GetLabel(opString),
                    InstructionNumber = instructionNumber
                };

                operations.Add(operation);
            }

            instructionNumber++;
        }

        return operations.ToArray();
    }

    private string GetMnemonic(string source)
    {
        if ((source.IndexOf(TokenConstants.CommentStartChar, StringComparison.Ordinal) > -1 &&
             source.IndexOf(TokenConstants.CommentStartChar, StringComparison.Ordinal) < 3) ||
            source.Trim() == GetLabel(source))
        {
            // Contains comment only or is a label
            return string.Empty;
        }

        if (IsBytePseudoOp(source))
        {
            return Data.BYTE;
        }

        if (IsAddressPseudoOp(source))
        {
            return Data.ORG;
        }

        if (IsVariablePseudoOp(source))
        {
            return Data.VAR;
        }

        return source.PadRight(3).Substring(0, 3).ToUpper();
    }

    private string GetArgument(string source)
    {
        var commentStart = source.IndexOf(TokenConstants.CommentStartChar, StringComparison.Ordinal);

        if ((commentStart > -1 && commentStart < 3) || source.Trim() == GetLabel(source))
        {
            // Contains comment only or is a label
            return string.Empty;
        }

        var instructionLength = GetInstructionLength(source);

        if (commentStart > -1)
        {
            return source.Substring(instructionLength, commentStart - 4).Trim();
        }

        return source.Length < instructionLength ? string.Empty : source.Substring(instructionLength).Trim();
    }

    private int GetInstructionLength(string source)
    {
        if (IsBytePseudoOp(source))
        {
            return Data.BYTE.Length;
        }

        if (IsAddressPseudoOp(source))
        {
            return Data.ORG.Length;
        }

        if (IsVariablePseudoOp(source))
        {
            return Data.VAR.Length;
        }

        return TokenConstants.OpCodeSize;
    }

    private static bool IsBytePseudoOp(string source)
    {
        return source.PadRight(Data.BYTE.Length).Substring(0, Data.BYTE.Length).ToUpper() == Data.BYTE;
    }

    private static bool IsAddressPseudoOp(string source)
    {
        return source.PadRight(Data.ORG.Length).Substring(0, Data.ORG.Length).ToUpper() == Data.ORG;
    }

    private static bool IsVariablePseudoOp(string source)
    {
        return source.PadRight(Data.VAR.Length).Substring(0, Data.VAR.Length).ToUpper() == Data.VAR;
    }

    private string GetComment(string source)
    {
        var commentLocation = source.IndexOf(TokenConstants.CommentStartChar, StringComparison.Ordinal);

        return commentLocation != -1 ? source.Substring(commentLocation) : string.Empty;
    }

    private string GetLabel(string source)
    {
        source = source.Trim();

        var label = Regex.Match(source, "[^ ]*:").Value;

        if (source == label)
        {
            // A label is text followed by : any other text is ignored
            return label;
        }

        return string.Empty;
    }

    private bool GetIsBlankLine(string text)
    {
        return string.IsNullOrWhiteSpace(text);
    }
}