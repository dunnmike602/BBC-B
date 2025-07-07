namespace BeeBoxSDL._6502.Engine.Communication;

public class LightChangedEventArgs : EventArgs
{
    public bool IsOn { get; set; }

    public LEDType Type { get; set; }
}