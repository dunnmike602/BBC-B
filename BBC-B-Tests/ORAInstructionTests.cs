namespace BBC_B_Tests;

using FluentAssertions;
using MLDComputing.Emulators.BBCSim._6502.Extensions;
using MLDComputing.Emulators.BBCSim._6502.Storage;

[TestClass]
public class ORAInstructionTests : TestBase
{
    [TestMethod]
    public void ORA_Immediate_SetsBitsCorrectly()
    {
        const string program = @"
        LDA #$10
        ORA #$44      ; 0x10 | 0x44 = 0x54
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x54);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ORA_Immediate_ResultNegative()
    {
        const string program = @"
        LDA #$01
        ORA #$80      ; Result = 0x81 (negative)
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x81);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
    }

    [TestMethod]
    public void ORA_ZeroPage_ResultZero()
    {
        const string program = @"
        LDA #$00
        STA $20
        ORA $20
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x00);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
    }

    [TestMethod]
    public void ORA_ZeroPageX_WrapsCorrectly()
    {
        const string program = @"
        LDX #$05
        LDA #$10
        STA $30
        LDA #$01
        ORA $2B,X     ; $2B + 5 = $30
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x11);
    }

    [TestMethod]
    public void ORA_Absolute_WorksCorrectly()
    {
        const string program = @"
        LDA #$F0
        STA $1234
        LDA #$0F
        ORA $1234
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0xFF);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void ORA_AbsoluteX_WorksCorrectly()
    {
        const string program = @"
        LDX #$01
        LDA #$01
        STA $1235
        LDA #$02
        ORA $1234,X   ; $1234 + 1 = $1235
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x03);
    }

    [TestMethod]
    public void ORA_AbsoluteY_WorksCorrectly()
    {
        const string program = @"
        LDY #$02
        LDA #$10
        STA $2002
        LDA #$04
        ORA $2000,Y   ; $2000 + 2 = $2002
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x14);
    }

    [TestMethod]
    public void ORA_IndexedIndirect_WorksCorrectly()
    {
        const string program = @"
        LDX #$04
        LDA #$01
        STA $0006
        LDA #$20
        STA $0007
        LDA #$40
        STA $2001
        LDA #$10
        ORA ($02,X)   ; → [$06 + $07] = $2001
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x50);
    }

    [TestMethod]
    public void ORA_IndirectIndexed_WorksCorrectly()
    {
        const string program = @"
        LDA #$03
        STA $000A
        LDA #$30
        STA $000B
        LDY #$01
        LDA #$22
        STA $3004
        LDA #$11
        ORA ($0A),Y   ; [$0A] = $3003, + Y = $3004
        BRK
    ";

        AssembleAndRun(program);

        Processor!.Accumulator.Should().Be(0x33);
    }
}