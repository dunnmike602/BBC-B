namespace MLDComputing.Emulators.BBCSim._6502.Engine;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assembler;
using Communication;
using Constants;
using Disassembler;
using Disassembler.Interfaces;
using Enums;
using Exceptions;
using Extensions;
using Storage;
using Byte = Storage.Byte;

public partial class Cpu6502(Func<ushort, byte> readByte, Action<ushort, byte> writeByte)
{
    private int _cyclesPerFrame;

    private Instruction[]? _instructions;

    public int CyclesPerSecond;

    public bool EnableLogging = false;

    public bool EnableProcessorEvents;

    public int ExpectedProcessorSpeed;

    public long InstructionCount;

    public bool IsInTestMode;

    public volatile bool IsRunning;

    public bool SingleStepModeOn;

    public long TotalCyclesExecuted;

    public long TotalElapsedInterruptTicks;

    public long TotalElapsedTicks;

    public long VideoFrameIntervalTicks;

    public event ExecuteHandler? Execute;

    public event ProcessorErrorHandler? ProcessorError;

    public void RaiseIrq()
    {
        _irqPending = true;
    }

    public void ClearIrq()
    {
        _irqPending = false;
    }

    /// <summary>
    ///     Lightweight initialisation for testing purposes
    /// </summary>
    /// <param name="cyclesPerSecond">Expected Cycles Per Second</param>
    /// <param name="videoFrameRate">Expected Frame Rate</param>
    /// <param name="programCounter">Start value for program counter, ignored if</param>
    /// <param name="isInTestMode">
    ///     Boolean that indicates whether processor runs in test mode. This cause the BRK instruction
    ///     to stop execution.
    /// </param>
    public void InitialiseSlim(int cyclesPerSecond, int videoFrameRate, ushort? programCounter = null,
        bool isInTestMode = false)
    {
        IsInTestMode = isInTestMode;

        CyclesPerSecond = cyclesPerSecond;

        VideoFrameIntervalTicks = (int)(1 / (float)videoFrameRate * Stopwatch.Frequency);

        _cyclesPerFrame = GetNumberOfInstructionsPerVideoRefresh();

        ExpectedProcessorSpeed = cyclesPerSecond;
        _instructions = Data.GetInstructions();

        if (programCounter.HasValue)
        {
            ProgramCounter = programCounter.Value;
        }

        ResetProcessor();

        IsRunning = true;

        _dis = Disassembler.Build();
    }

    /// <summary>
    ///     Full initialise for the BBC emulator performs a processor reset in line with the BBC requirements
    /// </summary>
    /// <param name="cyclesPerSecond">Expected Cycles Per Second</param>
    /// <param name="videoFrameRate">Expected Frame Rate</param>
    public void Initialise(int cyclesPerSecond, int videoFrameRate)
    {
        InitialiseSlim(cyclesPerSecond, videoFrameRate);
        InitProgramCounter();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetProcessor()
    {
        TotalCyclesExecuted = 0;
        Accumulator = 0;
        IX = 0;
        IY = 0;
        Status = Status.SetBit((Byte)Statuses.Unused, Bit.One);
        StackPointer = 0xFF;

        TotalElapsedTicks = 0;
        TotalElapsedInterruptTicks = 0;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitProgramCounter()
    {
        ProgramCounter = (ushort)(readByte(MachineConstants.ProcessorSetup.ResetVectorLow) |
                                  (readByte(MachineConstants.ProcessorSetup.ResetVectorHigh) << 8));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long RunSingleFrame()
    {
        var frameCycles = 0;

        try
        {
            while (frameCycles < _cyclesPerFrame)
            {
                var used = ExecuteNext();

                if (_irqPending && Status.GetBit((Byte)Statuses.InterruptDisable) == Bit.Zero)
                {
                    HandleIrq(); // Push PC + SR, then jump to IRQ vector
                }

                // 0 means either unknown opcode or HALT, we will force the processor to stop here
                if (used == 0)
                {
                    IsRunning = false;
                    break;
                }

                frameCycles += used;

                TotalCyclesExecuted = long.MaxValue - used <= TotalCyclesExecuted
                    ? used
                    : TotalCyclesExecuted + used;

                // Optional: allow breakpoints or stepping out
                if (SingleStepModeOn)
                {
                    break;
                }
            }

            return frameCycles;
        }
        catch (Exception ex)
        {
            HandleError(new Instruction(),
                $"An error occured after {frameCycles} cycles. The exception was:{Environment.NewLine}{ex} ");
        }

        return frameCycles;
    }

    private void HandleIrq()
    {
        if (Status.GetBit((Byte)Statuses.InterruptDisable) == Bit.One)
        {
            // IRQs are masked (di
            // sabled), so ignore it
            return;
        }

        // Step 1: Push PC high and low bytes (address of next instruction)
        Push((byte)((ProgramCounter >> 8) & 0xFF)); // High byte
        Push((byte)(ProgramCounter & 0xFF)); // Low byte

        // Step 2: Push processor status with B flag cleared (bit 4 = 0)
        var flagsToPush = (byte)(Status & 0xEF); // Clear B flag (bit 4)
        Push(flagsToPush);

        // Step 3: Set Interrupt Disable flag (bit 2)
        Status = Status.SetBit((Byte)Statuses.InterruptDisable, Bit.One);

        // Step 4: Read IRQ vector ($FFFE–$FFFF) and jump to it
        var lo = readByte(0xFFFE);
        var hi = readByte(0xFFFF);
        ProgramCounter = (ushort)(lo + (hi << 8));
    }

    private int GetNumberOfInstructionsPerVideoRefresh()
    {
        var cyclesPerTick = CyclesPerSecond / (float)Stopwatch.Frequency;

        return (int)(cyclesPerTick * VideoFrameIntervalTicks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ExecuteNext()
    {
        // Debug.Print($"PC={ProgramCounter:X4}");
        var opCode = readByte(ProgramCounter);

        var instruction = _instructions![opCode];

        // Instruction not found
        if (instruction.IsNotFound())
        {
            HandleError(instruction, $"Instruction with OpCode {opCode} not implemented by 6502.");
        }

        if (EnableProcessorEvents)
        {
            var executeEventArgs = new ExecuteEventArgs
            {
                Address = ProgramCounter,
                Instruction = instruction,
                Registers = new Registers
                {
                    Accumulator = Accumulator,
                    IX = IX,
                    IY = IY,
                    ProgramCounter = ProgramCounter,
                    StackPointer = StackPointer,
                    Status = Status
                }
            };

            if (EnableLogging)
            {
                var line = _dis.Disassemble(readByte, ProgramCounter, (ushort)(ProgramCounter + instruction.Bytes), 16)
                    .Select(op => op.MemoryAddress.ToString("X4") + " " + op.Definition?.Mnemonic + " " + op.Argument);

                executeEventArgs.InstructionText = line;
            }

            Execute?.Invoke(this,
                executeEventArgs);
        }

        InstructionCount++;

        #region Instruction Dispatch

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
                actualCyclesUsed = ProcessRelativeBranch(Statuses.Carry, Bit.Zero);
                break;

            case Data.BCS:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.Carry, Bit.One);
                break;

            case Data.BEQ:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.Zero, Bit.One);
                break;

            case Data.BIT:
                actualCyclesUsed = ProcessBIT(instruction);
                break;

            case Data.BMI:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.Negative, Bit.One);
                break;

            case Data.BNE:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.Zero, Bit.Zero);
                break;

            case Data.BPL:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.Negative, Bit.Zero);
                break;

            case Data.CLC:
                actualCyclesUsed = SetFlag(Statuses.Carry, Bit.Zero);
                break;

            case Data.CLD:
                actualCyclesUsed = SetFlag(Statuses.DecimalMode, Bit.Zero);
                break;

            case Data.CLI:
                actualCyclesUsed = SetFlag(Statuses.InterruptDisable, Bit.Zero);
                break;

            case Data.CLV:
                actualCyclesUsed = SetFlag(Statuses.Overflow, Bit.Zero);
                break;

            case Data.BRK:
                actualCyclesUsed = ProcessBRK();
                break;

            case Data.BVC:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.Overflow, Bit.Zero);
                break;

