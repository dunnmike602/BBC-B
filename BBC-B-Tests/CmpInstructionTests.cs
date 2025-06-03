namespace BBC_B_Tests;

using FluentAssertions;
using MLDComputing.Emulators.BBCSim._6502.Extensions;
using MLDComputing.Emulators.BBCSim._6502.Storage;

[TestClass]
public class CmpInstructionTests : TestBase
{
    private void AssertFlags(bool carry, bool zero, bool negative)
    {
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(carry ? Bit.One : Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Zero).Should().Be(zero ? Bit.One : Bit.Zero);
        Processor.Status.GetBit((Byte)Statuses.Negative).Should().Be(negative ? Bit.One : Bit.Zero);
    }

    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // 1) Immediate mode

    [TestMethod]
    public void CMP_Immediate_AgtM()
    {
        AssembleAndRun(@"
            LDA #$90
            CMP #$7F
            BRK
        ");
        AssertFlags(true, false, false);
    }

    [TestMethod]
    public void CMP_Immediate_AeqM()
    {
        AssembleAndRun(@"
            LDA #$80
            CMP #$80
            BRK
        ");
        AssertFlags(true, true, false);
    }

    [TestMethod]
    public void CMP_Immediate_AltM()
    {
        AssembleAndRun(@"
            LDA #$70
            CMP #$90
            BRK
        ");
        AssertFlags(false, false, true);
    }

    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // 2) Zero Page

    [TestMethod]
    public void CMP_ZeroPage_AgtM()
    {
        MemoryMap!.WriteByte(0x0042, 0x50);
        AssembleAndRun(@"
            LDA #$60
            CMP $42
            BRK
        ");
        AssertFlags(true, false, false);
    }

    [TestMethod]
    public void CMP_ZeroPage_AeqM()
    {
        MemoryMap!.WriteByte(0x0042, 0x33);
        AssembleAndRun(@"
            LDA #$33
            CMP $42
            BRK
        ");
        AssertFlags(true, true, false);
    }

    [TestMethod]
    public void CMP_ZeroPage_AltM()
    {
        MemoryMap!.WriteByte(0x0042, 0x99);
        AssembleAndRun(@"
            LDA #$80
            CMP $42
            BRK
        ");
        AssertFlags(false, false, true);
    }

    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // 3) Zero Page, X (wrap)

    [TestMethod]
    public void CMP_ZeroPageX_AgtM()
    {
        Processor!.IX = 1;
        MemoryMap!.WriteByte(0x00 + 1, 0x20);
        AssembleAndRun(@"
            LDA #$25
            LDX #$01
            CMP $00,X
            BRK
        ");
        AssertFlags(true, false, false);
    }

    [TestMethod]
    public void CMP_ZeroPageX_AeqM()
    {
        Processor!.IX = 5;
        MemoryMap!.WriteByte(0x10 + 5, 0xAB);
        AssembleAndRun(@"
            LDA #$AB
            LDX #$05
            CMP $10,X
            BRK
        ");
        AssertFlags(true, true, false);
    }

    [TestMethod]
    public void CMP_ZeroPageX_AltM()
    {
        Processor!.IX = 0xFF;
        // 0x0A+0xFF wraps to 0x09
        MemoryMap!.WriteByte((0x0A + 0xFF) & 0xFF, 0x80);
        AssembleAndRun(@"
            LDA #$50
            LDX #$FF
            CMP $0A,X
            BRK
        ");
        AssertFlags(false, false, true);
    }

    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // 4) Absolute

    [TestMethod]
    public void CMP_Absolute_AgtM()
    {
        MemoryMap!.WriteByte(0x1234, 0x10);
        AssembleAndRun(@"
            LDA #$20
            CMP $1234
            BRK
        ");
        AssertFlags(true, false, false);
    }

    [TestMethod]
    public void CMP_Absolute_AeqM()
    {
        MemoryMap!.WriteByte(0x1234, 0x7F);
        AssembleAndRun(@"
            LDA #$7F
            CMP $1234
            BRK
        ");
        AssertFlags(true, true, false);
    }

    [TestMethod]
    public void CMP_Absolute_AltM()
    {
        MemoryMap!.WriteByte(0x1234, 0xF0);
        AssembleAndRun(@"
            LDA #$0F
            CMP $1234
            BRK
        ");
        AssertFlags(false, false, false);
    }

    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // 5) Absolute,X

    [TestMethod]
    public void CMP_AbsoluteX_AgtM()
    {
        Processor!.IX = 2;
        MemoryMap!.WriteByte(0x2000 + 2, 0x30);
        AssembleAndRun(@"
            LDA #$40
            LDX #$02
            CMP $2000,X
            BRK
        ");
        AssertFlags(true, false, false);
    }

    [TestMethod]
    public void CMP_AbsoluteX_AeqM()
    {
        Processor!.IX = 3;
        MemoryMap!.WriteByte(0x2000 + 3, 0x55);
        AssembleAndRun(@"
            LDA #$55
            LDX #$03
            CMP $2000,X
            BRK
        ");
        AssertFlags(true, true, false);
    }

    [TestMethod]
    public void CMP_AbsoluteX_AltM()
    {
        Processor!.IX = 1;
        MemoryMap!.WriteByte(0x2000 + 1, 0x90);
        AssembleAndRun(@"
            LDA #$10
            LDX #$01
            CMP $2000,X
            BRK
        ");
        AssertFlags(false, false, true);
    }

    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // 6) Absolute,Y

    [TestMethod]
    public void CMP_AbsoluteY_AgtM()
    {
        Processor!.IY = 4;
        MemoryMap!.WriteByte(0x3000 + 4, 0x01);
        AssembleAndRun(@"
            LDA #$02
            LDY #$04
            CMP $3000,Y
            BRK
        ");
        AssertFlags(true, false, false);
    }

    [TestMethod]
    public void CMP_AbsoluteY_AeqM()
    {
        Processor!.IY = 10;
        MemoryMap!.WriteByte(0x3000 + 10, 0xA5);
        AssembleAndRun(@"
            LDA #$A5
            LDY #$0A
            CMP $3000,Y
            BRK
        ");
        AssertFlags(true, true, false);
    }

    [TestMethod]
    public void CMP_AbsoluteY_AltM()
    {
        Processor!.IY = 0xFF;
        MemoryMap!.WriteByte(0x3000 + 0xFF, 0x77);
        AssembleAndRun(@"
            LDA #$10
            LDY #$FF
            CMP $3000,Y
            BRK
        ");
        AssertFlags(false, false, true);
    }

    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // 7) Indexed-Indirect (ZP,X)

    [TestMethod]
    public void CMP_IndexedIndirect_AgtM()
    {
        // ZP pointer at $10+X → $11
        Processor!.IX = 1;
        byte zp = 0x10;
        MemoryMap!.WriteByte((byte)(zp + 1), 0x00); // LSB
        MemoryMap.WriteByte((byte)(zp + 1 + 1), 0x20); // MSB → address $2000
        MemoryMap.WriteByte(0x2000, 0x05);

        AssembleAndRun(@"
            LDA #$06
            LDX #$01
            CMP ($10,X)
            BRK
        ");
        AssertFlags(true, false, false);
    }

    [TestMethod]
    public void CMP_IndexedIndirect_AeqM()
    {
        Processor!.IX = 2;
        byte zp = 0x20;
        MemoryMap!.WriteByte((byte)(zp + 2), 0x34);
        MemoryMap.WriteByte((byte)(zp + 2 + 1), 0x12); // pointer → $1234
        MemoryMap.WriteByte(0x1234, 0xAA);

        AssembleAndRun(@"
            LDA #$AA
            LDX #$02
            CMP ($20,X)
            BRK
        ");
        AssertFlags(true, true, false);
    }

    [TestMethod]
    public void CMP_IndexedIndirect_AltM()
    {
        Processor!.IX = 0xFF;
        // wraps to $FF, pointer at $FF→LSB,$00→MSB
        MemoryMap!.WriteByte(0xFF, 0x34);
        MemoryMap.WriteByte(0x00, 0x02); // pointer→$0234
        MemoryMap.WriteByte(0x0234, 0x80);

        AssembleAndRun(@"
            LDA #$10
            LDX #$FF
            CMP ($00,X)
            BRK
        ");
        AssertFlags(false, false, true);
    }

    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // 8) Indirect-Indexed (ZP),Y

    [TestMethod]
    public void CMP_IndirectIndexed_AgtM()
    {
        Processor!.IY = 3;
        byte zp = 0x30;
        MemoryMap!.WriteByte(zp, 0x00);
        MemoryMap.WriteByte((byte)(zp + 1), 0x20); // base $2000 + Y=3 → $2003
        MemoryMap.WriteByte(0x2003, 0x11);

        AssembleAndRun(@"
            LDA #$12
            LDY #$03
            CMP ($30),Y
            BRK
        ");
        AssertFlags(true, false, false);
    }

    [TestMethod]
    public void CMP_IndirectIndexed_AeqM()
    {
        Processor!.IY = 7;
        byte zp = 0x40;
        MemoryMap!.WriteByte(zp, 0x34);
        MemoryMap.WriteByte((byte)(zp + 1), 0x12); // $1234+7 → $123B
        MemoryMap.WriteByte(0x123B, 0x77);

        AssembleAndRun(@"
            LDA #$77
            LDY #$07
            CMP ($40),Y
            BRK
        ");
        AssertFlags(true, true, false);
    }

    [TestMethod]
    public void CMP_IndirectIndexed_AltM()
    {
        Processor!.IY = 0xFF;
        byte zp = 0x50;
        MemoryMap!.WriteByte(zp, 0x00);
        MemoryMap.WriteByte((byte)(zp + 1), 0x20); // $2000+255 → $20FF
        MemoryMap.WriteByte(0x20FF, 0xCD);

        AssembleAndRun(@"
            LDA #$10
            LDY #$FF
            CMP ($50),Y
            BRK
        ");
        AssertFlags(false, false, false);
    }
}