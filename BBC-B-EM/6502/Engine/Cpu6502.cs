namespace MLDComputing.Emulators.BBCSim._6502.Engine;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assembler;
using Exceptions;
using Extensions;
using Storage;
using Storage.Interfaces;
using Byte = Storage.Byte;

public class Cpu6502(Func<ushort, byte> readByte, Action<ushort, byte> writeByte) : IProcessor
{
    private int _cyclesPerFrame;

    private long _frameCount;

    private Instruction[]? _instructions;

    private volatile bool _irq;

    private volatile bool _isRunning;

    private ushort _programCounter;

    private volatile bool _singleStepModeOn;

    private byte _stackPointer;

    public int CyclesPerSecond { get; set; }

    public long TotalElapsedInterruptTicks { get; set; }

    public long VideoFrameIntervalTicks { get; set; }

    public event InterruptHandler? Interrupt;

    public bool SingleStepModeOn
    {
        get => _singleStepModeOn;
        set => _singleStepModeOn = value;
    }

    public long FrameRate { get; set; }
    public long TotalCyclesExecuted { get; set; }
    public int ExpectedProcessorSpeed { get; set; }
    public long TotalElapsedTicks { get; set; }


    public void Initialise(int cyclesPerSecond, int videoFrameRate)
    {
        CyclesPerSecond = cyclesPerSecond;

        VideoFrameIntervalTicks = (int)(1 / (float)videoFrameRate * Stopwatch.Frequency);

        _cyclesPerFrame = GetNumberOfInstructionsPerVideoRefresh();

        ExpectedProcessorSpeed = cyclesPerSecond;
        ResetProcessor();

        _instructions = Data.GetInstructions();
    }

    public byte Accumulator { get; set; }

    public byte IX { get; set; }

    public byte IY { get; set; }

    public byte Status { get; set; }

    public Bit GetStatusFlag(Statuses statusFlag)
    {
        return Status.GetBit((Byte)statusFlag);
    }

    public void SetStatusFlag(Statuses statusFlag, Bit value)
    {
        Status = Status.SetBit((Byte)statusFlag, value);
    }

    public ushort GetProgramCounter()
    {
        return _programCounter;
    }

    public byte GetStackPointer()
    {
        return _stackPointer;
    }

    public byte BrkIncrement { get; set; } = 2;

    public bool Irq
    {
        get => _irq;
        set => _irq = value;
    }

    public long GetProcessorMicroSeconds()
    {
        var elapsedSeconds = TotalElapsedTicks / (float)Stopwatch.Frequency;

        return (long)(elapsedSeconds * 1e6);
    }

    public long GetInterruptMicroSeconds()
    {
        var elapsedSeconds = TotalElapsedInterruptTicks / (float)Stopwatch.Frequency;

        return (long)(elapsedSeconds * 1e6);
    }

    public void SetProgramCounter(ushort newValue)
    {
        _programCounter = newValue;
    }

