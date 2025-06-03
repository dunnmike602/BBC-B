namespace MLDComputing.Emulators.BBCSim._6502.Constants;

public static class ProcessorConstants
{
    public static class MachineSetup
    {
        public const int BBCProcessorSpeed = 2_000_000;
        public const int BBCFrameRate = 50;
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