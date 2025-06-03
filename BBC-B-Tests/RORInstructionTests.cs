namespace BBC_B_Tests;

using FluentAssertions;
using MLDComputing.Emulators.BBCSim._6502.Extensions;
using MLDComputing.Emulators.BBCSim._6502.Storage;

[TestClass]
public class RorInstructionTests : TestBase
{
    [TestMethod]
    public void ROR_Accumulator_WithCarryInAndCarryOut()
    {
        // Arrange
        const string program = @"
            SEC         ; Set carry to 1
            LDA #$01    ; %0000_0001
            ROR         ; Expect A = %1000_0000, Carry = 1
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        Processor!.Accumulator.Should().Be(0x80);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
    }

    [TestMethod]
    public void ROR_ZeroPage_CarryOutAndCarryIn()
    {
        // Arrange
        const string program = @"
            SEC
            LDA #$03
            STA $10
            ROR $10     ; %0000_0011 -> %1000_0001
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x0010).Should().Be(0x81);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
    }

    [TestMethod]
    public void ROR_ZeroPageX_WithZeroResult()
    {
        // Arrange
        const string program = @"
            CLC
            LDX #$02
            LDA #$02
            STA $12     ; $10 + X
            ROR $10,X
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x0012).Should().Be(0x01);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ROR_Absolute_SetsZeroFlag()
    {
        // Arrange
        const string program = @"
            CLC
            LDA #$01
            STA $1234
            ROR $1234
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x1234).Should().Be(0x00);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ROR_AbsoluteX_ShiftWithCarryInAndNegative()
    {
        // Arrange
        const string program = @"
            SEC
            LDX #$01
            LDA #$FE
            STA $2001
            ROR $2000,X
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x2001).Should().Be(0xFF);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
    }

    [TestMethod]
    public void ROR_Accumulator_ResultZero_SetsZeroFlag()
    {
        // Arrange
        const string program = @"
            CLC
            LDA #$01
            ROR
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        Processor!.Accumulator.Should().Be(0x00);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ROR_ZeroPage_SetsNegativeFlag()
    {
        // Arrange
        const string program = @"
            SEC
            LDA #$02
            STA $20
            ROR $20
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x0020).Should().Be(0x81);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
    }

    [TestMethod]
    public void ROR_ZeroPageX_AddressWrapsCorrectly()
    {
        // Arrange
        const string program = @"
            CLC
            LDX #$FF
            LDA #$04
            STA $00
            ROR $01,X
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x0000).Should().Be(0x02);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ROR_Accumulator_NoFlagsSet()
    {
        // Arrange
        const string program = @"
            CLC
            LDA #$04
            ROR
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        Processor!.Accumulator.Should().Be(0x02);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }
}