    public long GetActualProcessorSpeed()
    {
        var elapsedSeconds = TotalElapsedTicks / (float)Stopwatch.Frequency;

        const float epsilon = (float)0.00001;

        if (Math.Abs(elapsedSeconds - 0) < epsilon)
        {
            return 0;
        }

        var timeForOnceCycle = elapsedSeconds / TotalCyclesExecuted;

        return ((long)(1 / timeForOnceCycle)).RoundToSignificantDigits(4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetProcessor()
    {
        TotalCyclesExecuted = 0;
        Accumulator = 0;
        IX = 0;
        IY = 0;
        Status = 0x22;
        _stackPointer = 0xFF;
        Irq = false;
        _frameCount = 0;

        TotalElapsedTicks = 0;
        TotalElapsedInterruptTicks = 0;

        _programCounter = (ushort)(readByte(ResetVectorLow) | (readByte(ResetVectorHigh) << 8));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Run(ushort startAddress, bool singleStepModeOn)
    {
        _isRunning = true;
        SingleStepModeOn = singleStepModeOn;
        _programCounter = startAddress;

        var ticksPerSecond = Stopwatch.Frequency;
        var ticksPerFrame = ticksPerSecond / 50; // 20 ms per frame
        var frameStartTicks = Stopwatch.GetTimestamp();
        long frameCounter = 0;

        try
        {
            int lastUsedCycles;

            do
            {
                var cycles = 0;

                do
                {
                    var before = Stopwatch.GetTimestamp();

                    lastUsedCycles = ExecuteNext();
                    cycles += lastUsedCycles;

                    // Track total CPU cycles
                    TotalCyclesExecuted = long.MaxValue - lastUsedCycles <= TotalCyclesExecuted
                        ? lastUsedCycles
                        : TotalCyclesExecuted + lastUsedCycles;
                } while (cycles <= _cyclesPerFrame && lastUsedCycles > 0 && !SingleStepModeOn);

                _frameCount++;

                // Calculate elapsed and wait if ahead of schedule
                var now = Stopwatch.GetTimestamp();
                var expectedTicks = ++frameCounter * ticksPerFrame;
                var actualElapsed = now - frameStartTicks;
                var quantumToWait = expectedTicks - actualElapsed;

                SpinWaitTicks(quantumToWait); // New version below

                TotalElapsedTicks = Stopwatch.GetTimestamp() - frameStartTicks;

                // Handle interrupts
                if (_irq && GetStatusFlag(Statuses.InterruptDisable) == Bit.Zero)
                {
                    var isrStart = Stopwatch.GetTimestamp();
                    TotalCyclesExecuted += ServiceInterrupt();
                    TotalElapsedInterruptTicks += Stopwatch.GetTimestamp() - isrStart;
                    _irq = false;
                }
            } while (lastUsedCycles != 0 && !SingleStepModeOn);
        }
        finally
        {
            _isRunning = false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int RunSingleFrame()
    {
        var totalCycles = 0;

        while (totalCycles < _cyclesPerFrame && _isRunning)
        {
            var used = ExecuteNext();

            // 0 means either unknown opcode or HALT
            if (used <= 0)
            {
                break;
            }

            totalCycles += used;

            TotalCyclesExecuted = long.MaxValue - used <= TotalCyclesExecuted
                ? used
                : TotalCyclesExecuted + used;

            // Optional: allow breakpoints or stepping out
            if (SingleStepModeOn)
            {
                break;
            }
        }

        return totalCycles;
    }

    // Invoke the Changed event; called whenever list changes
    protected virtual void OnInterrupt(InterruptEventArgs e)
    {
        Interrupt!(this, e);
    }

    private int GetNumberOfInstructionsPerVideoRefresh()
    {
        var cyclesPerTick = CyclesPerSecond / (float)Stopwatch.Frequency;

        return (int)(cyclesPerTick * VideoFrameIntervalTicks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SpinWaitTicks(long remainingTicks)
    {
        if (remainingTicks <= 0)
        {
            return;
        }

        var start = Stopwatch.GetTimestamp();
        var end = start + remainingTicks;

        while (Stopwatch.GetTimestamp() < end)
        {
            var ticksLeft = end - Stopwatch.GetTimestamp();

            if (ticksLeft > Stopwatch.Frequency / 1000) // More than 1ms
            {
                Thread.Sleep(1);
            }
            else if (ticksLeft > Stopwatch.Frequency / 10000) // More than 100µs
            {
                Thread.SpinWait(50);
            }
        }
    }

    private int ServiceInterrupt()
    {
        var programCounter = _programCounter++;

        var statusToPush = Status;

        // The interrupt disable flag is set in the status register
        Status = Status.SetBit((Byte)Statuses.InterruptDisable, Bit.One);

        Push(programCounter.LowByte());
        Push(programCounter.HighByte());
        Push(statusToPush);

        var lo = readByte(IrqHandlerLowByte);
        var hi = readByte(IrqHandlerHighByte);
        _programCounter = (ushort)(lo | (hi << 8));

        // Hardware interrupt always takes 7 cycles
        return 7;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ExecuteNext()
    {
        var opCode = readByte(_programCounter);

        var instruction = _instructions![opCode];

        // Instruction not found
        if (instruction.IsNotFound())
        {
            return 0;
        }

        int? actualCyclesUsed = null;

        switch (instruction.Mnemonic)
        {
            case Data.ADC:
                actualCyclesUsed = ProcessADC(instruction);
                break;

            case Data.AND:
                actualCyclesUsed = ProcessAND(instruction);
                break;

            case Data.ASL:
                actualCyclesUsed = ProcessASL(instruction);
                break;

            case Data.BCC:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.CarryFlag, Bit.Zero);
                break;

            case Data.BCS:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.CarryFlag, Bit.One);
                break;

            case Data.BEQ:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.ZeroFlag, Bit.One);
                break;

            case Data.BIT:
                actualCyclesUsed = ProcessBIT(instruction);
                break;

            case Data.BMI:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.NegativeFlag, Bit.One);
                break;

            case Data.BNE:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.ZeroFlag, Bit.Zero);
                break;

            case Data.BPL:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.NegativeFlag, Bit.Zero);
                break;

            case Data.CLC:
                actualCyclesUsed = SetFlag(Statuses.CarryFlag, Bit.Zero);
                break;

            case Data.CLD:
                actualCyclesUsed = SetFlag(Statuses.DecimalMode, Bit.Zero);
                break;

            case Data.CLI:
                actualCyclesUsed = SetFlag(Statuses.InterruptDisable, Bit.Zero);
                break;

            case Data.CLV:
                actualCyclesUsed = SetFlag(Statuses.OverflowFlag, Bit.Zero);
                break;

            case Data.BRK:
                actualCyclesUsed = ProcessBRK();
                break;

            case Data.BVC:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.OverflowFlag, Bit.Zero);
                break;

            case Data.BVS:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.OverflowFlag, Bit.One);
                break;

            case Data.CMP:
                actualCyclesUsed = ProcessCMP(instruction);
                break;

            case Data.DBG:
                // Pseudo-op that allows code to be broke into
                SingleStepModeOn = true;
                _programCounter++;
                break;

            case Data.CPY:
                actualCyclesUsed = ProcessCPY(instruction);
                break;

            case Data.CPX:
                actualCyclesUsed = ProcessCPX(instruction);
                break;

            case Data.DEC:
                actualCyclesUsed = ProcessDEC(instruction);
                break;

            case Data.DEY:
                actualCyclesUsed = ProcessDEY();
                break;

            case Data.DEX:
                actualCyclesUsed = ProcessDEX();
                break;

            case Data.EOR:
                actualCyclesUsed = ProcessEOR(instruction);
                break;

            case Data.INC:
                actualCyclesUsed = ProcessINC(instruction);
                break;

            case Data.INY:
                actualCyclesUsed = ProcessINY();
                break;

            case Data.INX:
                actualCyclesUsed = ProcessINX();
                break;

            case Data.JMP:
                actualCyclesUsed = ProcessJMP(instruction);
                break;

            case Data.JSR:
                actualCyclesUsed = ProcessJSR();
                break;

            case Data.LDA:
                actualCyclesUsed = ProcessLDA(instruction);
                break;

            case Data.LDX:
                actualCyclesUsed = ProcessLDX(instruction);
                break;

            case Data.LDY:
                actualCyclesUsed = ProcessLDY(instruction);
                break;

            case Data.LSR:
                actualCyclesUsed = ProcessLSR(instruction);
                break;

            case Data.NOP:
                actualCyclesUsed = ProcessNOP();
                break;

            case Data.ORA:
                actualCyclesUsed = ProcessORA(instruction);
                break;

            case Data.PHA:
                actualCyclesUsed = ProcessPHA();
                break;

            case Data.PHP:
                actualCyclesUsed = ProcessPHP();
                break;

            case Data.PLA:
                actualCyclesUsed = ProcessPLA();
                break;

            case Data.PLP:
                actualCyclesUsed = ProcessPLP();
                break;

            case Data.ROL:
                actualCyclesUsed = ProcessROL(instruction);
                break;

            case Data.ROR:
                actualCyclesUsed = ProcessROR(instruction);
                break;

            case Data.RTI:
                actualCyclesUsed = ProcessRTI();
                break;

            case Data.STA:
                actualCyclesUsed = ProcessSTA(instruction);
                break;

            case Data.RTS:
                actualCyclesUsed = ProcessRTS();
                break;

            case Data.SBC:
                actualCyclesUsed = ProcessSBC(instruction);
                break;

            case Data.SEC:
                actualCyclesUsed = SetFlag(Statuses.CarryFlag, Bit.One);
                break;

            case Data.SED:
                actualCyclesUsed = SetFlag(Statuses.DecimalMode, Bit.One);
                break;

            case Data.SEI:
                actualCyclesUsed = SetFlag(Statuses.InterruptDisable, Bit.One);
                break;

            case Data.STX:
                actualCyclesUsed = ProcessSTX(instruction);
                break;

            case Data.STY:
                actualCyclesUsed = ProcessSTY(instruction);
                break;

            case Data.TAX:
                actualCyclesUsed = ProcessTAX();
                break;

            case Data.TAY:
                actualCyclesUsed = ProcessTAY();
                break;

            case Data.TSX:
                actualCyclesUsed = ProcessTSX();
                break;

            case Data.TXA:
                actualCyclesUsed = ProcessTXA();
                break;

            case Data.TXS:
                actualCyclesUsed = ProcessTXS();
                break;

            case Data.TYA:
                actualCyclesUsed = ProcessTYA();
                break;

            case Data.KIL:
                // Dummy Instruction to halt the processor.
                break;
        }

        return actualCyclesUsed ?? instruction.Cycles;
    }

    #region Execution code for each 6502 instruction keeping this inline for the moment for performance

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessSBC(Instruction instruction)
    {
        _programCounter++;

        byte operand = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.SBCImmediate:
                operand = readByte(_programCounter++);
                break;

            case (byte)OpCodeValues.SBCZeroPage:
                var zeroPageAddress = readByte(_programCounter++);
                operand = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.SBCZeroPageX:
                var zeroPageAddressX = (ushort)(readByte(_programCounter++) + IX % MsbMultiplier);
                operand = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.SBCAbsolute:
                var absoluteAddress = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                operand = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.SBCAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IX);
                operand = readByte(absoluteAddressX);
                break;

