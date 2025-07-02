namespace MLDComputing.Emulators.BBCSim.Beeb;

using System.Diagnostics;
using _6502.Constants;
using _6502.Engine;
using _6502.Engine.Communication;
using Hardware;

public class BeebEm
{
    private readonly long _frameIntervalTicks = Stopwatch.Frequency / 50;

    public double CpuSpeedMhz;

    public int FrameCount;

    public double FrameRate;

    public volatile bool MachineIsRunning;

    public BeebEm()
    {
        BitmapVideoRenderer = new BitmapVideoRenderer();

        SpeechChip = new SpeechChip();
        SoundChip = new SoundChip();
        RomBank = new RomBank();
        Mc6850Acia = new Mc6850Acia();
        SerialULA = new SerialULA();
        Via6522 = new Via6522();
        SystemVia = new SysVia(Via6522);
        OsRom = new OsRom();
        IoDevices = new IoDevices(SystemVia, RomBank, Mc6850Acia, SerialULA);
        Memory = new MemoryMap(RomBank, IoDevices, OsRom);
        Video = new VideoSystem(Memory);
        Sound = new SoundSystem();

        CPU = new Cpu6502(Memory.ReadByte, Memory.WriteByte);

        SystemVia.AttachIrq(() => CPU.RaiseIrq(), () => CPU.ClearIrq());

        SystemVia.AttachPeripheralHandlers(
            SetCapsLockLed,
            SetShiftLockLed,
            UpdateVideoBaseAddress,
            HandleAutoScanChange,
            EnableSpeechChip,
            EnableSoundChip
        );
    }

    public event LightChangedHandler? LightChanged;

    private void SetCapsLockLed(bool on)
    {
        LightChanged?.Invoke(this,
            new LightChangedEventArgs
            {
                IsOn = on,
                Type = LEDType.CapsLock
            });
    }

    private void SetShiftLockLed(bool on)
    {
        LightChanged?.Invoke(this,
            new LightChangedEventArgs
            {
                IsOn = on,
                Type = LEDType.ShiftLock
            });
    }

    private void UpdateVideoBaseAddress(ushort addr)
    {
        // Optionally reconfigure video rendering logic here
    }

    private void HandleAutoScanChange(bool enabled)
    {
    }

    private void EnableSpeechChip(bool on)
    {
    }

    private void EnableSoundChip(bool on)
    {
    }

    public void LoadRoms(string osRomPath, string? languageRomPath = null)
    {
        OsRom.LoadFromFile(osRomPath);

        if (!string.IsNullOrWhiteSpace(languageRomPath))
        {
            RomBank.LoadRomFromFile(languageRomPath, 0);
            RomBank.SelectSlot(0);
        }
    }

    public void Start()
    {
        SystemVia.Reset();

        CPU.Initialise(MachineConstants.MachineSetup.BbcProcessorSpeed, MachineConstants.MachineSetup.BbcFrameRate);

        FrameCount = 0;

        MachineIsRunning = true;

        var lastFrameTimestamp = Stopwatch.GetTimestamp();
        long viaTicksPending = 0;
        var lastStopwatchTimestamp = Stopwatch.GetTimestamp();

        while (MachineIsRunning)
        {
            var frameStart = Stopwatch.GetTimestamp();
            var targetTicks = frameStart + _frameIntervalTicks;
            var deltaTicks = frameStart - lastFrameTimestamp;
            lastFrameTimestamp = frameStart;

            FrameRate = Stopwatch.Frequency / (double)deltaTicks;

            // Run one frame worth of CPU
            var totalCycle = CPU.RunSingleFrame();
            CpuSpeedMhz = totalCycle * Stopwatch.Frequency / (deltaTicks * 1_000_000.0);
            FrameCount++;

            // Wait until next frame deadline
            var spinner = new SpinWait();
            while (Stopwatch.GetTimestamp() < targetTicks)
            {
                spinner.SpinOnce();
            }

            // Update VIA timer based on real elapsed time
            var now = Stopwatch.GetTimestamp();
            var elapsedTicks = now - lastStopwatchTimestamp;
            lastStopwatchTimestamp = now;

            var elapsedMicroseconds = elapsedTicks * 1_000_000 / Stopwatch.Frequency;
            viaTicksPending += elapsedMicroseconds;

            SystemVia.Tick((uint)viaTicksPending);
        }
    }

    #region Hardware

    public readonly Cpu6502 CPU;
    public readonly IoDevices IoDevices;
    public readonly MemoryMap Memory;
    public readonly OsRom OsRom;
    public readonly RomBank RomBank;
    public readonly SoundSystem Sound;
    public readonly SysVia SystemVia;
    public readonly VideoSystem Video;
    public readonly SpeechChip SpeechChip;
    public readonly SoundChip SoundChip;
    public readonly BitmapVideoRenderer BitmapVideoRenderer;
    public readonly Mc6850Acia Mc6850Acia;
    private readonly SerialULA SerialULA;
    public Via6522 Via6522;

    #endregion
}