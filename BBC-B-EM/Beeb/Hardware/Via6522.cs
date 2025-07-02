namespace MLDComputing.Emulators.BBCSim.Beeb.Hardware;

public class Via6522
{
    public byte ACR;

    public bool CA2;

    public bool CB2;

    public byte DDRA;

    public byte DDRB;

    public byte IC32State;

    public byte IER;

    public byte IFR;

    public int IRA;

    public int IRB;

    public byte ORA;

    public byte ORB;

    public byte PCR;

    public int Timer1Counter;

    public bool Timer1HasFinished;

    public int Timer1Latch;

    public int Timer2Counter;

    public bool Timer2HasFinished;

    public int Timer2Latch;

    public void Reset()
    {
        PCR = 0;
        IFR = 0;
        IER = 0;
        DDRB = 0xFF;
        DDRA = 0xFF;
        ACR = 0;
        IC32State = 0;
        ORA = 0;
        ORB = 0;
        IRA = 0xFF;
        IRB = 0xFF;

        Timer1Counter = 0;
        Timer2Counter = 0;
        Timer1Latch = 1;
        Timer2Latch = 2;
    }
}