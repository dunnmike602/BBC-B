namespace BBC_B_Tests;

using FluentAssertions;
using MLDComputing.Emulators.BBCSim._6502.Extensions;
using MLDComputing.Emulators.BBCSim._6502.Storage;

[TestClass]
public class AslInstructionTests : TestBase
{
    [TestMethod]
    public void ASL_Accumulator_CarryAndZero()
    {
        // Arrange
        const string program = @"
            LDA #$80
            ASL
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        Processor!.Accumulator.Should().Be(0x00);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ASL_Accumulator_SetsNegative()
    {
        // Arrange
        const string program = @"
            LDA #$40
            ASL
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        Processor!.Accumulator.Should().Be(0x80);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
    }

    [TestMethod]
    public void ASL_ZeroPage_ShiftWithCarry()
    {
        // Arrange
        const string program = @"
            LDA #$81
            STA $10
            ASL $10
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x0010).Should().Be(0x02);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ASL_ZeroPageX_ShiftWithNegative()
    {
        // Arrange
        const string program = @"
            LDX #$02
            LDA #$7F
            STA $12
            ASL $10,X
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x0012).Should().Be(0xFE);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
    }

    [TestMethod]
    public void ASL_Absolute_ShiftWithNoFlags()
    {
        // Arrange
        const string program = @"
            LDA #$01
            STA $1234
            ASL $1234
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x1234).Should().Be(0x02);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ASL_AbsoluteX_ShiftWithCarryAndNegative()
    {
        // Arrange
        const string program = @"
            LDX #$01
            LDA #$FF
            STA $2001
            ASL $2000,X
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x2001).Should().Be(0xFE);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
    }
}