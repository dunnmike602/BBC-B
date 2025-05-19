namespace MLDComputing.Emulators.BBCSim._6502.Disassembler;

using Assembler;
using Assembler.Validators;
using Engine;
using Extensions;
using Interfaces;

public class AddressArgumentProcessor : IAddressArgumentProcessor
{
    public string? MapToString(AddressingModes addressingMode, byte[] operands, int radix)
    {
        switch (addressingMode)
        {
            case AddressingModes.Immediate:
                return ImmediateAddressModeValidator.ImmediateModeIdentifier +
                       ((int)operands[0]).ConvertToBaseWithPrefix(radix, byte.MaxValue);

            case AddressingModes.Implied:
                return string.Empty;

            case AddressingModes.Accumulator:
                return AccumulatorAddressModeValidator.AccumatorAddressIdentifier;

            case AddressingModes.ZeroPage:
                return ((int)operands[0]).ConvertToBaseWithPrefix(radix, byte.MaxValue);

            case AddressingModes.ZeroPageX:
                return ((int)operands[0]).ConvertToBaseWithPrefix(radix, byte.MaxValue) + ",X";

            case AddressingModes.ZeroPageY:
                return ((int)operands[0]).ConvertToBaseWithPrefix(radix, byte.MaxValue) + ",Y";

            case AddressingModes.Relative:
                return ((int)operands[0]).ConvertToBaseWithPrefix(radix, byte.MaxValue);

            case AddressingModes.Absolute:
                return (operands[0] + operands[1] * Cpu6502.MsbMultiplier).ConvertToBaseWithPrefix(radix,
                    ushort
                        .MaxValue);

            case AddressingModes.AbsoluteX:
                return (operands[0] + operands[1] * Cpu6502.MsbMultiplier).ConvertToBaseWithPrefix(radix,
                           ushort
                               .MaxValue) +
                       ",X";

            case AddressingModes.AbsoluteY:
                return (operands[0] + operands[1] * Cpu6502.MsbMultiplier).ConvertToBaseWithPrefix(radix,
                           ushort
                               .MaxValue) +
                       ",Y";

            case AddressingModes.Indirect:
                return "(" + (operands[0] + operands[1] * Cpu6502.MsbMultiplier).ConvertToBaseWithPrefix(radix,
                    ushort.MaxValue) + ")";

            case AddressingModes.IndexedIndirect:
                return "(" + ((int)operands[0]).ConvertToBaseWithPrefix(radix, byte.MaxValue) + ",X)";

            case AddressingModes.IndirectIndexed:
                return "(" + ((int)operands[0]).ConvertToBaseWithPrefix(radix, byte.MaxValue) + "),Y";
        }

        return string.Empty;
    }
}