namespace MLDComputing.Emulators.BBCSim._6502.Engine;

public class InterruptEventArgs : EventArgs
{
    public InterruptSource Source { get; set; }
}