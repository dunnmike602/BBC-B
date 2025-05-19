namespace MLDComputing.Emulators.BBCSim._6502.Engine;

public enum OpCodeValues : byte
{
    ADCImmediate = 0x69,
    ADCZeroPage = 0x65,
    ADCZeroPageX = 0x75,
    ADCAbsolute = 0x6D,
    ADCAbsoluteX = 0x7D,
    ADCAbsoluteY = 0x79,
    ADCIndexedIndirect = 0x61,
    ADCIndirectIndexed = 0x71,

    ANDImmediate = 0x29,
    ANDZeroPage = 0x25,
    ANDZeroPageX = 0x35,
    ANDAbsolute = 0x2D,
    ANDAbsoluteX = 0x3D,
    ANDAbsoluteY = 0x39,
    ANDIndexedIndirect = 0x21,
    ANDIndirectIndexed = 0x31,

    ASLAccumulator = 0x0A,
    ASLZeroPage = 0x06,
    ASLZeroPageX = 0x16,
    ASLAbsolute = 0x0E,
    ASLAbsoluteX = 0x1E,

    LDAImmediate = 0xA9,
    LDAZeroPage = 0xA5,
    LDAZeroPageX = 0xB5,
    LDAAbsolute = 0xAD,
    LDAAbsoluteX = 0xBD,
    LDAAbsoluteY = 0xB9,
    LDAIndexedIndirect = 0xA1,
    LDAIndirectIndexed = 0xB1,

    BITZeroPage = 0x24,
    BITAbsolute = 0x2C,

    CMPImmediate = 0xC9,
    CMPZeroPage = 0xC5,
    CMPZeroPageX = 0xD5,
    CMPAbsolute = 0xCD,
    CMPAbsoluteX = 0xDD,
    CMPAbsoluteY = 0xD9,
    CMPIndexedIndirect = 0xC1,
    CMPIndirectIndexed = 0xD1,

    CPXImmediate = 0xE0,
    CPXZeroPage = 0xE4,
    CPXAbsolute = 0xEC,

    CPYImmediate = 0xC0,
    CPYZeroPage = 0xC4,
    CPYAbsolute = 0xCC,

    DECZeroPage = 0xC6,
    DECZeroPageX = 0xD6,
    DECAbsolute = 0xCE,
    DECAbsoluteX = 0xDE,

    DEXImplied = 0xCA,

    DEYImplied = 0x88,

    EORImmediate = 0x49,
    EORZeroPage = 0x45,
    EORZeroPageX = 0x55,
    EORAbsolute = 0x4D,
    EORAbsoluteX = 0x5D,
    EORAbsoluteY = 0x59,
    EORIndexedIndirect = 0x41,
    EORIndirectIndexed = 0x51,

    INCZeroPage = 0xE6,
    INCZeroPageX = 0xF6,
    INCAbsolute = 0xEE,
    INCAbsoluteX = 0xFE,

    INXImplied = 0xE8,

    INYImplied = 0xC8,

    JMPAbsolute = 0x4C,
    JMPIndirect = 0x6C,

    JSRAbsolute = 0x20,

    LDXImmediate = 0xA2,
    LDXZeroPage = 0xA6,
    LDXZeroPageY = 0xB6,
    LDXAbsolute = 0xAE,
    LDXAbsoluteY = 0xBE,

    LDYImmediate = 0xA0,
    LDYZeroPage = 0xA4,
    LDYZeroPageX = 0xB4,
    LDYAbsolute = 0xAC,
    LDYAbsoluteX = 0xBC,

    LSRAccumulator = 0x4A,
    LSRZeroPage = 0x46,
    LSRZeroPageX = 0x56,
    LSRAbsolute = 0x4E,
    LSRAbsoluteX = 0x5E,

    NOPImplied = 0xEA,

    ORAImmediate = 0x09,
    ORAZeroPage = 0x05,
    ORAZeroPageX = 0x15,
    ORAAbsolute = 0x0D,
    ORAAbsoluteX = 0x1D,
    ORAAbsoluteY = 0x19,
    ORAIndexedIndirect = 0x01,
    ORAIndirectIndexed = 0x11,

    PHAImplied = 0x48,

    PHPImplied = 0x08,

    PLAImplied = 0x68,

    PLPImplied = 0x28,

    ROLAccumulator = 0x2A,
    ROLZeroPage = 0x26,
    ROLZeroPageX = 0x36,
    ROLAbsolute = 0x2E,
    ROLAbsoluteX = 0x3E,

    RORAccumulator = 0x6A,
    RORZeroPage = 0x66,
    RORZeroPageX = 0x76,
    RORAbsolute = 0x6E,
    RORAbsoluteX = 0x7E,

    RTIImplied = 0x40,

    SBCImmediate = 0xE9,
    SBCZeroPage = 0xE5,
    SBCZeroPageX = 0xF5,
    SBCAbsolute = 0xED,
    SBCAbsoluteX = 0xFD,
    SBCAbsoluteY = 0xF9,
    SBCIndexedIndirect = 0xE1,
    SBCIndirectIndexed = 0xF1,

    STAZeroPage = 0x85,
    STAZeroPageX = 0x95,
    STAAbsolute = 0x8D,
    STAAbsoluteX = 0x9D,
    STAAbsoluteY = 0x99,
    STAIndexedIndirect = 0x81,
    STAIndirectIndexed = 0x91,

    SECImplied = 0x38,

    SEDImplied = 0xF8,

    SEIImplied = 0x78,

    STXZeroPage = 0x86,
    STXZeroPageY = 0x96,
    STXAbsolute = 0x8E,

    STYZeroPage = 0x84,
    STYZeroPageX = 0x94,
    STYAbsolute = 0x8C,

    TAXImplied = 0xAA,
    TAYImplied = 0xA8,
    TSXImplied = 0xBA,
    TXAImplied = 0x8A,
    TXSImplied = 0x9A,
    TYAImplied = 0x98,

    KILImplied = 0x42
}