            case (byte)OpCodeValues.SBCAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IY);
                operand = readByte(absoluteAddressY);
                break;

            case (byte)OpCodeValues.SBCIndexedIndirect:
                var zeroPageIndexX = (ushort)((readByte(_programCounter) + IX) % MsbMultiplier);
                var zeroPageLsbX = readByte(zeroPageIndexX);
                var zeroPageMsbX = readByte((ushort)(zeroPageIndexX + 1 % MsbMultiplier));
                var zeroPageAddressIndexedX = (ushort)(zeroPageLsbX + zeroPageMsbX * MsbMultiplier);

                operand = readByte(zeroPageAddressIndexedX);
                _programCounter++;
                break;

            case (byte)OpCodeValues.SBCIndirectIndexed:
                var zeroPageIndexY = readByte(_programCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY = readByte((ushort)((zeroPageIndexY + 1) % MsbMultiplier));

                var zeroPageAddressIndexY = (ushort)(zeroPageLsbY + zeroPageMsbY * MsbMultiplier + IY);
                operand = readByte(zeroPageAddressIndexY);
                _programCounter++;
                break;
        }

        var decimalMode = GetStatusFlag(Statuses.DecimalMode) == Bit.One;
        var carryIn = GetStatusFlag(Statuses.CarryFlag) == Bit.One;
        var carry = carryIn ? 0 : 1; // In SBC, Carry Clear means borrow

        var value = operand ^ 0xFF; // Invert bits for two's complement subtraction
        var sum = Accumulator + value + (1 - carry); // A - M - (1 - C) == A + ~M + C

        int result;
        bool carryOut;
        bool overflow;

        if (!decimalMode)
        {
            result = sum & 0xFF;

            // Carry set if NO borrow (i.e., result >= 0)
            carryOut = sum >= 0;

            // Overflow occurs if the sign of Accumulator and operand differ, and the result sign differs from Accumulator
            overflow = ((Accumulator ^ operand) & (Accumulator ^ result) & 0x80) != 0;
        }
        else
        {
            // Decimal mode BCD subtraction
            var al = (Accumulator & 0x0F) - (operand & 0x0F) - carry;
            var ah = (Accumulator >> 4) - (operand >> 4);

            if (al < 0)
            {
                al -= 6;
                ah -= 1;
            }

            if (ah < 0)
            {
                ah -= 6;
            }

            result = ((ah << 4) | (al & 0x0F)) & 0xFF;

            // Binary sum used to determine carry and overflow in decimal mode
            carryOut = sum >= 0;
            overflow = ((Accumulator ^ operand) & (Accumulator ^ sum) & 0x80) != 0;
        }

        Accumulator = (byte)result;

        // Carry: set if no borrow occurred
        Status = Status.SetBit((Byte)Statuses.CarryFlag, carryOut ? Bit.One : Bit.Zero);

        // Negative: set if result has bit 7 set
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, (result & 0x80) != 0 ? Bit.One : Bit.Zero);

        // Zero: set if result is zero
        Status = Status.SetBit((Byte)Statuses.ZeroFlag, result == 0 ? Bit.One : Bit.Zero);

        // Overflow: from signed interpretation
        Status = Status.SetBit((Byte)Statuses.OverflowFlag, overflow ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessADC(Instruction instruction)
    {
        _programCounter++;

        byte operand = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.ADCImmediate:
                operand = readByte(_programCounter++);
                break;

            case (byte)OpCodeValues.ADCZeroPage:
                var zeroPageAddress = readByte(_programCounter++);
                operand = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.ADCZeroPageX:
                var zeroPageAddressX = (ushort)(readByte(_programCounter++) + IX % MsbMultiplier);
                operand = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.ADCAbsolute:
                var absoluteAddress = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                operand = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.ADCAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IX);
                operand = readByte(absoluteAddressX);
                break;

            case (byte)OpCodeValues.ADCAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IY);
                operand = readByte(absoluteAddressY);
                break;

            case (byte)OpCodeValues.ADCIndexedIndirect:
                var zeroPageIndexX = (ushort)((readByte(_programCounter) + IX) % MsbMultiplier);
                var zeroPageLsbX = readByte(zeroPageIndexX);
                var zeroPageMsbX = readByte((ushort)((zeroPageIndexX + 1) % MsbMultiplier));
                var zeroPageAddressIndexedX = (ushort)(zeroPageLsbX + zeroPageMsbX * MsbMultiplier);

                operand = readByte(zeroPageAddressIndexedX);
                _programCounter++;
                break;

            case (byte)OpCodeValues.ADCIndirectIndexed:
                var zeroPageIndexY = readByte(_programCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY = readByte((ushort)((zeroPageIndexY + 1) % MsbMultiplier));

                var zeroPageAddressIndexY = (ushort)(zeroPageLsbY + zeroPageMsbY * MsbMultiplier + IY);
                operand = readByte(zeroPageAddressIndexY);
                _programCounter++;
                break;
        }

        var decimalMode = GetStatusFlag(Statuses.DecimalMode) == Bit.One;
        var carryIn = GetStatusFlag(Statuses.CarryFlag) == Bit.One;
        var carry = carryIn ? 1 : 0;

        int result;
        bool carryOut;
        bool overflow;

        if (!decimalMode)
        {
            // Binary mode addition
            var sum = Accumulator + operand + carry;
            result = sum & 0xFF;

            // Carry: if sum exceeds 255
            carryOut = sum > 0xFF;

            // Overflow: if sign bit is incorrect for signed addition
            overflow = (~(Accumulator ^ operand) & (Accumulator ^ result) & 0x80) != 0;
        }
        else
        {
            // Decimal mode BCD addition
            var al = (Accumulator & 0x0F) + (operand & 0x0F) + carry;
            var ah = (Accumulator >> 4) + (operand >> 4);

            if (al > 9)
            {
                al += 6;
                ah += 1;
            }

            carryOut = false;
            if (ah > 9)
            {
                ah += 6;
                carryOut = true;
            }

            result = ((ah << 4) | (al & 0x0F)) & 0xFF;

            // Overflow is still based on binary result
            var binarySum = Accumulator + operand + carry;
            overflow = (~(Accumulator ^ operand) & (Accumulator ^ binarySum) & 0x80) != 0;
        }

        Accumulator = (byte)result;

        // Set or clear Carry flag
        Status = Status.SetBit((Byte)Statuses.CarryFlag, carryOut ? Bit.One : Bit.Zero);

        // Set or clear Negative flag (bit 7 of result)
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, (result & 0x80) != 0 ? Bit.One : Bit.Zero);

        // Set or clear Zero flag
        Status = Status.SetBit((Byte)Statuses.ZeroFlag, result == 0 ? Bit.One : Bit.Zero);

        // Set or clear Overflow flag
        Status = Status.SetBit((Byte)Statuses.OverflowFlag, overflow ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessJMP(Instruction instruction)
    {
        _programCounter++;

        ushort memoryValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.JMPAbsolute:
                memoryValue = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) * MsbMultiplier);
                break;

            case (byte)OpCodeValues.JMPIndirect:
                memoryValue = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) * MsbMultiplier);
                memoryValue = (ushort)(readByte(memoryValue) + readByte((ushort)(memoryValue + 1)) * MsbMultiplier);
                break;
        }

        _programCounter = memoryValue;

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? SetFlag(Statuses flagToSet, Bit state)
    {
        _programCounter++;

        Status = Status.SetBit((Byte)flagToSet, state);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessRelativeBranch(Statuses flagToCheck, Bit state)
    {
        _programCounter++;

        if (Status.GetBit((Byte)flagToCheck) == state)
        {
            var relativeOffSet = (sbyte)readByte(_programCounter);
            _programCounter = (ushort)(_programCounter + relativeOffSet +
                                       (relativeOffSet > 0 ? -1 : 1));
        }
        else
        {
            _programCounter++;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessRTI()
    {
        // Get the status register from the stack
        var status = Pop();

        // Break flag is never set in the processor status register
        Status = status.SetBit((Byte)Statuses.BreakCommand, Bit.Zero);

        _programCounter = (ushort)(Pop() * MsbMultiplier + Pop());

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessBRK()
    {
        if (Status.GetBit((Byte)Statuses.InterruptDisable) == Bit.One)
        {
            // If BRK is executed when the flag is already set this will return 0
            // cycles and have the effect of terminating the program
            return 0;
        }

        var programCounter = _programCounter += BrkIncrement;

        // Defined 6502 behaviour is to set the B flag in the value pushed only
        var statusToPush = Status.SetBit((Byte)Statuses.BreakCommand, Bit.One);

        // The interrupt disable flag is set in the status register
        Status = Status.SetBit((Byte)Statuses.InterruptDisable, Bit.One);

        Push(programCounter.LowByte());
        Push(programCounter.HighByte());
        Push(statusToPush);

        _programCounter = (ushort)(readByte(IrqHandlerLowByte) + readByte(IrqHandlerHighByte) *
            MsbMultiplier);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Push(byte value)
    {
        writeByte((ushort)(_stackPointer + StackPage * MsbMultiplier), value);
        _stackPointer--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte Pop()
    {
        _stackPointer++;
        var value = readByte((ushort)(_stackPointer + StackPage * MsbMultiplier));

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte PeekStack(byte stackPointer)
    {
        var value = readByte((ushort)(_stackPointer + StackPage * MsbMultiplier));

        return value;
    }

    public bool IsCarrySet()
    {
        return Status.GetBit((Byte)Statuses.CarryFlag) == Bit.One;
    }

    public bool IsRunning()
    {
        return _isRunning;
    }

    public void SetResetHandler(ushort startAddress)
    {
        writeByte(ResetVectorLow, startAddress.LowByte());
        writeByte(ResetVectorHigh, startAddress.HighByte());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessCMP(Instruction instruction)
    {
        _programCounter++;

        byte memoryValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.CMPImmediate:
                memoryValue = readByte(_programCounter++);
                break;

            case (byte)OpCodeValues.CMPZeroPage:
                var zeroPageAddress = readByte(_programCounter++);
                memoryValue = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.CMPZeroPageX:
                var zeroPageAddressX = (ushort)(readByte(_programCounter++) + IX % MsbMultiplier);
                memoryValue = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.CMPAbsolute:
                var absoluteAddress = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                memoryValue = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.CMPAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IX);
                memoryValue = readByte(absoluteAddressX);
                break;

            case (byte)OpCodeValues.CMPAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IY);
                memoryValue = readByte(absoluteAddressY);
                break;

            case (byte)OpCodeValues.CMPIndexedIndirect:
                var zeroPageIndexX = (ushort)((readByte(_programCounter) + IX) % MsbMultiplier);
                var zeroPageLsbX = readByte(zeroPageIndexX);
                var zeroPageMsbX = readByte((ushort)((zeroPageIndexX + 1) % MsbMultiplier));
                var zeroPageAddressIndexedX = (ushort)(zeroPageLsbX + zeroPageMsbX * MsbMultiplier);

                memoryValue = readByte(zeroPageAddressIndexedX);
                _programCounter++;
                break;

            case (byte)OpCodeValues.CMPIndirectIndexed:
                var zeroPageIndexY = readByte(_programCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY = readByte((ushort)((zeroPageIndexY + 1) % MsbMultiplier));

                var zeroPageAddressIndexY = (ushort)(zeroPageLsbY + zeroPageMsbY * MsbMultiplier + IY);
                memoryValue = readByte(zeroPageAddressIndexY);
                _programCounter++;
                break;
        }

        var negativeFlag = Bit.Zero;
        var zeroFlag = Bit.Zero;
        var carryFlag = Bit.Zero;

        var result = (byte)(Accumulator - memoryValue);

        if (Accumulator < memoryValue)
        {
            negativeFlag = result.GetBit(Byte.Seven);
            zeroFlag = Bit.Zero;
            carryFlag = Bit.Zero;
        }

        if (Accumulator == memoryValue)
        {
            negativeFlag = Bit.Zero;
            zeroFlag = Bit.One;
            carryFlag = Bit.One;
        }

        if (Accumulator > memoryValue)
        {
            negativeFlag = result.GetBit(Byte.Seven);
            zeroFlag = Bit.Zero;
            carryFlag = Bit.One;
        }

        Status = Status.SetBit((Byte)Statuses.NegativeFlag, negativeFlag);
        Status = Status.SetBit((Byte)Statuses.ZeroFlag, zeroFlag);
        Status = Status.SetBit((Byte)Statuses.CarryFlag, carryFlag);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessCPX(Instruction instruction)
    {
        _programCounter++;

        byte memoryValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.CPXImmediate:
                memoryValue = readByte(_programCounter++);
                break;

            case (byte)OpCodeValues.CPXZeroPage:
                var zeroPageAddress = readByte(_programCounter++);
                memoryValue = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.CPXAbsolute:
                var absoluteAddress = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                memoryValue = readByte(absoluteAddress);
                break;
        }

        var negativeFlag = Bit.Zero;
        var zeroFlag = Bit.Zero;
        var carryFlag = Bit.Zero;

        var result = (byte)(IX - memoryValue);

        if (IX < memoryValue)
        {
            negativeFlag = result.GetBit(Byte.Seven);
            zeroFlag = Bit.Zero;
            carryFlag = Bit.Zero;
        }

        if (IX == memoryValue)
        {
            negativeFlag = Bit.Zero;
            zeroFlag = Bit.One;
            carryFlag = Bit.One;
        }

        if (IX > memoryValue)
        {
            negativeFlag = result.GetBit(Byte.Seven);
            zeroFlag = Bit.Zero;
            carryFlag = Bit.One;
        }

        Status = Status.SetBit((Byte)Statuses.NegativeFlag, negativeFlag);
        Status = Status.SetBit((Byte)Statuses.ZeroFlag, zeroFlag);
        Status = Status.SetBit((Byte)Statuses.CarryFlag, carryFlag);

        if (zeroFlag == Bit.Zero)
        {
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessCPY(Instruction instruction)
    {
        _programCounter++;

        byte memoryValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.CPYImmediate:
                memoryValue = readByte(_programCounter++);
                break;

            case (byte)OpCodeValues.CPYZeroPage:
                var zeroPageAddress = readByte(_programCounter++);
                memoryValue = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.CPYAbsolute:
                var absoluteAddress = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                memoryValue = readByte(absoluteAddress);
                break;
        }

        var negativeFlag = Bit.Zero;
        var zeroFlag = Bit.Zero;
        var carryFlag = Bit.Zero;

        var result = (byte)(IY - memoryValue);

        if (IY < memoryValue)
        {
            negativeFlag = result.GetBit(Byte.Seven);
            zeroFlag = Bit.Zero;
            carryFlag = Bit.Zero;
        }

        if (IY == memoryValue)
        {
            negativeFlag = Bit.Zero;
            zeroFlag = Bit.One;
            carryFlag = Bit.One;
        }

        if (IY > memoryValue)
        {
            negativeFlag = result.GetBit(Byte.Seven);
            zeroFlag = Bit.Zero;
            carryFlag = Bit.One;
        }

        Status = Status.SetBit((Byte)Statuses.NegativeFlag, negativeFlag);
        Status = Status.SetBit((Byte)Statuses.ZeroFlag, zeroFlag);
        Status = Status.SetBit((Byte)Statuses.CarryFlag, carryFlag);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessORA(Instruction instruction)
    {
        _programCounter++;

        byte memoryValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.ORAImmediate:
                memoryValue = readByte(_programCounter++);
                break;

            case (byte)OpCodeValues.ORAZeroPage:
                var zeroPageAddress = readByte(_programCounter++);
                memoryValue = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.ORAZeroPageX:
                var zeroPageAddressX = (ushort)(readByte(_programCounter++) + IX % MsbMultiplier);
                memoryValue = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.ORAAbsolute:
                var absoluteAddress = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                memoryValue = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.ORAAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IX);
                memoryValue = readByte(absoluteAddressX);
                break;

            case (byte)OpCodeValues.ORAAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IY);
                memoryValue = readByte(absoluteAddressY);
                break;

            case (byte)OpCodeValues.ORAIndexedIndirect:
                var zeroPageIndexX = (ushort)((readByte(_programCounter) + IX) % MsbMultiplier);
                var zeroPageLsbX = readByte(zeroPageIndexX);
                var zeroPageMsbX = readByte((ushort)((zeroPageIndexX + 1) % MsbMultiplier));
                var zeroPageAddressIndexedX = (ushort)(zeroPageLsbX + zeroPageMsbX * MsbMultiplier);

                memoryValue = readByte(zeroPageAddressIndexedX);
                _programCounter++;
                break;

            case (byte)OpCodeValues.ORAIndirectIndexed:
                var zeroPageIndexY = readByte(_programCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY = readByte((ushort)((zeroPageIndexY + 1) % MsbMultiplier));

                var zeroPageAddressIndexY = (ushort)(zeroPageLsbY + zeroPageMsbY * MsbMultiplier + IY);
                memoryValue = readByte(zeroPageAddressIndexY);
                _programCounter++;
                break;
        }

        Accumulator = (byte)(memoryValue | Accumulator);

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, Accumulator.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, Accumulator.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessEOR(Instruction instruction)
    {
        _programCounter++;

        byte memoryValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.EORImmediate:
                memoryValue = readByte(_programCounter++);
                break;

            case (byte)OpCodeValues.EORZeroPage:
                var zeroPageAddress = readByte(_programCounter++);
                memoryValue = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.EORZeroPageX:
                var zeroPageAddressX = (ushort)(readByte(_programCounter++) + IX % MsbMultiplier);
                memoryValue = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.EORAbsolute:
                var absoluteAddress = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                memoryValue = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.EORAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IX);
                memoryValue = readByte(absoluteAddressX);
                break;

            case (byte)OpCodeValues.EORAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IY);
                memoryValue = readByte(absoluteAddressY);
                break;

            case (byte)OpCodeValues.EORIndexedIndirect:
                var zeroPageIndexX = (ushort)((readByte(_programCounter) + IX) % MsbMultiplier);
                var zeroPageLsbX = readByte(zeroPageIndexX);
                var zeroPageMsbX = readByte((ushort)(zeroPageIndexX + 1)) % MsbMultiplier;
                var zeroPageAddressIndexedX = (ushort)(zeroPageLsbX + zeroPageMsbX * MsbMultiplier);

                memoryValue = readByte(zeroPageAddressIndexedX);
                _programCounter++;
                break;

            case (byte)OpCodeValues.EORIndirectIndexed:
                var zeroPageIndexY = readByte(_programCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY = readByte((ushort)(zeroPageIndexY + 1)) % MsbMultiplier;

                var zeroPageAddressIndexY = (ushort)(zeroPageLsbY + zeroPageMsbY * MsbMultiplier + IY);
                memoryValue = readByte(zeroPageAddressIndexY);
                _programCounter++;
                break;
        }

        Accumulator = (byte)(memoryValue ^ Accumulator);

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, Accumulator.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, Accumulator.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessAND(Instruction instruction)
    {
        _programCounter++;

        byte memoryValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.ANDImmediate:
                memoryValue = readByte(_programCounter++);
                break;

            case (byte)OpCodeValues.ANDZeroPage:
                var zeroPageAddress = readByte(_programCounter++);
                memoryValue = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.ANDZeroPageX:
                var zeroPageAddressX = (ushort)(readByte(_programCounter++) + IX % MsbMultiplier);
                memoryValue = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.ANDAbsolute:
                var absoluteAddress = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                memoryValue = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.ANDAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IX);
                memoryValue = readByte(absoluteAddressX);
                break;

            case (byte)OpCodeValues.ANDAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IY);
                memoryValue = readByte(absoluteAddressY);
                break;

            case (byte)OpCodeValues.ANDIndexedIndirect:
                var zeroPageIndexX = (ushort)((readByte(_programCounter) + IX) % MsbMultiplier);
                var zeroPageLsbX = readByte(zeroPageIndexX);
                var zeroPageMsbX = readByte((ushort)((zeroPageIndexX + 1) % MsbMultiplier));
                var zeroPageAddressIndexedX = (ushort)(zeroPageLsbX + zeroPageMsbX * MsbMultiplier);

                memoryValue = readByte(zeroPageAddressIndexedX);
                _programCounter++;
                break;

            case (byte)OpCodeValues.ANDIndirectIndexed:
                var zeroPageIndexY = readByte(_programCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY = readByte((ushort)((zeroPageIndexY + 1) % MsbMultiplier));

                var zeroPageAddressIndexY = zeroPageLsbY + zeroPageMsbY * MsbMultiplier + IY;
                memoryValue = readByte((ushort)zeroPageAddressIndexY);
                _programCounter++;
                break;
        }

        Accumulator = (byte)(memoryValue & Accumulator);

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, Accumulator.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, Accumulator.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessINY()
    {
        _programCounter++;

        IY++;

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, IY.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, IY.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessDEY()
    {
        _programCounter++;

        IY--;

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, IY.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, IY.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessINX()
    {
        _programCounter++;

        IX++;

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, IX.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, IX.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    private int? ProcessDEX()
    {
        _programCounter++;

        IX--;

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, IX.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, IX.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessDEC(Instruction instruction)
    {
        _programCounter++;

        ushort memoryLocation = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.DECZeroPage:
                memoryLocation = readByte(_programCounter++);
                break;

            case (byte)OpCodeValues.DECZeroPageX:
                memoryLocation = (ushort)(readByte(_programCounter++) + IX % MsbMultiplier);
                break;

            case (byte)OpCodeValues.DECAbsolute:
                memoryLocation = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                break;

            case (byte)OpCodeValues.DECAbsoluteX:
                memoryLocation = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IX);
                break;
        }

        writeByte(memoryLocation, (byte)(readByte(memoryLocation) - 1));

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, readByte(memoryLocation).IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, readByte(memoryLocation).IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessINC(Instruction instruction)
    {
        _programCounter++;

        ushort memoryLocation = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.INCZeroPage:
                memoryLocation = readByte(_programCounter++);
                break;

            case (byte)OpCodeValues.INCZeroPageX:
                memoryLocation = (ushort)(readByte(_programCounter++) + IX % MsbMultiplier);
                break;

            case (byte)OpCodeValues.INCAbsolute:
                memoryLocation = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                break;

            case (byte)OpCodeValues.INCAbsoluteX:
                memoryLocation = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IX);
                break;
        }

        writeByte(memoryLocation, (byte)(readByte(memoryLocation) + 1));

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, readByte(memoryLocation).IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, readByte(memoryLocation).IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessASL(Instruction instruction)
    {
        _programCounter++;

        var msb = Bit.Zero;

        byte finalValue = 0;
        switch (instruction.Code)
        {
            case (byte)OpCodeValues.ASLAccumulator:
            {
                var value = Accumulator;
                msb = value.GetBit(Byte.Seven);
                Accumulator = (byte)(value << 1);
                finalValue = Accumulator;
                break;
            }

            case (byte)OpCodeValues.ASLZeroPage:
            {
                var address = readByte(_programCounter++);
                var value = readByte(address);
                msb = value.GetBit(Byte.Seven);
                value = (byte)(value << 1);
                writeByte(address, value);
                finalValue = value;
                break;
            }

            case (byte)OpCodeValues.ASLZeroPageX:
            {
                var address = (ushort)((readByte(_programCounter++) + IX) % MsbMultiplier);
                var value = readByte(address);
                msb = value.GetBit(Byte.Seven);
                value = (byte)(value << 1);
                writeByte(address, value);
                finalValue = value;
                break;
            }

            case (byte)OpCodeValues.ASLAbsolute:
            {
                var low = readByte(_programCounter++);
                var high = readByte(_programCounter++);
                var address = (ushort)(low + high * MsbMultiplier);
                var value = readByte(address);
                msb = value.GetBit(Byte.Seven);
                value = (byte)(value << 1);
                writeByte(address, value);
                finalValue = value;
                break;
            }

            case (byte)OpCodeValues.ASLAbsoluteX:
            {
                var low = readByte(_programCounter++);
                var high = readByte(_programCounter++);
                var address = (ushort)(low + high * MsbMultiplier + IX);
                var value = readByte(address);
                msb = value.GetBit(Byte.Seven);
                value = (byte)(value << 1);
                writeByte(address, value);
                finalValue = value;
                break;
            }
        }


        Status = Status.SetBit((Byte)Statuses.CarryFlag, msb);
        Status = Status.SetBit((Byte)Statuses.ZeroFlag, finalValue.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, finalValue.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessROR(Instruction instruction)
    {
        _programCounter++;

        var lsb = Bit.Zero;

        byte finalValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.RORAccumulator:
                lsb = Accumulator.GetBit(Byte.Zero);
                Accumulator = (byte)(Accumulator >> 1).LowWord();
                Accumulator = Accumulator.SetBit(Byte.Seven, Status.GetBit((Byte)Statuses.CarryFlag));
                finalValue = Accumulator;
                break;

            case (byte)OpCodeValues.RORZeroPage:
                var zeroPageAddress = readByte(_programCounter);
                lsb = readByte(zeroPageAddress).GetBit(Byte.Zero);
                writeByte(zeroPageAddress, (byte)(readByte(zeroPageAddress) >> 1).LowWord());
                writeByte(zeroPageAddress,
                    readByte(zeroPageAddress).SetBit(Byte.Seven, Status.GetBit((Byte)Statuses.CarryFlag)));
                _programCounter++;
                finalValue = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.RORZeroPageX:
                var zeroPageAddressX = (ushort)(readByte(_programCounter) + IX % MsbMultiplier);
                lsb = readByte(zeroPageAddressX).GetBit(Byte.Zero);
                writeByte(zeroPageAddressX, (byte)(readByte(zeroPageAddressX) >> 1).LowWord());
                writeByte(zeroPageAddressX,
                    readByte(zeroPageAddressX).SetBit(Byte.Seven, Status.GetBit((Byte)Statuses.CarryFlag)));
                _programCounter++;
                finalValue = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.RORAbsolute:
                var absoluteAddress = (ushort)(readByte(_programCounter) + readByte((ushort)(_programCounter + 1)) *
                    MsbMultiplier);
                lsb = readByte(absoluteAddress).GetBit(Byte.Zero);
                writeByte(absoluteAddress, (byte)(readByte(absoluteAddress) >> 1).LowWord());
                writeByte(absoluteAddress,
                    readByte(absoluteAddress).SetBit(Byte.Seven, Status.GetBit((Byte)Statuses.CarryFlag)));
                _programCounter += 2;
                finalValue = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.RORAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(_programCounter) + readByte((ushort)(_programCounter + 1)) *
                    MsbMultiplier + IX);
                lsb = readByte(absoluteAddressX).GetBit(Byte.Zero);
                writeByte(absoluteAddressX, (byte)(readByte(absoluteAddressX) >> 1).LowWord());
                writeByte(absoluteAddressX,
                    readByte(absoluteAddressX).SetBit(Byte.Seven, Status.GetBit((Byte)Statuses.CarryFlag)));
                _programCounter += 2;
                finalValue = readByte(absoluteAddressX);
                break;
        }

        Status = Status.SetBit((Byte)Statuses.CarryFlag, lsb);
        Status = Status.SetBit((Byte)Statuses.ZeroFlag, finalValue.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, finalValue.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessROL(Instruction instruction)
    {
        _programCounter++;

        var msb = Bit.Zero;

        byte finalValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.ROLAccumulator:
                msb = Accumulator.GetBit(Byte.Seven);
                Accumulator = (byte)(Accumulator << 1).LowWord();
                Accumulator = Accumulator.SetBit(Byte.Zero, Status.GetBit((Byte)Statuses.CarryFlag));
                finalValue = Accumulator;
                break;

            case (byte)OpCodeValues.ROLZeroPage:
            {
                var zeroPageAddress = readByte(_programCounter);
                msb = readByte(zeroPageAddress).GetBit(Byte.Seven);
                var value = readByte(zeroPageAddress);
                finalValue = (byte)((value << 1) | (byte)Status.GetBit((byte)Statuses.CarryFlag));
                writeByte(zeroPageAddress, finalValue);
                _programCounter++;
                break;
            }
            case (byte)OpCodeValues.ROLZeroPageX:
            {
                var zeroPageAddressX = (ushort)(readByte(_programCounter) + IX % MsbMultiplier);
                msb = readByte(zeroPageAddressX).GetBit(Byte.Seven);
                var value = readByte(zeroPageAddressX);
                finalValue = (byte)((value << 1) | (byte)Status.GetBit((byte)Statuses.CarryFlag));
                writeByte(zeroPageAddressX, finalValue);
                _programCounter++;
                break;
            }
            case (byte)OpCodeValues.ROLAbsolute:
            {
                var absoluteAddress = (ushort)(readByte(_programCounter) + readByte((ushort)(_programCounter + 1)) *
                    MsbMultiplier);
                msb = readByte(absoluteAddress).GetBit(Byte.Seven);
                var value = readByte(absoluteAddress);
                finalValue = (byte)((value << 1) | (byte)Status.GetBit((byte)Statuses.CarryFlag));
                writeByte(absoluteAddress, finalValue);
                _programCounter += 2;
                break;
            }
            case (byte)OpCodeValues.ROLAbsoluteX:
            {
                var absoluteAddressX = (ushort)(readByte(_programCounter) + readByte((ushort)(_programCounter + 1)) *
                    MsbMultiplier + IX);
                msb = readByte(absoluteAddressX).GetBit(Byte.Seven);
                var value = readByte(absoluteAddressX);
                finalValue = (byte)((value << 1) | (byte)Status.GetBit((byte)Statuses.CarryFlag));
                writeByte(absoluteAddressX, finalValue);
                _programCounter += 2;

                break;
            }
        }

        Status = Status.SetBit((Byte)Statuses.CarryFlag, msb);
        Status = Status.SetBit((Byte)Statuses.ZeroFlag, finalValue.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, finalValue.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessLSR(Instruction instruction)
    {
        _programCounter++;

        var lsb = Bit.Zero;
        byte finalValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.LSRAccumulator:
                lsb = Accumulator.GetBit(Byte.Zero);
                Accumulator = (byte)(Accumulator >> 1).LowWord();
                finalValue = Accumulator;
                break;

            case (byte)OpCodeValues.LSRZeroPage:
            {
                var zeroPageAddress = readByte(_programCounter);
                var value = readByte(zeroPageAddress);
                finalValue = (byte)((value << 1) | (byte)Status.GetBit((byte)Statuses.CarryFlag));
                writeByte(zeroPageAddress, finalValue);
                _programCounter++;
                break;
            }
            case (byte)OpCodeValues.LSRZeroPageX:
            {
                var zeroPageAddressX = (ushort)(readByte(_programCounter) + IX % MsbMultiplier);
                var value = readByte(zeroPageAddressX);
                finalValue = (byte)((value << 1) | (byte)Status.GetBit((byte)Statuses.CarryFlag));
                writeByte(zeroPageAddressX, finalValue);
                _programCounter++;
                break;
            }
            case (byte)OpCodeValues.LSRAbsolute:
            {
                var absoluteAddress = (ushort)(readByte(_programCounter) + readByte((ushort)(_programCounter + 1)) *
                    MsbMultiplier);
                var value = readByte(absoluteAddress);
                finalValue = (byte)((value << 1) | (byte)Status.GetBit((byte)Statuses.CarryFlag));
                writeByte(absoluteAddress, finalValue);
                _programCounter += 2;
                break;
            }
            case (byte)OpCodeValues.LSRAbsoluteX:
            {
                var absoluteAddressX = (ushort)(readByte(_programCounter) + readByte((ushort)(_programCounter + 1)) *
                    MsbMultiplier + IX);
                var value = readByte(absoluteAddressX);
                finalValue = (byte)((value << 1) | (byte)Status.GetBit((byte)Statuses.CarryFlag));
                writeByte(absoluteAddressX, finalValue);
                _programCounter += 2;
                break;
            }
        }

        Status = Status.SetBit((Byte)Statuses.CarryFlag, lsb);
        Status = Status.SetBit((Byte)Statuses.ZeroFlag, finalValue.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, finalValue.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessNOP()
    {
        _programCounter++;
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessPHA()
    {
        _programCounter++;

        Push(Accumulator);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessPHP()
    {
        _programCounter++;

        Push(Status);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessPLA()
    {
        _programCounter++;

        Accumulator = Pop();

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, Accumulator.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, Accumulator.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessPLP()
    {
        _programCounter++;

        Status = Pop();

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessJSR()
    {
        _programCounter++;

        var absoluteAddress = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
            MsbMultiplier);

        var addressToPush = (ushort)(_programCounter - 1);

        Push(addressToPush.LowByte());
        Push(addressToPush.HighByte());
        _programCounter = absoluteAddress;

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessRTS()
    {
        _programCounter = (ushort)(Pop() * MsbMultiplier + Pop() + 1);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessLDA(Instruction instruction)
    {
        _programCounter++;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.LDAImmediate:
                Accumulator = readByte(_programCounter++);
                break;

            case (byte)OpCodeValues.LDAZeroPage:
                var zeroPageAddress = readByte(_programCounter++);
                Accumulator = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.LDAZeroPageX:
                var zeroPageAddressX = readByte(_programCounter++) + IX % MsbMultiplier;
                Accumulator = readByte((ushort)zeroPageAddressX);
                break;

            case (byte)OpCodeValues.LDAAbsolute:
                var absoluteAddress = readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier;
                Accumulator = readByte((ushort)absoluteAddress);
                break;

            case (byte)OpCodeValues.LDAAbsoluteX:
                var absoluteAddressX = readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IX;
                Accumulator = readByte((ushort)absoluteAddressX);
                break;

            case (byte)OpCodeValues.LDAAbsoluteY:
                var absoluteAddressY = readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IY;
                Accumulator = readByte((ushort)absoluteAddressY);
                break;

            case (byte)OpCodeValues.LDAIndexedIndirect:
                var zeroPageIndexX = (ushort)(readByte(_programCounter) + IX) % MsbMultiplier;
                var zeroPageLsbX = readByte((ushort)zeroPageIndexX);
                var zeroPageMsbX = readByte((ushort)((zeroPageIndexX + 1) % MsbMultiplier));
                var zeroPageAddressIndexedX = zeroPageLsbX + zeroPageMsbX * MsbMultiplier;

                Accumulator = readByte((ushort)zeroPageAddressIndexedX);
                _programCounter++;
                break;

            case (byte)OpCodeValues.LDAIndirectIndexed:
                var zeroPageIndexY = readByte(_programCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY = readByte((ushort)((zeroPageIndexY + 1) % MsbMultiplier));

                var zeroPageAddressIndexY = zeroPageLsbY + zeroPageMsbY * MsbMultiplier + IY;
                Accumulator = readByte((ushort)zeroPageAddressIndexY);
                _programCounter++;
                break;
        }

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, Accumulator.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, Accumulator.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessSTA(Instruction instruction)
    {
        _programCounter++;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.STAZeroPage:
                var zeroPageAddress = readByte(_programCounter++);
                writeByte(zeroPageAddress, Accumulator);
                break;

            case (byte)OpCodeValues.STAZeroPageX:
                var zeroPageAddressX = (ushort)(readByte(_programCounter++) + IX % MsbMultiplier);
                writeByte(zeroPageAddressX, Accumulator);
                break;

            case (byte)OpCodeValues.STAAbsolute:
                var absoluteAddress = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                writeByte(absoluteAddress, Accumulator);
                break;

            case (byte)OpCodeValues.STAAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IX);
                writeByte(absoluteAddressX, Accumulator);
                break;

            case (byte)OpCodeValues.STAAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IY);
                writeByte(absoluteAddressY, Accumulator);
                break;

            case (byte)OpCodeValues.STAIndexedIndirect:
                var zeroPageIndexX = (ushort)((readByte(_programCounter) + IX) % MsbMultiplier);
                var zeroPageLsbX = readByte(zeroPageIndexX);
                var zeroPageMsbX = readByte((ushort)((zeroPageIndexX + 1) % MsbMultiplier));
                var zeroPageAddressIndexedX = (ushort)(zeroPageLsbX + zeroPageMsbX * MsbMultiplier);
                writeByte(zeroPageAddressIndexedX, Accumulator);
                _programCounter++;
                break;

            case (byte)OpCodeValues.STAIndirectIndexed:
                var zeroPageIndexY = readByte(_programCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY = readByte((ushort)((zeroPageIndexY + 1) % MsbMultiplier));
                var zeroPageAddressIndexY = (ushort)(zeroPageLsbY + zeroPageMsbY * MsbMultiplier + IY);
                writeByte(zeroPageAddressIndexY, Accumulator);
                _programCounter++;
                break;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessTAX()
    {
        _programCounter++;

        IX = Accumulator;

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, IX.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, IX.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessTAY()
    {
        _programCounter++;

        IY = Accumulator;

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, IY.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, IY.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessTSX()
    {
        _programCounter++;

        IX = _stackPointer;

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, IX.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, IX.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessTYA()
    {
        _programCounter++;

        Accumulator = IY;

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, Accumulator.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, Accumulator.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessTXA()
    {
        _programCounter++;

        Accumulator = IX;

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, Accumulator.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, Accumulator.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessTXS()
    {
        _programCounter++;

        _stackPointer = IX;

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessSTX(Instruction instruction)
    {
        _programCounter++;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.STXZeroPage:
                var zeroPageAddress = readByte(_programCounter++);
                writeByte(zeroPageAddress, IX);
                break;

            case (byte)OpCodeValues.STXZeroPageY:
                var zeroPageAddressX = (ushort)(readByte(_programCounter++) + IY % MsbMultiplier);
                writeByte(zeroPageAddressX, IX);
                break;

            case (byte)OpCodeValues.STXAbsolute:
                var absoluteAddress = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                writeByte(absoluteAddress, IX);
                break;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessSTY(Instruction instruction)
    {
        _programCounter++;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.STYZeroPage:
                var zeroPageAddress = readByte(_programCounter++);
                writeByte(zeroPageAddress, IY);
                break;

            case (byte)OpCodeValues.STYZeroPageX:
                var zeroPageAddressX = (ushort)(readByte(_programCounter++) + IX % MsbMultiplier);
                writeByte(zeroPageAddressX, IY);
                break;

            case (byte)OpCodeValues.STYAbsolute:
                var absoluteAddress = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                writeByte(absoluteAddress, IY);
                break;
        }

        return null;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessLDY(Instruction instruction)
    {
        _programCounter++;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.LDYImmediate:
                IY = readByte(_programCounter++);
                break;

            case (byte)OpCodeValues.LDYZeroPage:
                var zeroPageAddress = readByte(_programCounter++);
                IY = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.LDYZeroPageX:
                var zeroPageAddressX = (ushort)(readByte(_programCounter++) + IX % MsbMultiplier);
                IY = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.LDYAbsolute:
                var absoluteAddress = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                IY = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.LDYAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IX);
                IY = readByte(absoluteAddressX);
                break;
        }

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, IY.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, IY.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessLDX(Instruction instruction)
    {
        _programCounter++;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.LDXImmediate:
                IX = readByte(_programCounter++);
                break;

            case (byte)OpCodeValues.LDXZeroPage:
                var zeroPageAddress = readByte(_programCounter++);
                IX = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.LDXZeroPageY:
                var zeroPageAddressY = (ushort)(readByte(_programCounter++) + IY % MsbMultiplier);
                IX = readByte(zeroPageAddressY);
                break;

            case (byte)OpCodeValues.LDXAbsolute:
                var absoluteAddress = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                IX = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.LDXAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier + IY);
                IX = readByte(absoluteAddressY);
                break;
        }

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, IX.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, IX.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessBIT(Instruction instruction)
    {
        _programCounter++;

        ushort address;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.BITZeroPage:
                address = readByte(_programCounter++);
                break;

            case (byte)OpCodeValues.BITAbsolute:
                address = (ushort)(readByte(_programCounter++) + readByte(_programCounter++) *
                    MsbMultiplier);
                break;

            default:
                throw new OpCodeNotFoundException($"OpCode {instruction.Code} does not exist");
        }

        var value = (byte)(readByte(address) & Accumulator);

        writeByte(address, value);

        Status = Status.SetBit((Byte)Statuses.ZeroFlag, value.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.OverflowFlag, value.GetBit(Byte.Six));
        Status = Status.SetBit((Byte)Statuses.NegativeFlag, value.GetBit(Byte.Seven));

        return null;
    }

    #endregion

    #region Memory Related Code - inlined for performance

    public const int RelativeAddressBackwardsLimit = -126;
    public const int RelativeAddressForwardsLimit = 129;
    public const int ProgramCounterOffset = 2;
    public const int MsbMultiplier = 256;
    public const int StackPage = 1;
    public const ushort DefaultBRKHandler = 0xF100;
    public const ushort NMIHandlerLowByte = 0xFFFA;
    public const ushort NMIHandlerHighByte = 0xFFFB;
    public const ushort ResetVectorLow = 0xFFFC;
    public const ushort ResetVectorHigh = 0xFFFD;
    public const ushort IrqHandlerLowByte = 0xFFFE;
    public const ushort IrqHandlerHighByte = 0xFFFF;

    #endregion
}