namespace MLDComputing.Emulators.BBCSim._6502.Exceptions;

public class OpCodeNotFoundException : Exception
{
    public OpCodeNotFoundException()
    {
    }

    public OpCodeNotFoundException(string message)
        : base(message)
    {
    }

    public OpCodeNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }
}