namespace BBC_B_Tests;

using FluentAssertions;
using MLDComputing.Emulators.BBCSim._6502.Extensions;
using MLDComputing.Emulators.BBCSim._6502.Storage;

[TestClass]
public class SbcInstructionTests : TestBase
{
    [TestMethod]
    public void SBC_Immediate_WithoutBorrow()
    {
        const string program = @"
            SEC
            LDA #$30
            SBC #$10    ; 0x30 - 0x10 = 0x20
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x20);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Overflow).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void SBC_Immediate_WithBorrow()
    {
        const string program = @"
            CLC
            LDA #$30
            SBC #$10    ; 0x30 - 0x10 - 1 = 0x1F
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x1F);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
    }

    [TestMethod]
    public void SBC_Immediate_SetsCarryFlag()
    {
        const string program = @"
            SEC
            LDA #$10
            SBC #$20    ; 0x10 - 0x20 = borrow → result = 0xF0, carry cleared
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0xF0);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
    }

    [TestMethod]
    public void SBC_Immediate_SetsOverflowFlag()
    {
        const string program = @"
            SEC
            LDA #$80
            SBC #$01    ; 0x80 - 0x01 = 0x7F (overflow, sign flipped)
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x7F);
        Processor!.Status.GetBit((Byte)Statuses.Overflow).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void SBC_Immediate_ResultIsZero()
    {
        const string program = @"
            SEC
            LDA #$10
            SBC #$10    ; 0x10 - 0x10 = 0
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x00);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
    }

    [TestMethod]
    public void SBC_ZeroPage_SubtractsCorrectly()
    {
        const string program = @"
            LDA #$30
            STA $10
            SEC
            LDA #$50
            SBC $10     ; 0x50 - 0x30 = 0x20
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x20);
    }

    [TestMethod]
    public void SBC_ZeroPageX_SubtractsCorrectly()
    {
        const string program = @"
            LDX #$01
            LDA #$40
            STA $21
            SEC
            LDA #$60
            SBC $20,X   ; 0x60 - 0x40 = 0x20
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x20);
    }

    [TestMethod]
    public void SBC_Absolute_SubtractsCorrectly()
    {
        const string program = @"
            LDA #$11
            STA $1234
            SEC
            LDA #$22
            SBC $1234   ; 0x22 - 0x11 = 0x11
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x11);
    }

    [TestMethod]
    public void SBC_AbsoluteX_SubtractsCorrectly()
    {
        const string program = @"
            LDX #$01
            LDA #$11
            STA $1235
            SEC
            LDA #$22
            SBC $1234,X ; 0x22 - 0x11 = 0x11
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x11);
    }

    [TestMethod]
    public void SBC_AbsoluteY_SubtractsCorrectly()
    {
        const string program = @"
            LDY #$01
            LDA #$01
            STA $1235
            SEC
            LDA #$03
            SBC $1234,Y ; 0x03 - 0x01 = 0x02
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x02);
    }

    [TestMethod]
    public void SBC_IndirectX_SubtractsCorrectly()
    {
        const string program = @"
            LDX #$02
            LDA #$78
            STA $0002
            LDA #$56
            STA $0003
            LDA #$01
            STA $5678
            SEC
            LDA #$03
            SBC ($00,X) ; 0x03 - 0x01 = 0x02
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x02);
    }

    [TestMethod]
    public void SBC_IndexedIndirect_SubtractWithBorrow()
    {
        // Arrange
        const string program = @"
        SEC             ; Set Carry = 1 (no borrow)
        LDX #$04        ; X = 0x04
        LDA #$50        ; A = 0x50
        SBC ($02,X)     ; Effective address = word at $06 = $3000
        BRK
    ";

        // Set pointer address ($02 + X = $06)
        MemoryMap!.WriteByte(0x06, 0x00); // low byte of $3000
        MemoryMap!.WriteByte(0x07, 0x30); // high byte of $3000
        MemoryMap!.WriteByte(0x3000, 0x20); // value at $3000

        // Act
        AssembleAndRun(program);

        // Assert
        Processor!.Accumulator.Should().Be(0x30);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One); // No borrow
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Overflow).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void SBC_IndirectY_SubtractsCorrectly()
    {
        const string program = @"
            LDY #$01
            LDA #$44
            STA $10
            LDA #$22
            STA $11
            LDA #$01
            STA $2245
            SEC
            LDA #$03
            SBC ($10),Y ; 0x03 - 0x01 = 0x02
            BRK
        ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x02);
    }

    // Decimal Mode tests
    [TestMethod]
    public void SBC_Decimal_NoBorrow_Simple()
    {
        const string program = @"
        SED         ; Enable decimal mode
        SEC         ; Set carry = 1 (no borrow)
        LDA #$25    ; A = 25 (BCD)
        SBC #$12    ; A = 25 - 12 = 13
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x13); // 13 decimal
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void SBC_Decimal_WithBorrow()
    {
        const string program = @"
        SED
        CLC         ; Carry = 0 → borrow
        LDA #$25    ; A = 25
        SBC #$12    ; A = 25 - 12 - 1 = 12 (BCD)
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x12); // 18 - 1 = 17, in decimal BCD
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One); // Still no real borrow
    }

    [TestMethod]
    public void SBC_Decimal_ResultZero()
    {
        const string program = @"
        SED
        SEC
        LDA #$34
        SBC #$34    ; 34 - 34 = 00
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x00);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One); // No borrow
    }

    [TestMethod]
    public void SBC_Decimal_Underflow()
    {
        const string program = @"
        SED
        SEC
        LDA #$12
        SBC #$25    ; 12 - 25 = -13 → BCD = 87, borrow
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x87); // BCD wraparound: 100 - 13 = 87
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero); // Borrow occurred
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
    }

    [TestMethod]
    public void SBC_Decimal_BorrowAcrossDigits()
    {
        const string program = @"
        SED
        SEC
        LDA #$40
        SBC #$01    ; 40 - 01 = 39
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x39); // Decimal subtraction
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
    }

    [TestMethod]
    public void SBC_Decimal_BorrowDueToClearCarry()
    {
        const string program = @"
        SED
        CLC
        LDA #$40
        SBC #$01    ; 40 - 1 - 1 = 38
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x38); // Decimal result with carry-in subtracted
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One); // Still no borrow
    }

    [TestMethod]
    public void ADC_Decimal_SimpleAddition_NoCarry()
    {
        const string program = @"
        SED
        CLC
        LDA #$25    ; 25 BCD
        ADC #$12    ; +12 = 37 BCD
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x37);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ADC_Decimal_SimpleAddition_WithCarryIn()
    {
        const string program = @"
        SED
        SEC
        LDA #$25    ; 25 BCD
        ADC #$12    ; +12 + 1 = 38 BCD
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x38);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ADC_Decimal_AdditionWithCarryOut()
    {
        const string program = @"
        SED
        CLC
        LDA #$58    ; 58
        ADC #$45    ; +45 = 103 → BCD = 03, Carry out
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x03);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One); // Overflow past 99
    }

    [TestMethod]
    public void ADC_Decimal_CarryInAndCarryOut()
    {
        const string program = @"
        SED
        SEC
        LDA #$58    ; 58
        ADC #$45    ; +45 + 1 = 104 → BCD = 04, Carry out
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x04);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
    }

    [TestMethod]
    public void ADC_Decimal_ResultZero()
    {
        const string program = @"
        SED
        CLC
        LDA #$00
        ADC #$00
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x00);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ADC_Decimal_LowDigitCarry()
    {
        const string program = @"
        SED
        CLC
        LDA #$19
        ADC #$02    ; 19 + 2 = 21 BCD (with low digit wrap)
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x21);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ADC_Decimal_HighDigitCarry()
    {
        const string program = @"
        SED
        CLC
        LDA #$99
        ADC #$01    ; 99 + 1 = 00 with carry out
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x00);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
    }

    [TestMethod]
    public void ADC_Decimal_OverflowFlagShouldBeCorrect()
    {
        const string program = @"
        SED
        CLC
        LDA #$40    ; +64
        ADC #$40    ; +64 = 128 → Overflow (signed)
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x80); // BCD 80
        Processor.Status.GetBit((Byte)Statuses.Overflow).Should().Be(Bit.One);
    }
}