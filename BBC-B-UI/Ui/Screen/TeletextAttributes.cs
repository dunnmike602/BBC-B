namespace MLDComputing.Emulators.BeebBox.Ui.Screen;

using System.Windows.Media;

public class TeletextAttributes
{
    private byte _lastGraphicsChar;
    public Brush Foreground { get; set; } = Brushes.White;
    public bool GraphicsMode { get; set; }
    public bool HoldGraphics { get; set; }
    public bool Conceal { get; set; } = false;
    public bool Flash { get; set; } = false;
    public bool DoubleHeight { get; set; } = false;

    public void ProcessControlCode(byte code)
    {
        switch (code)
        {
            case 0x11: Foreground = Brushes.Red; break;
            case 0x12: Foreground = Brushes.Green; break;
            case 0x13: Foreground = Brushes.Yellow; break;
            case 0x14: Foreground = Brushes.Blue; break;
            case 0x15: Foreground = Brushes.Magenta; break;
            case 0x16: Foreground = Brushes.Cyan; break;
            case 0x17: Foreground = Brushes.White; break;
            case 0x1C: GraphicsMode = true; break;
            case 0x1D: GraphicsMode = false; break;
            case 0x1E: HoldGraphics = true; break;
            case 0x1F: HoldGraphics = false; break;
        }
    }

    public void UpdateLastGraphics(byte ch)
    {
        _lastGraphicsChar = ch;
    }

    public byte GetHeldGraphicsChar()
    {
        return _lastGraphicsChar;
    }
}