namespace MLDComputing.Emulators.BBCSim.Beeb.Hardware;

public class KeyState
{
    public bool IsPressed; // True = currently down
    public ulong LastChangeCycle; // When press/release occurred
    public ulong LastScanCycle; // Last time OS scanned this key
    public bool Latched; // True = latched
}