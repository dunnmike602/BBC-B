namespace BBC_B_Tests;

using MLDComputing.Emulators.BBCSim.Beeb.Hardware;

[TestClass]
public class Via6522Tests
{
    private bool _irqCleared;
    private bool _irqRaised;
    private Via6522 _via = null!;

    [TestInitialize]
    public void Init()
    {
        _irqRaised = false;
        _irqCleared = false;

        _via = new Via6522(null!, null!, null!, null!, null!, null!);
        _via.SetInterruptHandlers(
            () => _irqRaised = true,
            () => _irqCleared = true
        );
    }

    [TestMethod]
    public void TimerInterruptFiresWhenEnabled()
    {
        // Arrange: Set Timer 1 latch to fire in 256 cycles
        _via.Write(0x04, 0xFF); // Timer 1 low byte
        _via.Write(0x05, 0x00); // Timer 1 high byte
        _via.Write(0x0B, 0x40); // ACR: Timer 1 one-shot mode
        _via.Write(0x0E, 0xC1); // IER: Set bit 7 and enable Timer 1 interrupt

        // Act: Tick for enough cycles
        for (var i = 0; i < 256; i++)
        {
            _via.Tick();
        }

        // Assert
        Assert.IsTrue(_irqRaised, "IRQ should be raised after Timer 1 expires.");
    }

    [TestMethod]
    public void TimerInterruptDoesNotFireWhenDisabled()
    {
        // Arrange: Do not enable interrupt in IER
        _via.Write(0x04, 0xFF);
        _via.Write(0x05, 0x00);
        _via.Write(0x0B, 0x40); // Timer 1 one-shot
        _via.Write(0x0E, 0x01); // Missing bit 7: should disable interrupt

        // Act
        for (var i = 0; i < 256; i++)
        {
            _via.Tick();
        }

        // Assert
        Assert.IsFalse(_irqRaised, "IRQ should not be raised if interrupt is not enabled.");
    }

    [TestMethod]
    public void InterruptClearsCorrectly()
    {
        // Arrange
        _via.Write(0x04, 0xFF);
        _via.Write(0x05, 0x00);
        _via.Write(0x0B, 0x40);
        _via.Write(0x0E, 0xC1);

        for (var i = 0; i < 256; i++)
        {
            _via.Tick();
        }

        // Clear the interrupt
        _via.Read(0x0D); // IFR read to acknowledge

        // Act
        _via.Tick(); // Tick one more time to trigger clear if needed

        // Assert
        Assert.IsTrue(_irqRaised);
        Assert.IsTrue(_irqCleared);
    }
}