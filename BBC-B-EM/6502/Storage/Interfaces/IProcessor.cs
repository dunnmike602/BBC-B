namespace MLDComputing.Emulators.BBCSim._6502.Storage.Interfaces;

using Engine;

public interface IProcessor
{
    bool Irq { get; set; }
    bool SingleStepModeOn { get; set; }
    long FrameRate { get; set; }
    long TotalCyclesExecuted { get; set; }
    int ExpectedProcessorSpeed { get; set; }
    long TotalElapsedTicks { get; set; }
    byte Accumulator { get; set; }
    byte IX { get; set; }
    byte IY { get; set; }
    byte Status { get; set; }
    byte BrkIncrement { get; set; }
    int RunSingleFrame();

    void Initialise(int cyclesPerSecond = 2_000_000, int videoFrameRate = 50);

    event InterruptHandler Interrupt;
    void Run(ushort startAddress, bool singleStepModeOn);
    void ResetProcessor();
    long GetActualProcessorSpeed();
    Bit GetStatusFlag(Statuses statusFlag);
    void SetStatusFlag(Statuses statusFlag, Bit value);
    ushort GetProgramCounter();
    byte GetStackPointer();
    byte PeekStack(byte stackPointer);
    bool IsCarrySet();
    bool IsRunning();
    void SetResetHandler(ushort startAddress);
    long GetProcessorMicroSeconds();
    long GetInterruptMicroSeconds();
    void SetProgramCounter(ushort newValue);
}