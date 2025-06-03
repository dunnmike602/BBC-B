namespace BBC_B_Tests;

using FluentAssertions;
using MLDComputing.Emulators.BBCSim._6502.Extensions;
using MLDComputing.Emulators.BBCSim._6502.Storage;

[TestClass]
public class RtiInstructionTests : TestBase
{
    [TestMethod]
    public void RTI_ShouldRestoreProcessorState()
    {
        // Arrange
        const string program = @"
            SEI           ; I = 1
            LDX #$FF
            TXS           ; SP = $FF
            LDA #$C1      ; high byte = $C1
            PHA
            LDA #$23      ; low  byte = $23
            PHA
            LDA #%10100100
            PHA
            RTI
            BRK
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        Processor!.ProgramCounter.Should().Be(0xC123);
        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.InterruptDisable).Should().Be(Bit.One);


        // Break flag must be cleared after RTI
        Processor!.Status.GetBit((Byte)Statuses.BreakCommand).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void RTI_ShouldClearBreakFlag()
    {
        const string program = @"
            LDX #$FF       ; Stack pointer at $01FF
            TXS

            LDA #$12       ; High byte of return address
            PHA

            LDA #$34       ; Low byte of return address
            PHA

            LDA #$10       ; Status
            PHA            ; Status pushed last — sits on top

            RTI            ; Should now pull status, then $34, then $12 → PC = $1234
            BRK            ; Should not be reached
                                ";

        AssembleAndRun(program);

        Processor!.ProgramCounter.Should().Be(0x1234);

        Processor!.Status.GetBit((Byte)Statuses.BreakCommand).Should().Be(Bit.Zero);
    }

    [TestMethod]
    public void RTI_ShouldRestoreAllStandardFlags()
    {
        // Arrange
        const string program = @"
            SEI         ; disable interrupts so flags don’t change
            LDX #$FF
            TXS         ; Reset SP to $FF

            LDA #$AB    ; Push the “return” PC high byte
            PHA
            LDA #$CD    ; Push the “return” PC low  byte
            PHA
            LDA #$C3    ; Load the desired status byte
            PHA         ; Push it

            RTI         ; Pull Status, PCL, PCH → resume at $CDAB
            BRK         ; (should never get here)
        ";

        // Act
        AssembleAndRun(program);

        // Assert
        Processor!.ProgramCounter.Should().Be(0xABCD);

        Processor!.Status.GetBit((Byte)Statuses.Negative).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Overflow).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.DecimalMode).Should().Be(Bit.Zero); // Not present in most 6502 variants
        Processor!.Status.GetBit((Byte)Statuses.InterruptDisable).Should().Be(Bit.Zero);
        Processor!.Status.GetBit((Byte)Statuses.Zero).Should().Be(Bit.One);
        Processor!.Status.GetBit((Byte)Statuses.Carry).Should().Be(Bit.One);
    }
}