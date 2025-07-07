namespace MLDComputing.Emulators.BBCSim._6502.Engine.Communication;

public delegate void FrameReady(object sender, FrameReadyEventArgs e);

public class FrameReadyEventArgs
{
    public ulong FrameCount { get; set; }

    public double CpuSpeedMhz { get; set; }

    public double FrameRate { get; set; }
}