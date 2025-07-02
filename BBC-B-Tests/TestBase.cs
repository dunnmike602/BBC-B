namespace BBC_B_Tests;

using MLDComputing.Emulators.BBCSim._6502.Assembler;
using MLDComputing.Emulators.BBCSim._6502.Engine;
using TestDoubles;

public abstract class TestBase
{
    protected const ushort StartAddress = 3000;

    protected Assembler? Assembler;
    protected Mapper? Mapper;
    protected TestMemoryMap? MemoryMap;
    protected Cpu6502? Processor;
    protected Tokeniser? Tokeniser;

    [TestInitialize]
    public void Init()
    {
        Tokeniser = new Tokeniser();
        MemoryMap = new TestMemoryMap();
        Mapper = new Mapper();
        Processor = new Cpu6502(MemoryMap.ReadByte, MemoryMap.WriteByte);
        Processor!.InitialiseSlim(10_000_000, 50, StartAddress, true);
        Assembler = new Assembler(MemoryMap.WriteByte);
    }

    protected void AssembleAndRun(string program)
    {
        var ops = Tokeniser!.Parse(program);
        Mapper!.MapAndValidate(ops);
        Assembler!.Assemble(ops, StartAddress);

        while (Processor!.IsRunning)
        {
            Processor.RunSingleFrame();
        }
    }
}