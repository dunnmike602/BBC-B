namespace BBC_B_Tests;

using FluentAssertions;
using MLDComputing.Emulators.BBCSim._6502.Constants;
using MLDComputing.Emulators.BBCSim._6502.Extensions;
using MLDComputing.Emulators.BBCSim._6502.Storage;

[TestClass]
public class BrkInstructionTests : TestBase
{
    private const ushort IrqHandler = 0x4000;

    [TestMethod]
    public void BRK_DoesNotAffectOtherFlags()
    {
        // Arrange
        SetupVectors();

        // Seed all flags
        foreach (Statuses s in Enum.GetValues(typeof(Statuses)))
        {
            Processor!.Status = Processor!.Status.SetBit((Byte)s, Bit.One);
        }

        // Act
        AssembleAndRun("BRK");

        // Carry, Decimal, Zero, Overflow, Negative should remain what they were
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.DecimalMode).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Overflow).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);

        // Only I was forced to 1 (it already was), B and unused are not stored in Status
    }

    [TestMethod]
    public void BRK_IncrementsProgramCounterPastPaddingByte()
    {
        // Arrange
        SetupVectors();

        // Place a dummy byte after BRK (which is padding)
        var program = @"
            BRK
            .byte $FF
            ";

        // Act
        AssembleAndRun(program);

        // Assert

        // PC should point at the padding-byte’s successor (Start+2), but we've vectored away
        // So instead check that the pushed return address = Start+2
        var hi = Processor!.PeekStack((byte)(Processor!.StackPointer + 3));
        var lo = Processor!.PeekStack((byte)(Processor!.StackPointer + 2));

        var actual = (ushort)((hi << 8) | lo);

        // Assert
        actual.Should().Be(StartAddress + 2);
    }

    [TestMethod]
    public void BRK_JumpsToIrqVector()
    {
        // Arrange
        SetupVectors();

        // Act
        AssembleAndRun("BRK");

        // Assert
        Processor!.ProgramCounter.Should().Be(IrqHandler);
    }

    [TestMethod]
    public void BRK_PushesReturnAddressOntoStack()
    {
        // Arrange
        SetupVectors();

        // Act
        AssembleAndRun("BRK");

        var hi = Processor!.PeekStack((byte)(Processor!.StackPointer + 3));
        var lo = Processor!.PeekStack((byte)(Processor!.StackPointer + 2));

        var actual = (ushort)((hi << 8) | lo);

        // Assert
        actual.Should().Be(StartAddress + 2);
    }

    [TestMethod]
    public void BRK_PushesStatusWithBreakAndUnusedBitsSet()
    {
        // Arrange
        SetupVectors();

        // Pre-set some flags so we can see what gets pushed
        Processor!.Status = Processor!.Status.SetBit((Byte)Statuses.Carry, Bit.One);
        Processor!.Status = Processor.Status.SetBit((Byte)Statuses.DecimalMode, Bit.One);

        // Act
        AssembleAndRun("BRK");

        // Assert
        var status = Processor!.PeekStack((byte)(Processor!.StackPointer + 1));

        status.GetBit((Byte)Statuses.Unused).Should().Be(Bit.One);
        status.GetBit((Byte)Statuses.BreakCommand).Should().Be(Bit.One);
        status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        status.GetBit((Byte)Statuses.DecimalMode).Should().Be(Bit.One);
    }

    [TestMethod]
    public void BRK_SetsInterruptDisableFlag()
    {
        // Arrange
        SetupVectors();

        // Clear I first
        Processor!.Status.SetBit((Byte)Statuses.InterruptDisable, Bit.Zero);

        // Act
        AssembleAndRun("BRK");

        // Assert
        Processor.Status.GetBit((Byte)Statuses.InterruptDisable)
            .Should().Be(Bit.One);
    }

    private void SetupVectors()
    {
        Processor!.IsInTestMode = false;

        var low = (byte)(IrqHandler & 0xFF);
        var high = (byte)(IrqHandler >> 8);

        MemoryMap!.WriteByte(MachineConstants.ProcessorSetup.IrqHandlerLowByte, low);
        MemoryMap!.WriteByte(MachineConstants.ProcessorSetup.IrqHandlerHighByte, high);

        // Illegal opcode sentinel
        MemoryMap!.WriteByte(IrqHandler, 2);
    }
}