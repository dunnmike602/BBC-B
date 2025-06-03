namespace BBC_B_Tests;

using FluentAssertions;
using MLDComputing.Emulators.BBCSim._6502.Extensions;
using MLDComputing.Emulators.BBCSim._6502.Storage;

[TestClass]
public class LsrInstructionTests : TestBase
{
    [TestMethod]
    public void LSR_Accumulator_ShiftsRightAndSetsCarry()
    {
        const string program = @"
        LDA #$81      ; 1000 0001
        LSR A         ; → 0100 0000 (Carry = 1)
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x40);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void LSR_Accumulator_ResultZero()
    {
        const string program = @"
        LDA #$01
        LSR A         ; → 0x00 (Carry = 1, Zero = 1)
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x00);
        Processor.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void LSR_ZeroPage_ShiftsCorrectly()
    {
        const string program = @"
        LDA #$80
        STA $40       ; 1000 0000
        LSR $40       ; → 0100 0000
        BRK
    ";

        AssembleAndRun(program);

        MemoryMap!.ReadByte(0x40).Should().Be(0x40);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void LSR_ZeroPageX_ShiftsCorrectly()
    {
        const string program = @"
        LDX #$05
        LDA #$02
        STA $50       ; 0000 0010
        LSR $4B,X     ; $4B + X = $50 → 0000 0001
        BRK
    ";

        AssembleAndRun(program);

        MemoryMap!.ReadByte(0x50).Should().Be(0x01);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void LSR_Absolute_SetsZeroFlag()
    {
        const string program = @"
        LDA #$01
        STA $1234
        LSR $1234     ; → 0000 0000
        BRK
    ";

        AssembleAndRun(program);

        MemoryMap!.ReadByte(0x1234).Should().Be(0x00);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
    }

    [TestMethod]
    public void LSR_AbsoluteX_SetsCarry()
    {
        const string program = @"
        LDX #$02
        LDA #$05
        STA $3002     ; 0000 0101
        LSR $3000,X   ; → 0000 0010
        BRK
    ";

        AssembleAndRun(program);

        MemoryMap!.ReadByte(0x3002).Should().Be(0x02);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
    }
}