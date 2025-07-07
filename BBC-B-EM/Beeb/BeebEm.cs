﻿namespace MLDComputing.Emulators.BBCSim.Beeb;

using System.Diagnostics;
using _6502.Constants;
using _6502.Engine;
using _6502.Engine.Communication;
using Hardware;

public class BeebEm
{
    private readonly long _frameIntervalTicks = Stopwatch.Frequency / 50;

    public double CpuSpeedMhz;

    public ulong FrameCount;

    public double FrameRate;

    public volatile bool MachineIsRunning;

    public BeebEm()
    {
        KeyboardMatrix = new KeyboardMatrix();
        SpeechChip = new SpeechChip();
        SoundChip = new SoundChip();
        RomBank = new RomBank();
        Mc6850Acia = new Mc6850Acia();
        SerialULA = new SerialULA();
        Via6522 = new Via6522();
        SystemVia = new SysVia(Via6522, KeyboardMatrix);
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
            EnableSpeechChip,
            EnableSoundChip
        );
    }

    public KeyboardMatrix KeyboardMatrix { get; set; }

    public event FrameReady? FrameReady;

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
        var lastFrameTimestamp = InitMachine(out var viaTicksPending, out var lastStopwatchTimestamp);

        while (MachineIsRunning)
        {
            var frameStart = Stopwatch.GetTimestamp();
            var targetTicks = frameStart + _frameIntervalTicks;
            var deltaTicks = frameStart - lastFrameTimestamp;
            lastFrameTimestamp = frameStart;

            FrameRate = Stopwatch.Frequency / (double)deltaTicks;

            // Run one frame worth of CPU
            var totalCycle = CPU.RunSingleFrame();

            CpuSpeedMhz = totalCycle * (ulong)Stopwatch.Frequency / (deltaTicks * 1_000_000.0);
            FrameCount++;

            // Wait until next frame deadline
            var spinner = new SpinWait();
            while (Stopwatch.GetTimestamp() < targetTicks)
            {
                spinner.SpinOnce();
            }

            FrameReady?.Invoke(this,
                new FrameReadyEventArgs { FrameCount = FrameCount, CpuSpeedMhz = CpuSpeedMhz, FrameRate = FrameRate });
        }
    }

    private long InitMachine(out long viaTicksPending, out long lastStopwatchTimestamp)
    {
        SystemVia.Reset();

        CPU.Initialise(MachineConstants.MachineSetup.BbcProcessorSpeed, MachineConstants.MachineSetup.BbcFrameRate);

        FrameCount = 0;

        MachineIsRunning = true;

        var lastFrameTimestamp = Stopwatch.GetTimestamp();
        viaTicksPending = 0;
        lastStopwatchTimestamp = Stopwatch.GetTimestamp();
        return lastFrameTimestamp;
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
    public readonly Mc6850Acia Mc6850Acia;
    private readonly SerialULA SerialULA;
    public Via6522 Via6522;

    #endregion
}