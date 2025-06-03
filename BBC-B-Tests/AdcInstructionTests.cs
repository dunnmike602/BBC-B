namespace BBC_B_Tests;

using FluentAssertions;
using MLDComputing.Emulators.BBCSim._6502.Extensions;
using MLDComputing.Emulators.BBCSim._6502.Storage;

[TestClass]
public class AdcInstructionTests : TestBase
{
    [TestMethod]
    public void ADC_Immediate_WithoutCarry()
    {
        const string program = @"
            CLC
            LDA #$10
            ADC #$20    ; 0x10 + 0x20 = 0x30
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x30);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Overflow).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ADC_Immediate_WithCarry()
    {
        const string program = @"
            SEC
            LDA #$01
            ADC #$01    ; 0x01 + 0x01 + 1 = 0x03
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x03);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ADC_Immediate_SetsCarryFlag()
    {
        const string program = @"
            CLC
            LDA #$F0
            ADC #$20    ; 0xF0 + 0x20 = 0x110 → 0x10 with carry
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x10);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
    }

    [TestMethod]
    public void ADC_Immediate_SetsOverflowFlag()
    {
        const string program = @"
            CLC
            LDA #$50
            ADC #$50    ; 0x50 + 0x50 = 0xA0, overflow because both operands were positive
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0xA0);
        Processor.Status.GetBit((Byte)Statuses.Overflow).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
    }

    [TestMethod]
    public void ADC_Immediate_ResultIsZero()
    {
        const string program = @"
            SEC
            LDA #$FF
            ADC #$01    ; 0xFF + 0x01 + 1 = 0x101 → 0x00
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x01);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ADC_ZeroPage_AddsCorrectly()
    {
        const string program = @"
            LDA #$10
            STA $10
            CLC
            LDA #$10
            ADC $10     ; 0x10 + 0x10 = 0x20
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x20);
    }

    [TestMethod]
    public void ADC_ZeroPageX_AddsCorrectly()
    {
        const string program = @"
            LDX #$01
            LDA #$20
            STA $21
            CLC
            LDA #$10
            ADC $20,X
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x30);
    }

    [TestMethod]
    public void ADC_Absolute_AddsCorrectly()
    {
        const string program = @"
            LDA #$20
            STA $1234
            CLC
            LDA #$10
            ADC $1234
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x30);
    }

    [TestMethod]
    public void ADC_AbsoluteX_AddsCorrectly()
    {
        const string program = @"
            LDX #$01
            LDA #$30
            STA $1235
            CLC
            LDA #$10
            ADC $1234,X
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x40);
    }

    [TestMethod]
    public void ADC_AbsoluteY_AddsCorrectly()
    {
        const string program = @"
            LDY #$01
            LDA #$22
            STA $1235
            CLC
            LDA #$11
            ADC $1234,Y
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x33);
    }

    [TestMethod]
    public void ADC_IndirectX_AddsCorrectly()
    {
        const string program = @"
            LDX #$02
            LDA #$78
            STA $0002
            LDA #$56
            STA $0003
            LDA #$01
            STA $5678
            CLC
            LDA #$01
            ADC ($00,X)
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x02);
    }

    [TestMethod]
    public void ADC_IndirectY_AddsCorrectly()
    {
        const string program = @"
            LDY #$01
            LDA #$44
            STA $10
            LDA #$22
            STA $11
            LDA #$10
            STA $2245
            CLC
            LDA #$10
            ADC ($10),Y
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x20);
    }
}