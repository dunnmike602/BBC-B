namespace BBC_B_Tests;

using FluentAssertions;
using MLDComputing.Emulators.BBCSim._6502.Extensions;
using MLDComputing.Emulators.BBCSim._6502.Storage;

[TestClass]
public class RolInstructionTests : TestBase
{
    [TestMethod]
    public void ROL_Accumulator_WithCarryInAndCarryOut()
    {
        // Arrange
        const string program = @"
            SEC         ; Carry = 1
            LDA #$80    ; %1000_0000
            ROL         ; Expect A = %0000_0001, Carry = 1
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        Processor!.Accumulator.Should().Be(0x01);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ROL_Accumulator_ResultZero()
    {
        // Arrange
        const string program = @"
            CLC
            LDA #$00
            ROL
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        Processor!.Accumulator.Should().Be(0x00);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ROL_Accumulator_ResultNegative()
    {
        // Arrange
        const string program = @"
            CLC
            LDA #$40
            ROL         ; %1000_0000
            BRK
        ";
        // Act
        AssembleAndRun(program);

        // Assert
        Processor!.Accumulator.Should().Be(0x80);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
    }

    [TestMethod]
    public void ROL_ZeroPage_WithCarryIn()
    {
        // Arrange
        const string program = @"
            SEC
            LDA #$01
            STA $10
            ROL $10     ; %0000_0010 + carry -> %0000_0011
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x0010).Should().Be(0x03);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ROL_ZeroPage_SetsCarry()
    {
        // Arrange
        const string program = @"
            CLC
            LDA #$80
            STA $20
            ROL $20     ; %1000_0000 -> %0000_0000, Carry = 1
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x0020).Should().Be(0x00);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ROL_ZeroPageX_Wraparound()
    {
        // Arrange
        const string program = @"
            CLC
            LDX #$FF
            LDA #$01
            STA $00
            ROL $01,X   ; $01 + $FF = $00 (wrap)
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x0000).Should().Be(0x02);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ROL_Absolute_NegativeFlagSet()
    {
        // Arrange
        const string program = @"
            CLC
            LDA #$40
            STA $1234
            ROL $1234   ; becomes 0x80
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x1234).Should().Be(0x80);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
    }

    [TestMethod]
    public void ROL_AbsoluteX_SetsCarry()
    {
        // Arrange
        const string program = @"
            SEC
            LDX #$01
            LDA #$81
            STA $2001
            ROL $2000,X
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x2001).Should().Be(0x03);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ROL_AbsoluteX_ResultZero()
    {
        // Arrange
        const string program = @"
            CLC
            LDX #$01
            LDA #$80
            STA $3001
            ROL $3000,X
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        MemoryMap!.ReadByte(0x3001).Should().Be(0x00);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
    }
}