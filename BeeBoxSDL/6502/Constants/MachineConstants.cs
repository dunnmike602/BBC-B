﻿namespace BeeBoxSDL._6502.Constants;

public static class MachineConstants
{
    public static class PcrControl
    {
        public const byte PcrCb2Control = 0xe0;
        public const byte PcrCb1InterruptControl = 0x10;
        public const byte PcrCA2Control = 0x0e;
        public const byte PcrCA1InterruptControl = 0x01;

        public const byte PcrCA2OutputPulse = 0x0a;
        public const byte PcrCA2OutputLow = 0x0c;
        public const byte PcrCA2OutputHigh = 0x0e;

        public const byte PcrCb2OutputPulse = 0xa0;
        public const byte PcrCb2OutputLow = 0xc0;
        public const byte PcrCb2OutputHigh = 0xe0;
    }

    public static class MachineSetup
    {
        public const int BbcProcessorSpeed = 2_000_000;
        public const int BbcFrameRate = 50;
    }

    public static class BitPatterns
    {
        public const byte ClearBit4 = 0b11101111;
    }

    public static class InterruptBits
    {
        public const byte CA2 = 0x01;
        public const byte CA1 = 0x02;
        public const byte Cb2 = 0x04;
        public const byte Cb1 = 0x08;
        public const byte Timer2 = 0x10;
        public const byte Shift = 0x20;
        public const byte Timer1 = 0x40;
        public const byte IrqFlag = 0x80;
    }

    public static class Ic32Constants
    {
        public const byte Ic32SoundWrite = 0x01;
        public const byte Ic32SpeechRead = 0x02; // BBC B only
        public const byte Ic32SpeechWrite = 0x04; // BBC B only
        public const byte Ic32RtcRead = 0x02; // Master only
        public const byte Ic32RtcDataStrobe = 0x04; // Master only
        public const byte Ic32KeyboardWrite = 0x08;
        public const byte Ic32ScreenAddress = 0x30;
        public const byte Ic32CapsLock = 0x40;
        public const byte Ic32ShiftLock = 0x80;
    }

    public static class ProcessorSetup
    {
        public const ushort StackBase = 0x100;
        public const int RelativeAddressBackwardsLimit = -126;
        public const int RelativeAddressForwardsLimit = 129;
        public const int ProgramCounterOffset = 2;
        public const int MsbMultiplier = 256;
        public const ushort ResetVectorLow = 0xFFFC;
        public const ushort ResetVectorHigh = 0xFFFD;
        public const ushort IrqHandlerLowByte = 0xFFFE;
        public const ushort IrqHandlerHighByte = 0xFFFF;
    }

    public static class ViaRegisters
    {
        public const byte PortBRegisterOffset00 = 0x00;
        public const byte PortARegisterOffset01 = 0x01;
        public const byte DataDirectionRegisterBOffset02 = 0x02;
        public const byte DataDirectionRegisterAOffset03 = 0x03;
        public const byte Timer1LowCounterOffset04 = 0x04; // T1C-L
        public const byte Timer1HighCounterOffset05 = 0x05; // T1C-H
        public const byte Timer1LatchLowOffset06 = 0x06;
        public const byte Timer1LatchHighOffset07 = 0x07;
        public const byte AuxiliaryControlRegisterOffset06 = 0x0B; // ACR
        public const byte InterruptFlagRegisterOffset0D = 0x0D;
        public const byte InterruptEnableRegisterOffset0E = 0x0E;
        public const byte PortARegisterNoHandshakeOffset0F = 0x0F;
        public const byte PeripheralControlRegisterOffset0C = 0x0c;
    }
}