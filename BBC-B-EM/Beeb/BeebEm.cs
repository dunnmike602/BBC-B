namespace MLDComputing.Emulators.BBCSim.Beeb;

using System.Diagnostics;
using _6502.Engine;
using _6502.Storage.Interfaces;

public class BeebEm
{
    private readonly IProcessor _cpu; // your 6502 core
    private readonly long _frameIntervalTicks = Stopwatch.Frequency / 50;
    private readonly Keyboard _keyboard;
    private readonly MemoryMap _memory;
    private readonly RomBank _romBank;
    private readonly SoundSystem _sound;
    private readonly Via6522 _systemVia;
    private readonly VideoSystem _video;

    public BeebEm()
    {
        _memory = new MemoryMap();
        _video = new VideoSystem(_memory);
        _sound = new SoundSystem();
        _keyboard = new Keyboard();
        _systemVia = new Via6522(_memory, _keyboard);
        _romBank = new RomBank();
        _cpu = new Cpu6502(_memory.ReadByte, _memory.WriteByte);
    }

    public void LoadRom(string path, int slot = 0)
    {
        // _romBank.LoadRom(path, slot);
    }

    public void Start()
    {
        _cpu.ResetProcessor();

        var skipCount = 0;
        const int maxSkip = 5;

        while (true)
        {
            var frameStart = Stopwatch.GetTimestamp();
            var targetTicks = frameStart + _frameIntervalTicks;

            // 1) Advance emulation state:
            _cpu.RunSingleFrame();
            //  _sound.Tick();
            //    _keyboard.Poll();
            //     _systemVia.Tick();

            // 2) Conditionally render or skip:
            if (skipCount == 0)
            {
                //   _video.RenderFrame();
            }
            else
            {
                skipCount--;
            }

            // 3) Adaptive spin-wait until the end of the 20 ms window:
            var spinner = new SpinWait();
            while (Stopwatch.GetTimestamp() < targetTicks)
            {
                spinner.SpinOnce();
            }

            // 4) Detect overrun and set up frame-skip if needed:
            var elapsed = Stopwatch.GetTimestamp() - frameStart;
            skipCount = elapsed > _frameIntervalTicks ? Math.Min(skipCount + 1, maxSkip) : 0;
        }
    }
}

internal class VideoSystem(MemoryMap memory)
{
}

internal class Via6522(MemoryMap memory, Keyboard keyboard)
{
}

internal class SoundSystem
{
}

internal class Keyboard
{
}