            case Data.BVS:
                actualCyclesUsed = ProcessRelativeBranch(Statuses.Overflow, Bit.One);
                break;

            case Data.CMP:
                actualCyclesUsed = ProcessCMP(instruction);
                break;

            case Data.DBG:
                // Pseudo-op that allows code to be broke into
                SingleStepModeOn = true;
                ProgramCounter++;
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
                actualCyclesUsed = SetFlag(Statuses.Carry, Bit.One);
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
        }

        #endregion

        return actualCyclesUsed ?? instruction.Cycles;
    }

    private void HandleError(Instruction instruction, string errorMessage)
    {
        ProcessorError?.Invoke(this,
            new ProcessorErrorEventArgs
            {
                ErrorMessage = errorMessage,
                Instruction = instruction,
                Registers = new Registers
                {
                    Accumulator = Accumulator,
                    IX = IX,
                    IY = IY,
                    ProgramCounter = ProgramCounter,
                    StackPointer = StackPointer,
                    Status = Status
                }
            });
    }

    #region Registers

    public byte StackPointer;

    public byte Accumulator;

    public byte IX;

    public byte IY;

    public byte Status;

    public ushort ProgramCounter;

    private bool _irqPending;

    private IDisassembler _dis;

    #endregion

    #region Execution code for each 6502 instruction keeping this inline for the moment for performance

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessSBC(Instruction instruction)
    {
        ProgramCounter++;

        byte operand = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.SBCImmediate:
                operand = readByte(ProgramCounter++);
                break;

            case (byte)OpCodeValues.SBCZeroPage:
                var zeroPageAddress = readByte(ProgramCounter++);
                operand = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.SBCZeroPageX:
                var zeroPageAddressX = (byte)(readByte(ProgramCounter++) + IX);
                operand = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.SBCAbsolute:
                var absoluteAddress = (ushort)(readByte(ProgramCounter++) +
                                               readByte(ProgramCounter++) *
                                               MachineConstants.ProcessorSetup.MsbMultiplier);
                operand = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.SBCAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(ProgramCounter++) +
                                                readByte(ProgramCounter++) *
                                                MachineConstants.ProcessorSetup.MsbMultiplier + IX);
                operand = readByte(absoluteAddressX);
                break;

            case (byte)OpCodeValues.SBCAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(ProgramCounter++) +
                                                readByte(ProgramCounter++) *
                                                MachineConstants.ProcessorSetup.MsbMultiplier + IY);
                operand = readByte(absoluteAddressY);
                break;

            case (byte)OpCodeValues.SBCIndexedIndirect:
                var zeroPageIndexX = (byte)(readByte(ProgramCounter) + IX);
                var zeroPageLsbX = readByte(zeroPageIndexX);
                var zeroPageMsbX = readByte((byte)(zeroPageIndexX + 1));
                var zeroPageAddressIndexedX =
                    (ushort)(zeroPageLsbX + zeroPageMsbX * MachineConstants.ProcessorSetup.MsbMultiplier);
                operand = readByte(zeroPageAddressIndexedX);
                ProgramCounter++;
                break;

            case (byte)OpCodeValues.SBCIndirectIndexed:
                var zeroPageIndexY = readByte(ProgramCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY =
                    readByte((ushort)((zeroPageIndexY + 1) % MachineConstants.ProcessorSetup.MsbMultiplier));

                var zeroPageAddressIndexY =
                    (ushort)(zeroPageLsbY + zeroPageMsbY * MachineConstants.ProcessorSetup.MsbMultiplier + IY);
                operand = readByte(zeroPageAddressIndexY);
                ProgramCounter++;
                break;
        }

        var decimalMode = Status.GetBit((Byte)Statuses.DecimalMode) == Bit.One;
        var carryIn = Status.GetBit((Byte)Statuses.Carry) == Bit.One;
        var carry = carryIn ? 0 : 1; // In SBC, Carry Clear means borrow

        var value = operand ^ 0xFF; // Invert bits for two's complement subtraction
        var sum = Accumulator + value + (1 - carry); // A - M - (1 - C) == A + ~M + C

        int result;
        bool carryOut;
        bool overflow;

        if (!decimalMode)
        {
            result = sum & 0xFF;
            carryOut = sum > 0xFF;

            // Overflow occurs if the sign of Accumulator and operand differ, and the result sign differs from Accumulator
            overflow = ((Accumulator ^ operand) & (Accumulator ^ result) & 0x80) != 0;
        }
        else
        {
            var low = (Accumulator & 0x0F) - (operand & 0x0F) - (carryIn ? 0 : 1);
            var borrow = false;

            if (low < 0)
            {
                low += 10;
                borrow = true;
            }

            var high = (Accumulator >> 4) - (operand >> 4) - (borrow ? 1 : 0);

            if (high < 0)
            {
                high += 10;
                carryOut = false; // borrow occurred
            }
            else
            {
                carryOut = true; // no borrow
            }

            result = ((high << 4) | (low & 0x0F)) & 0xFF;

            // Overflow logic remains binary (optional: undefined in decimal mode)
            overflow = ((Accumulator ^ operand) & (Accumulator ^ sum) & 0x80) != 0;
        }

        Accumulator = (byte)result;

        // Carry: set if no borrow occurred
        Status = Status.SetBit((Byte)Statuses.Carry, carryOut ? Bit.One : Bit.Zero);

        // Negative: set if result has bit 7 set
        Status = Status.SetBit((Byte)Statuses.Negative, (result & 0x80) != 0 ? Bit.One : Bit.Zero);

        // Zero: set if result is zero
        Status = Status.SetBit((Byte)Statuses.Zero, result == 0 ? Bit.One : Bit.Zero);

        // Overflow: from signed interpretation
        Status = Status.SetBit((Byte)Statuses.Overflow, overflow ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessADC(Instruction instruction)
    {
        ProgramCounter++;

        byte operand = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.ADCImmediate:
                operand = readByte(ProgramCounter++);
                break;

            case (byte)OpCodeValues.ADCZeroPage:
                var zeroPageAddress = readByte(ProgramCounter++);
                operand = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.ADCZeroPageX:
                var zeroPageAddressX = (byte)(readByte(ProgramCounter++) + IX);
                operand = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.ADCAbsolute:
                var absoluteAddress = (ushort)(readByte(ProgramCounter++) +
                                               readByte(ProgramCounter++) *
                                               MachineConstants.ProcessorSetup.MsbMultiplier);
                operand = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.ADCAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(ProgramCounter++) +
                                                readByte(ProgramCounter++) *
                                                MachineConstants.ProcessorSetup.MsbMultiplier + IX);
                operand = readByte(absoluteAddressX);
                break;

            case (byte)OpCodeValues.ADCAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(ProgramCounter++) +
                                                readByte(ProgramCounter++) *
                                                MachineConstants.ProcessorSetup.MsbMultiplier + IY);
                operand = readByte(absoluteAddressY);
                break;

            case (byte)OpCodeValues.ADCIndexedIndirect:
                var basePointer = readByte(ProgramCounter); // usually $00
                var pointerAddress = (byte)(basePointer + IX); // wrap around zero page
                var low = readByte(pointerAddress);
                var high = readByte((byte)(pointerAddress + 1));
                var effectiveAddress = (ushort)(low | (high << 8));
                operand = readByte(effectiveAddress);
                ProgramCounter++;
                break;

            case (byte)OpCodeValues.ADCIndirectIndexed:
                var zeroPageIndexY = readByte(ProgramCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY =
                    readByte((ushort)((zeroPageIndexY + 1) % MachineConstants.ProcessorSetup.MsbMultiplier));

                var zeroPageAddressIndexY =
                    (ushort)(zeroPageLsbY + zeroPageMsbY * MachineConstants.ProcessorSetup.MsbMultiplier + IY);
                operand = readByte(zeroPageAddressIndexY);
                ProgramCounter++;
                break;
        }

        var decimalMode = Status.GetBit((Byte)Statuses.DecimalMode) == Bit.One;
        var carryIn = Status.GetBit((Byte)Statuses.Carry) == Bit.One;
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
        Status = Status.SetBit((Byte)Statuses.Carry, carryOut ? Bit.One : Bit.Zero);

        // Set or clear Negative flag (bit 7 of result)
        Status = Status.SetBit((Byte)Statuses.Negative, (result & 0x80) != 0 ? Bit.One : Bit.Zero);

        // Set or clear Zero flag
        Status = Status.SetBit((Byte)Statuses.Zero, result == 0 ? Bit.One : Bit.Zero);

        // Set or clear Overflow flag
        Status = Status.SetBit((Byte)Statuses.Overflow, overflow ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessJMP(Instruction instruction)
    {
        ProgramCounter++;

        ushort memoryValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.JMPAbsolute:
                memoryValue = (ushort)(readByte(ProgramCounter++) +
                                       readByte(ProgramCounter++) * MachineConstants.ProcessorSetup.MsbMultiplier);
                break;

            case (byte)OpCodeValues.JMPIndirect:
                memoryValue = (ushort)(readByte(ProgramCounter++) +
                                       readByte(ProgramCounter++) * MachineConstants.ProcessorSetup.MsbMultiplier);
                memoryValue = (ushort)(readByte(memoryValue) +
                                       readByte((ushort)(memoryValue + 1)) *
                                       MachineConstants.ProcessorSetup.MsbMultiplier);
                break;
        }

        ProgramCounter = memoryValue;

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? SetFlag(Statuses flagToSet, Bit state)
    {
        ProgramCounter++;

        Status = Status.SetBit((Byte)flagToSet, state);

        return null;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessRelativeBranch(Statuses flagToCheck, Bit state)
    {
        // 1) PC points at opcode; read the offset from the next byte
        var operandAddr = (ushort)(ProgramCounter + 1);
        var offset = (sbyte)readByte(operandAddr);

        // 2) Calculate the address immediately *after* the operand
        var nextInstr = (ushort)(operandAddr + 1);

        // 3) If the flag matches, branch; otherwise just fall through
        if (Status.GetBit((Byte)flagToCheck) == state)
        {
            ProgramCounter = (ushort)(nextInstr + offset);
        }
        else
        {
            ProgramCounter = nextInstr;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessRTI()
    {
        var status = Pop();
        Status = status.SetBit((Byte)Statuses.BreakCommand, Bit.Zero); // Clear Break flag

        var pcl = Pop(); // Pull low byte first
        var pch = Pop(); // Then high byte
        ProgramCounter = (ushort)((pch << 8) | pcl);
        _irqPending = false;
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessBRK()
    {
        if (IsInTestMode)
        {
            // When processor is in test mode BRK simply acts as a HALT instruction
            return 0;
        }

        // BRK is 2 byte instruction but second byte is ignored so we skip 2 bytes here
        ProgramCounter += 2;

        // Break flag is only set in value pushed to stack, not in actual Status
        var statusToPush = Status.SetBit((Byte)Statuses.BreakCommand, Bit.One);

        // Push return address: High byte first, then low byte
        Push((byte)((ProgramCounter >> 8) & 0xFF));
        Push((byte)(ProgramCounter & 0xFF));

        // Push status with B flag set
        Push(statusToPush);

        // Set Interrupt Disable flag in Status
        Status = Status.SetBit((Byte)Statuses.InterruptDisable, Bit.One);

        // Read BRK/IRQ vector 
        var low = readByte(MachineConstants.ProcessorSetup.IrqHandlerLowByte);
        var high = readByte(MachineConstants.ProcessorSetup.IrqHandlerHighByte);
        ProgramCounter = (ushort)((high << 8) | low);

        return null;
    }

    public ushort GetStackAddress()
    {
        var address = MachineConstants.ProcessorSetup.StackBase + StackPointer;

        return (ushort)address;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(byte value)
    {
        writeByte(GetStackAddress(), value);
        StackPointer--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Pop()
    {
        StackPointer++;
        var value = readByte(GetStackAddress());

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte PeekStack(byte stackPointer)
    {
        var value = readByte((ushort)(MachineConstants.ProcessorSetup.StackBase + stackPointer));

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessCMP(Instruction instruction)
    {
        ProgramCounter++;

        byte memoryValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.CMPImmediate:
                memoryValue = readByte(ProgramCounter++);
                break;

            case (byte)OpCodeValues.CMPZeroPage:
                var zeroPageAddress = readByte(ProgramCounter++);
                memoryValue = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.CMPZeroPageX:
                var zeroPageAddressX = (byte)(readByte(ProgramCounter++) + IX);
                memoryValue = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.CMPAbsolute:
                var absoluteAddress = (ushort)(readByte(ProgramCounter++) +
                                               readByte(ProgramCounter++) *
                                               MachineConstants.ProcessorSetup.MsbMultiplier);
                memoryValue = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.CMPAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(ProgramCounter++) +
                                                readByte(ProgramCounter++) *
                                                MachineConstants.ProcessorSetup.MsbMultiplier + IX);
                memoryValue = readByte(absoluteAddressX);
                break;

            case (byte)OpCodeValues.CMPAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(ProgramCounter++) +
                                                readByte(ProgramCounter++) *
                                                MachineConstants.ProcessorSetup.MsbMultiplier + IY);
                memoryValue = readByte(absoluteAddressY);
                break;

            case (byte)OpCodeValues.CMPIndexedIndirect:
                var zeroPageIndexX =
                    (ushort)((readByte(ProgramCounter) + IX) % MachineConstants.ProcessorSetup.MsbMultiplier);
                var zeroPageLsbX = readByte(zeroPageIndexX);
                var zeroPageMsbX =
                    readByte((ushort)((zeroPageIndexX + 1) % MachineConstants.ProcessorSetup.MsbMultiplier));
                var zeroPageAddressIndexedX =
                    (ushort)(zeroPageLsbX + zeroPageMsbX * MachineConstants.ProcessorSetup.MsbMultiplier);

                memoryValue = readByte(zeroPageAddressIndexedX);
                ProgramCounter++;
                break;

            case (byte)OpCodeValues.CMPIndirectIndexed:
                var zeroPageIndexY = readByte(ProgramCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY =
                    readByte((ushort)((zeroPageIndexY + 1) % MachineConstants.ProcessorSetup.MsbMultiplier));

                var zeroPageAddressIndexY =
                    (ushort)(zeroPageLsbY + zeroPageMsbY * MachineConstants.ProcessorSetup.MsbMultiplier + IY);
                memoryValue = readByte(zeroPageAddressIndexY);
                ProgramCounter++;
                break;
        }

        var result = (byte)(Accumulator - memoryValue);

        // Carry is set if A ≥ M
        var carry = Accumulator >= memoryValue;

        // Zero is set if A == M
        var zero = Accumulator == memoryValue;

        // Negative is just bit 7 of the subtraction result
        var negative = (result & 0x80) != 0;

        // Now pack into your Bit enum
        Status = Status
            .SetBit((Byte)Statuses.Carry, carry ? Bit.One : Bit.Zero)
            .SetBit((Byte)Statuses.Zero, zero ? Bit.One : Bit.Zero)
            .SetBit((Byte)Statuses.Negative, negative ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessCPX(Instruction instruction)
    {
        ProgramCounter++;

        byte memoryValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.CPXImmediate:
                memoryValue = readByte(ProgramCounter++);
                break;

            case (byte)OpCodeValues.CPXZeroPage:
                var zeroPageAddress = readByte(ProgramCounter++);
                memoryValue = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.CPXAbsolute:
                var absoluteAddress = (ushort)(readByte(ProgramCounter++) +
                                               readByte(ProgramCounter++) *
                                               MachineConstants.ProcessorSetup.MsbMultiplier);
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

        Status = Status.SetBit((Byte)Statuses.Negative, negativeFlag);
        Status = Status.SetBit((Byte)Statuses.Zero, zeroFlag);
        Status = Status.SetBit((Byte)Statuses.Carry, carryFlag);

        if (zeroFlag == Bit.Zero)
        {
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessCPY(Instruction instruction)
    {
        ProgramCounter++;

        byte memoryValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.CPYImmediate:
                memoryValue = readByte(ProgramCounter++);
                break;

            case (byte)OpCodeValues.CPYZeroPage:
                var zeroPageAddress = readByte(ProgramCounter++);
                memoryValue = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.CPYAbsolute:
                var absoluteAddress = (ushort)(readByte(ProgramCounter++) +
                                               readByte(ProgramCounter++) *
                                               MachineConstants.ProcessorSetup.MsbMultiplier);
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

        Status = Status.SetBit((Byte)Statuses.Negative, negativeFlag);
        Status = Status.SetBit((Byte)Statuses.Zero, zeroFlag);
        Status = Status.SetBit((Byte)Statuses.Carry, carryFlag);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessORA(Instruction instruction)
    {
        ProgramCounter++;

        byte memoryValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.ORAImmediate:
                memoryValue = readByte(ProgramCounter++);
                break;

            case (byte)OpCodeValues.ORAZeroPage:
            {
                var addr = readByte(ProgramCounter++);
                memoryValue = readByte(addr);
                break;
            }

            case (byte)OpCodeValues.ORAZeroPageX:
            {
                var baseAddr = readByte(ProgramCounter++);
                var addr = (byte)(baseAddr + IX); // wraparound
                memoryValue = readByte(addr);
                break;
            }

            case (byte)OpCodeValues.ORAAbsolute:
            {
                var lo = readByte(ProgramCounter++);
                var hi = readByte(ProgramCounter++);
                var addr = (ushort)(lo + (hi << 8));
                memoryValue = readByte(addr);
                break;
            }

            case (byte)OpCodeValues.ORAAbsoluteX:
            {
                var lo = readByte(ProgramCounter++);
                var hi = readByte(ProgramCounter++);
                var addr = (ushort)(lo + (hi << 8) + IX);
                memoryValue = readByte(addr);
                break;
            }

            case (byte)OpCodeValues.ORAAbsoluteY:
            {
                var lo = readByte(ProgramCounter++);
                var hi = readByte(ProgramCounter++);
                var addr = (ushort)(lo + (hi << 8) + IY);
                memoryValue = readByte(addr);
                break;
            }

            case (byte)OpCodeValues.ORAIndexedIndirect: // (zp,X)
            {
                var baseZp = (byte)(readByte(ProgramCounter++) + IX); // wrap at zero page
                var lo = readByte(baseZp);
                var hi = readByte((byte)(baseZp + 1));
                var addr = (ushort)(lo + (hi << 8));
                memoryValue = readByte(addr);
                break;
            }

            case (byte)OpCodeValues.ORAIndirectIndexed: // (zp),Y
            {
                var zp = readByte(ProgramCounter++);
                var lo = readByte(zp);
                var hi = readByte((byte)(zp + 1));
                var addr = (ushort)(lo + (hi << 8) + IY);
                memoryValue = readByte(addr);
                break;
            }
        }

        Accumulator = (byte)(Accumulator | memoryValue);

        Status = Status.SetBit((Byte)Statuses.Zero, Accumulator.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, Accumulator.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessEOR(Instruction instruction)
    {
        ProgramCounter++;

        byte memoryValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.EORImmediate:
                memoryValue = readByte(ProgramCounter++);
                break;

            case (byte)OpCodeValues.EORZeroPage:
                var zeroPageAddress = readByte(ProgramCounter++);
                memoryValue = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.EORZeroPageX:
                var zeroPageAddressX = (byte)(readByte(ProgramCounter++) + IX);
                memoryValue = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.EORAbsolute:
                var absoluteAddress = (ushort)(readByte(ProgramCounter++) +
                                               readByte(ProgramCounter++) *
                                               MachineConstants.ProcessorSetup.MsbMultiplier);
                memoryValue = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.EORAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(ProgramCounter++) +
                                                readByte(ProgramCounter++) *
                                                MachineConstants.ProcessorSetup.MsbMultiplier + IX);
                memoryValue = readByte(absoluteAddressX);
                break;

            case (byte)OpCodeValues.EORAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(ProgramCounter++) +
                                                readByte(ProgramCounter++) *
                                                MachineConstants.ProcessorSetup.MsbMultiplier + IY);
                memoryValue = readByte(absoluteAddressY);
                break;

            case (byte)OpCodeValues.EORIndexedIndirect:
                var zeroPageIndexX =
                    (ushort)((readByte(ProgramCounter) + IX) % MachineConstants.ProcessorSetup.MsbMultiplier);
                var zeroPageLsbX = readByte(zeroPageIndexX);
                var zeroPageMsbX = readByte((ushort)(zeroPageIndexX + 1)) %
                                   MachineConstants.ProcessorSetup.MsbMultiplier;
                var zeroPageAddressIndexedX =
                    (ushort)(zeroPageLsbX + zeroPageMsbX * MachineConstants.ProcessorSetup.MsbMultiplier);

                memoryValue = readByte(zeroPageAddressIndexedX);
                ProgramCounter++;
                break;

            case (byte)OpCodeValues.EORIndirectIndexed:
                var zeroPageIndexY = readByte(ProgramCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY = readByte((ushort)(zeroPageIndexY + 1)) %
                                   MachineConstants.ProcessorSetup.MsbMultiplier;

                var zeroPageAddressIndexY =
                    (ushort)(zeroPageLsbY + zeroPageMsbY * MachineConstants.ProcessorSetup.MsbMultiplier + IY);
                memoryValue = readByte(zeroPageAddressIndexY);
                ProgramCounter++;
                break;
        }

        Accumulator = (byte)(memoryValue ^ Accumulator);

        Status = Status.SetBit((Byte)Statuses.Zero, Accumulator.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, Accumulator.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessAND(Instruction instruction)
    {
        ProgramCounter++;

        byte memoryValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.ANDImmediate:
                memoryValue = readByte(ProgramCounter++);
                break;

            case (byte)OpCodeValues.ANDZeroPage:
                var zeroPageAddress = readByte(ProgramCounter++);
                memoryValue = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.ANDZeroPageX:
                var zeroPageAddressX = (byte)(readByte(ProgramCounter++) + IX);
                memoryValue = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.ANDAbsolute:
                var absoluteAddress = (ushort)(readByte(ProgramCounter++) +
                                               readByte(ProgramCounter++) *
                                               MachineConstants.ProcessorSetup.MsbMultiplier);
                memoryValue = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.ANDAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(ProgramCounter++) +
                                                readByte(ProgramCounter++) *
                                                MachineConstants.ProcessorSetup.MsbMultiplier + IX);
                memoryValue = readByte(absoluteAddressX);
                break;

            case (byte)OpCodeValues.ANDAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(ProgramCounter++) +
                                                readByte(ProgramCounter++) *
                                                MachineConstants.ProcessorSetup.MsbMultiplier + IY);
                memoryValue = readByte(absoluteAddressY);
                break;

            case (byte)OpCodeValues.ANDIndexedIndirect:
                var zeroPageIndexX =
                    (ushort)((readByte(ProgramCounter) + IX) % MachineConstants.ProcessorSetup.MsbMultiplier);
                var zeroPageLsbX = readByte(zeroPageIndexX);
                var zeroPageMsbX =
                    readByte((ushort)((zeroPageIndexX + 1) % MachineConstants.ProcessorSetup.MsbMultiplier));
                var zeroPageAddressIndexedX =
                    (ushort)(zeroPageLsbX + zeroPageMsbX * MachineConstants.ProcessorSetup.MsbMultiplier);

                memoryValue = readByte(zeroPageAddressIndexedX);
                ProgramCounter++;
                break;

            case (byte)OpCodeValues.ANDIndirectIndexed:
                var zeroPageIndexY = readByte(ProgramCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY =
                    readByte((ushort)((zeroPageIndexY + 1) % MachineConstants.ProcessorSetup.MsbMultiplier));

                var zeroPageAddressIndexY =
                    zeroPageLsbY + zeroPageMsbY * MachineConstants.ProcessorSetup.MsbMultiplier + IY;
                memoryValue = readByte((ushort)zeroPageAddressIndexY);
                ProgramCounter++;
                break;
        }

        Accumulator = (byte)(memoryValue & Accumulator);

        Status = Status.SetBit((Byte)Statuses.Zero, Accumulator.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, Accumulator.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessINY()
    {
        ProgramCounter++;

        IY++;

        Status = Status.SetBit((Byte)Statuses.Zero, IY.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, IY.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessDEY()
    {
        ProgramCounter++;

        IY--;

        Status = Status.SetBit((Byte)Statuses.Zero, IY.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, IY.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessINX()
    {
        ProgramCounter++;

        IX++;

        Status = Status.SetBit((Byte)Statuses.Zero, IX.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, IX.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    private int? ProcessDEX()
    {
        ProgramCounter++;

        IX--;

        Status = Status.SetBit((Byte)Statuses.Zero, IX.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, IX.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessDEC(Instruction instruction)
    {
        ProgramCounter++;

        ushort memoryLocation = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.DECZeroPage:
                memoryLocation = readByte(ProgramCounter++);
                break;

            case (byte)OpCodeValues.DECZeroPageX:
                memoryLocation = (byte)(readByte(ProgramCounter++) + IX);
                break;

            case (byte)OpCodeValues.DECAbsolute:
                memoryLocation = (ushort)(readByte(ProgramCounter++) +
                                          readByte(ProgramCounter++) * MachineConstants.ProcessorSetup.MsbMultiplier);
                break;

            case (byte)OpCodeValues.DECAbsoluteX:
                memoryLocation = (ushort)(readByte(ProgramCounter++) +
                                          readByte(ProgramCounter++) * MachineConstants.ProcessorSetup.MsbMultiplier +
                                          IX);
                break;
        }

        writeByte(memoryLocation, (byte)(readByte(memoryLocation) - 1));

        Status = Status.SetBit((Byte)Statuses.Zero, readByte(memoryLocation).IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, readByte(memoryLocation).IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessINC(Instruction instruction)
    {
        ProgramCounter++;

        ushort memoryLocation = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.INCZeroPage:
                memoryLocation = readByte(ProgramCounter++);
                break;

            case (byte)OpCodeValues.INCZeroPageX:
                memoryLocation = (byte)(readByte(ProgramCounter++) + IX);
                break;

            case (byte)OpCodeValues.INCAbsolute:
                memoryLocation = (ushort)(readByte(ProgramCounter++) +
                                          readByte(ProgramCounter++) * MachineConstants.ProcessorSetup.MsbMultiplier);
                break;

            case (byte)OpCodeValues.INCAbsoluteX:
                memoryLocation = (ushort)(readByte(ProgramCounter++) +
                                          readByte(ProgramCounter++) * MachineConstants.ProcessorSetup.MsbMultiplier +
                                          IX);
                break;
        }

        writeByte(memoryLocation, (byte)(readByte(memoryLocation) + 1));

        Status = Status.SetBit((Byte)Statuses.Zero, readByte(memoryLocation).IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, readByte(memoryLocation).IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessASL(Instruction instruction)
    {
        ProgramCounter++;

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
                var address = readByte(ProgramCounter++);
                var value = readByte(address);
                msb = value.GetBit(Byte.Seven);
                value = (byte)(value << 1);
                writeByte(address, value);
                finalValue = value;
                break;
            }

            case (byte)OpCodeValues.ASLZeroPageX:
            {
                var address = (byte)(readByte(ProgramCounter++) + IX);
                var value = readByte(address);
                msb = value.GetBit(Byte.Seven);
                value = (byte)(value << 1);
                writeByte(address, value);
                finalValue = value;
                break;
            }

            case (byte)OpCodeValues.ASLAbsolute:
            {
                var low = readByte(ProgramCounter++);
                var high = readByte(ProgramCounter++);
                var address = (ushort)(low + high * MachineConstants.ProcessorSetup.MsbMultiplier);
                var value = readByte(address);
                msb = value.GetBit(Byte.Seven);
                value = (byte)(value << 1);
                writeByte(address, value);
                finalValue = value;
                break;
            }

            case (byte)OpCodeValues.ASLAbsoluteX:
            {
                var low = readByte(ProgramCounter++);
                var high = readByte(ProgramCounter++);
                var address = (ushort)(low + high * MachineConstants.ProcessorSetup.MsbMultiplier + IX);
                var value = readByte(address);
                msb = value.GetBit(Byte.Seven);
                value = (byte)(value << 1);
                writeByte(address, value);
                finalValue = value;
                break;
            }
        }


        Status = Status.SetBit((Byte)Statuses.Carry, msb);
        Status = Status.SetBit((Byte)Statuses.Zero, finalValue.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, finalValue.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessROR(Instruction instruction)
    {
        ProgramCounter++;

        byte finalValue = 0;
        byte value;
        var carryOut = Bit.Zero;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.RORAccumulator:
            {
                value = Accumulator;
                carryOut = value.GetBit(Byte.Zero);
                var carryIn = Status.GetBit((byte)Statuses.Carry);
                value = (byte)(value >> 1);
                value = value.SetBit(Byte.Seven, carryIn);
                Accumulator = value;
                finalValue = value;
                break;
            }

            case (byte)OpCodeValues.RORZeroPage:
            {
                var address = readByte(ProgramCounter++);
                value = readByte(address);
                carryOut = value.GetBit(Byte.Zero);
                var carryIn = Status.GetBit((byte)Statuses.Carry);
                value = (byte)(value >> 1);
                value = value.SetBit(Byte.Seven, carryIn);
                writeByte(address, value);
                finalValue = value;
                break;
            }

            case (byte)OpCodeValues.RORZeroPageX:
            {
                var address = (byte)(readByte(ProgramCounter++) + IX);
                value = readByte(address);
                carryOut = value.GetBit(Byte.Zero);
                var carryIn = Status.GetBit((byte)Statuses.Carry);
                value = (byte)(value >> 1);
                value = value.SetBit(Byte.Seven, carryIn);
                writeByte(address, value);
                finalValue = value;
                break;
            }

            case (byte)OpCodeValues.RORAbsolute:
            {
                var low = readByte(ProgramCounter++);
                var high = readByte(ProgramCounter++);
                var address = (ushort)(low + high * MachineConstants.ProcessorSetup.MsbMultiplier);
                value = readByte(address);
                carryOut = value.GetBit(Byte.Zero);
                var carryIn = Status.GetBit((byte)Statuses.Carry);
                value = (byte)(value >> 1);
                value = value.SetBit(Byte.Seven, carryIn);
                writeByte(address, value);
                finalValue = value;
                break;
            }

            case (byte)OpCodeValues.RORAbsoluteX:
            {
                var low = readByte(ProgramCounter++);
                var high = readByte(ProgramCounter++);
                var address = (ushort)(low + high * MachineConstants.ProcessorSetup.MsbMultiplier + IX);
                value = readByte(address);
                carryOut = value.GetBit(Byte.Zero);
                var carryIn = Status.GetBit((byte)Statuses.Carry);
                value = (byte)(value >> 1);
                value = value.SetBit(Byte.Seven, carryIn);
                writeByte(address, value);
                finalValue = value;
                break;
            }
        }

        Status = Status.SetBit((Byte)Statuses.Carry, carryOut);
        Status = Status.SetBit((Byte)Statuses.Zero, finalValue.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, finalValue.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessROL(Instruction instruction)
    {
        ProgramCounter++;

        var carryOut = Bit.Zero;
        byte finalValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.ROLAccumulator:
            {
                var value = Accumulator;
                carryOut = value.GetBit(Byte.Seven);
                var carryIn = Status.GetBit((byte)Statuses.Carry);
                value = (byte)((value << 1) | (byte)carryIn);
                Accumulator = value;
                finalValue = value;
                break;
            }

            case (byte)OpCodeValues.ROLZeroPage:
            {
                var address = readByte(ProgramCounter++);
                var value = readByte(address);
                carryOut = value.GetBit(Byte.Seven);
                var carryIn = Status.GetBit((byte)Statuses.Carry);
                finalValue = (byte)((value << 1) | (byte)carryIn);
                writeByte(address, finalValue);
                break;
            }

            case (byte)OpCodeValues.ROLZeroPageX:
            {
                var address = (byte)(readByte(ProgramCounter++) + IX);
                var value = readByte(address);
                carryOut = value.GetBit(Byte.Seven);
                var carryIn = Status.GetBit((byte)Statuses.Carry);
                finalValue = (byte)((value << 1) | (byte)carryIn);
                writeByte(address, finalValue);
                break;
            }

            case (byte)OpCodeValues.ROLAbsolute:
            {
                var low = readByte(ProgramCounter++);
                var high = readByte(ProgramCounter++);
                var address = (ushort)(low + high * MachineConstants.ProcessorSetup.MsbMultiplier);
                var value = readByte(address);
                carryOut = value.GetBit(Byte.Seven);
                var carryIn = Status.GetBit((byte)Statuses.Carry);
                finalValue = (byte)((value << 1) | (byte)carryIn);
                writeByte(address, finalValue);
                break;
            }

            case (byte)OpCodeValues.ROLAbsoluteX:
            {
                var low = readByte(ProgramCounter++);
                var high = readByte(ProgramCounter++);
                var address = (ushort)(low + high * MachineConstants.ProcessorSetup.MsbMultiplier + IX);
                var value = readByte(address);
                carryOut = value.GetBit(Byte.Seven);
                var carryIn = Status.GetBit((byte)Statuses.Carry);
                finalValue = (byte)((value << 1) | (byte)carryIn);
                writeByte(address, finalValue);
                break;
            }
        }

        // Update flags
        Status = Status.SetBit((Byte)Statuses.Carry, carryOut);
        Status = Status.SetBit((Byte)Statuses.Zero, finalValue.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, finalValue.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessLSR(Instruction instruction)
    {
        ProgramCounter++;

        var lsb = Bit.Zero;
        byte finalValue = 0;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.LSRAccumulator:
                lsb = Accumulator.GetBit(0);
                Accumulator = (byte)(Accumulator >> 1);
                finalValue = Accumulator;
                break;

            case (byte)OpCodeValues.LSRZeroPage:
            {
                var zeroPageAddress = readByte(ProgramCounter++);
                var value = readByte(zeroPageAddress);
                lsb = value.GetBit(0);
                finalValue = (byte)(value >> 1);
                writeByte(zeroPageAddress, finalValue);
                break;
            }

            case (byte)OpCodeValues.LSRZeroPageX:
            {
                var zeroPageAddressX = (byte)(readByte(ProgramCounter++) + IX);
                var value = readByte(zeroPageAddressX);
                lsb = value.GetBit(0);
                finalValue = (byte)(value >> 1);
                writeByte(zeroPageAddressX, finalValue);
                break;
            }

            case (byte)OpCodeValues.LSRAbsolute:
            {
                var low = readByte(ProgramCounter);
                var high = readByte((ushort)(ProgramCounter + 1));
                var absoluteAddress = (ushort)(low + high * MachineConstants.ProcessorSetup.MsbMultiplier);
                var value = readByte(absoluteAddress);
                lsb = value.GetBit(0);
                finalValue = (byte)(value >> 1);
                writeByte(absoluteAddress, finalValue);
                ProgramCounter += 2;
                break;
            }

            case (byte)OpCodeValues.LSRAbsoluteX:
            {
                var low = readByte(ProgramCounter);
                var high = readByte((ushort)(ProgramCounter + 1));
                var absoluteAddressX = (ushort)(low + high * MachineConstants.ProcessorSetup.MsbMultiplier + IX);
                var value = readByte(absoluteAddressX);
                lsb = value.GetBit(0);
                finalValue = (byte)(value >> 1);
                writeByte(absoluteAddressX, finalValue);
                ProgramCounter += 2;
                break;
            }
        }

        Status = Status.SetBit((Byte)Statuses.Carry, lsb);
        Status = Status.SetBit((Byte)Statuses.Zero, finalValue.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, finalValue.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessNOP()
    {
        ProgramCounter++;
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessPHA()
    {
        ProgramCounter++;

        Push(Accumulator);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessPHP()
    {
        ProgramCounter++;

        Push(Status);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessPLA()
    {
        ProgramCounter++;

        Accumulator = Pop();

        Status = Status.SetBit((Byte)Statuses.Zero, Accumulator.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, Accumulator.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessPLP()
    {
        ProgramCounter++;

        Status = Pop();

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessJSR()
    {
        ProgramCounter++;

        var absoluteAddress =
            (ushort)(readByte(ProgramCounter++) +
                     readByte(ProgramCounter++) * MachineConstants.ProcessorSetup.MsbMultiplier);

        var addressToPush = (ushort)(ProgramCounter - 1);

        Push(addressToPush.HighByte());
        Push(addressToPush.LowByte());
        ProgramCounter = absoluteAddress;

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessRTS()
    {
        // 1) pull low byte
        var low = Pop();
        // 2) pull high byte
        var high = Pop();

        // 3) reassemble and add 1
        var addr = (ushort)((high << 8) | low);
        ProgramCounter = (ushort)(addr + 1);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessLDA(Instruction instruction)
    {
        ProgramCounter++;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.LDAImmediate:
                Accumulator = readByte(ProgramCounter++);
                break;

            case (byte)OpCodeValues.LDAZeroPage:
                var zeroPageAddress = readByte(ProgramCounter++);
                Accumulator = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.LDAZeroPageX:
                var zeroPageAddressX = (byte)(readByte(ProgramCounter++) + IX);
                Accumulator = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.LDAAbsolute:
                var absoluteAddress = readByte(ProgramCounter++) +
                                      readByte(ProgramCounter++) * MachineConstants.ProcessorSetup.MsbMultiplier;
                Accumulator = readByte((ushort)absoluteAddress);
                break;

            case (byte)OpCodeValues.LDAAbsoluteX:
                var absoluteAddressX = readByte(ProgramCounter++) +
                                       readByte(ProgramCounter++) * MachineConstants.ProcessorSetup.MsbMultiplier +
                                       IX;
                Accumulator = readByte((ushort)absoluteAddressX);
                break;

            case (byte)OpCodeValues.LDAAbsoluteY:
                var absoluteAddressY = readByte(ProgramCounter++) +
                                       readByte(ProgramCounter++) * MachineConstants.ProcessorSetup.MsbMultiplier +
                                       IY;
                Accumulator = readByte((ushort)absoluteAddressY);
                break;

            case (byte)OpCodeValues.LDAIndexedIndirect:
                var zeroPageIndexX = (ushort)(readByte(ProgramCounter) + IX) %
                                     MachineConstants.ProcessorSetup.MsbMultiplier;
                var zeroPageLsbX = readByte((ushort)zeroPageIndexX);
                var zeroPageMsbX =
                    readByte((ushort)((zeroPageIndexX + 1) % MachineConstants.ProcessorSetup.MsbMultiplier));
                var zeroPageAddressIndexedX =
                    zeroPageLsbX + zeroPageMsbX * MachineConstants.ProcessorSetup.MsbMultiplier;

                Accumulator = readByte((ushort)zeroPageAddressIndexedX);
                ProgramCounter++;
                break;

            case (byte)OpCodeValues.LDAIndirectIndexed:
                var zeroPageIndexY = readByte(ProgramCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY =
                    readByte((ushort)((zeroPageIndexY + 1) % MachineConstants.ProcessorSetup.MsbMultiplier));

                var zeroPageAddressIndexY =
                    zeroPageLsbY + zeroPageMsbY * MachineConstants.ProcessorSetup.MsbMultiplier + IY;
                Accumulator = readByte((ushort)zeroPageAddressIndexY);
                ProgramCounter++;
                break;
        }

        Status = Status.SetBit((Byte)Statuses.Zero, Accumulator.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, Accumulator.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessSTA(Instruction instruction)
    {
        ProgramCounter++;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.STAZeroPage:
                var zeroPageAddress = readByte(ProgramCounter++);
                writeByte(zeroPageAddress, Accumulator);
                break;

            case (byte)OpCodeValues.STAZeroPageX:
                var zeroPageAddressX = (byte)(readByte(ProgramCounter++) + IX);
                writeByte(zeroPageAddressX, Accumulator);
                break;

            case (byte)OpCodeValues.STAAbsolute:
                var absoluteAddress = (ushort)(readByte(ProgramCounter++) +
                                               readByte(ProgramCounter++) *
                                               MachineConstants.ProcessorSetup.MsbMultiplier);
                writeByte(absoluteAddress, Accumulator);
                break;

            case (byte)OpCodeValues.STAAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(ProgramCounter++) +
                                                readByte(ProgramCounter++) *
                                                MachineConstants.ProcessorSetup.MsbMultiplier + IX);
                writeByte(absoluteAddressX, Accumulator);
                break;

            case (byte)OpCodeValues.STAAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(ProgramCounter++) +
                                                readByte(ProgramCounter++) *
                                                MachineConstants.ProcessorSetup.MsbMultiplier + IY);
                writeByte(absoluteAddressY, Accumulator);
                break;

            case (byte)OpCodeValues.STAIndexedIndirect:
                var zeroPageIndexX =
                    (ushort)((readByte(ProgramCounter) + IX) % MachineConstants.ProcessorSetup.MsbMultiplier);
                var zeroPageLsbX = readByte(zeroPageIndexX);
                var zeroPageMsbX =
                    readByte((ushort)((zeroPageIndexX + 1) % MachineConstants.ProcessorSetup.MsbMultiplier));
                var zeroPageAddressIndexedX =
                    (ushort)(zeroPageLsbX + zeroPageMsbX * MachineConstants.ProcessorSetup.MsbMultiplier);
                writeByte(zeroPageAddressIndexedX, Accumulator);
                ProgramCounter++;
                break;

            case (byte)OpCodeValues.STAIndirectIndexed:
                var zeroPageIndexY = readByte(ProgramCounter);
                var zeroPageLsbY = readByte(zeroPageIndexY);
                var zeroPageMsbY =
                    readByte((ushort)((zeroPageIndexY + 1) % MachineConstants.ProcessorSetup.MsbMultiplier));
                var zeroPageAddressIndexY =
                    (ushort)(zeroPageLsbY + zeroPageMsbY * MachineConstants.ProcessorSetup.MsbMultiplier + IY);
                writeByte(zeroPageAddressIndexY, Accumulator);
                ProgramCounter++;
                break;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessTAX()
    {
        ProgramCounter++;

        IX = Accumulator;

        Status = Status.SetBit((Byte)Statuses.Zero, IX.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, IX.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessTAY()
    {
        ProgramCounter++;

        IY = Accumulator;

        Status = Status.SetBit((Byte)Statuses.Zero, IY.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, IY.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessTSX()
    {
        ProgramCounter++;

        IX = StackPointer;

        Status = Status.SetBit((Byte)Statuses.Zero, IX.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, IX.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessTYA()
    {
        ProgramCounter++;

        Accumulator = IY;

        Status = Status.SetBit((Byte)Statuses.Zero, Accumulator.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, Accumulator.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessTXA()
    {
        ProgramCounter++;

        Accumulator = IX;

        Status = Status.SetBit((Byte)Statuses.Zero, Accumulator.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, Accumulator.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessTXS()
    {
        ProgramCounter++;

        StackPointer = IX;

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessSTX(Instruction instruction)
    {
        ProgramCounter++;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.STXZeroPage:
                var zeroPageAddress = readByte(ProgramCounter++);
                writeByte(zeroPageAddress, IX);
                break;

            case (byte)OpCodeValues.STXZeroPageY:
                var zeroPageAddressX =
                    (ushort)(readByte(ProgramCounter++) + IY % MachineConstants.ProcessorSetup.MsbMultiplier);
                writeByte(zeroPageAddressX, IX);
                break;

            case (byte)OpCodeValues.STXAbsolute:
                var absoluteAddress = (ushort)(readByte(ProgramCounter++) +
                                               readByte(ProgramCounter++) *
                                               MachineConstants.ProcessorSetup.MsbMultiplier);
                writeByte(absoluteAddress, IX);
                break;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessSTY(Instruction instruction)
    {
        ProgramCounter++;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.STYZeroPage:
                var zeroPageAddress = readByte(ProgramCounter++);
                writeByte(zeroPageAddress, IY);
                break;

            case (byte)OpCodeValues.STYZeroPageX:
                var zeroPageAddressX = (byte)(readByte(ProgramCounter++) + IX);
                writeByte(zeroPageAddressX, IY);
                break;

            case (byte)OpCodeValues.STYAbsolute:
                var absoluteAddress = (ushort)(readByte(ProgramCounter++) +
                                               readByte(ProgramCounter++) *
                                               MachineConstants.ProcessorSetup.MsbMultiplier);
                writeByte(absoluteAddress, IY);
                break;
        }

        return null;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessLDY(Instruction instruction)
    {
        ProgramCounter++;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.LDYImmediate:
                IY = readByte(ProgramCounter++);
                break;

            case (byte)OpCodeValues.LDYZeroPage:
                var zeroPageAddress = readByte(ProgramCounter++);
                IY = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.LDYZeroPageX:
                var zeroPageAddressX = (byte)(readByte(ProgramCounter++) + IX);
                IY = readByte(zeroPageAddressX);
                break;

            case (byte)OpCodeValues.LDYAbsolute:
                var absoluteAddress = (ushort)(readByte(ProgramCounter++) +
                                               readByte(ProgramCounter++) *
                                               MachineConstants.ProcessorSetup.MsbMultiplier);
                IY = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.LDYAbsoluteX:
                var absoluteAddressX = (ushort)(readByte(ProgramCounter++) +
                                                readByte(ProgramCounter++) *
                                                MachineConstants.ProcessorSetup.MsbMultiplier + IX);
                IY = readByte(absoluteAddressX);
                break;
        }

        Status = Status.SetBit((Byte)Statuses.Zero, IY.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, IY.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessLDX(Instruction instruction)
    {
        ProgramCounter++;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.LDXImmediate:
                IX = readByte(ProgramCounter++);
                break;

            case (byte)OpCodeValues.LDXZeroPage:
                var zeroPageAddress = readByte(ProgramCounter++);
                IX = readByte(zeroPageAddress);
                break;

            case (byte)OpCodeValues.LDXZeroPageY:
                var zeroPageAddressY =
                    (ushort)(readByte(ProgramCounter++) + IY % MachineConstants.ProcessorSetup.MsbMultiplier);
                IX = readByte(zeroPageAddressY);
                break;

            case (byte)OpCodeValues.LDXAbsolute:
                var absoluteAddress = (ushort)(readByte(ProgramCounter++) +
                                               readByte(ProgramCounter++) *
                                               MachineConstants.ProcessorSetup.MsbMultiplier);
                IX = readByte(absoluteAddress);
                break;

            case (byte)OpCodeValues.LDXAbsoluteY:
                var absoluteAddressY = (ushort)(readByte(ProgramCounter++) +
                                                readByte(ProgramCounter++) *
                                                MachineConstants.ProcessorSetup.MsbMultiplier + IY);
                IX = readByte(absoluteAddressY);
                break;
        }

        Status = Status.SetBit((Byte)Statuses.Zero, IX.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Negative, IX.IsNegative() ? Bit.One : Bit.Zero);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int? ProcessBIT(Instruction instruction)
    {
        ProgramCounter++;

        ushort address;

        switch (instruction.Code)
        {
            case (byte)OpCodeValues.BITZeroPage:
                address = readByte(ProgramCounter++);
                break;

            case (byte)OpCodeValues.BITAbsolute:
                address = (ushort)(readByte(ProgramCounter++) +
                                   readByte(ProgramCounter++) * MachineConstants.ProcessorSetup.MsbMultiplier);
                break;

            default:
                throw new OpCodeNotFoundException($"OpCode {instruction.Code} does not exist");
        }

        var memoryValue = readByte(address);

        var value = (byte)(memoryValue & Accumulator);

        Status = Status.SetBit((Byte)Statuses.Zero, value.IsZero() ? Bit.One : Bit.Zero);
        Status = Status.SetBit((Byte)Statuses.Overflow, memoryValue.GetBit(Byte.Six));
        Status = Status.SetBit((Byte)Statuses.Negative, memoryValue.GetBit(Byte.Seven));

        return null;
    }

    #endregion
}