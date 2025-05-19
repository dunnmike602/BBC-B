namespace BBC_B_Tests;

using FluentAssertions;
using MLDComputing.Emulators.BBCSim._6502.Assembler;
using MLDComputing.Emulators.BBCSim._6502.Assembler.Interfaces;
using MLDComputing.Emulators.BBCSim._6502.Engine;
using MLDComputing.Emulators.BBCSim.Beeb;

[TestClass]
public class AdvancedTests
{
    private Assembler _assembler;

    private Mapper _mapper;

    private MemoryMap? _memoryMap;

    private Cpu6502 _processor;
    private Tokeniser? _tokeniser;

    [TestInitialize]
    public void Init()
    {
        _tokeniser = new Tokeniser();

        _memoryMap = new MemoryMap();

        _mapper = new Mapper();

        _processor = new Cpu6502(_memoryMap!.ReadByte, _memoryMap.WriteByte);
        _processor.Initialise(1_000_000, 50);
        _assembler = new Assembler(_memoryMap.WriteByte);
    }

    [TestMethod]
    public void Can_Set_A_Block_Of_Memory()
    {
        // Arrange 

        var program = @"
        LDA #0
        STA 20
        LDA #10
        STA 21
        LDX #10
        JSR CLRMEM:
        KIL
        CLRMEM:
        LDA#255
        LDY #0
        CLRM1:
        STA(20),Y
        INY
        DEX
        BNE CLRM1:
        RTS
        ";

        var operations = _tokeniser!.Parse(program);

        _mapper.MapAndValidate(operations);

        _assembler.Assemble(operations, 0x1000);

        // Act
        _processor.Run(3000, false);

        // Assert
        _memoryMap.ReadByte(2560).Should().Be(0xFF);
        _memoryMap.ReadByte(2561).Should().Be(0xFF);
        _memoryMap.ReadByte(2562).Should().Be(0xFF);
        _memoryMap.ReadByte(2563).Should().Be(0xFF);
        _memoryMap.ReadByte(2570).Should().Be(0x00);
    }

    [TestMethod]
    public void Standard_Test_1()
    {
        // Arrange
        const string program = @"
            LDA #85
            LDX #42
            LDY #115
            STA $81
            LDA #$01
            STA $61
            LDA #$7E
            LDA $81
            STA $0910
            LDA #$7E
            LDA $0910
            STA $56,X
            LDA #$7E
            LDA $56,X
            STY $60
            STA ($60),Y
            LDA #$7E
            LDA ($60),Y
            STA $07ff,X
            LDA #$7E
            LDA $07ff,X
            STA $07ff,Y
            LDA #$7E
            LDA $07ff,Y
            STA ($36,X)
            LDA #$7E
            LDA ($36,X)
            STX $50
            LDX $60
            LDY $50
            STX $0913
            LDX #$22
            LDX $0913
            STY $0914
            LDY #$99
            LDY $0914
            STY $2D,X
            STX $77,Y
            LDY #$99
            LDY $2D,X
            LDX #$22
            LDX $77,Y
            LDY #$99
            LDY $08A0,X
            LDX #$22
            LDX $08A1,Y
            STA $0200,X 
            KIL";

        var operations = _tokeniser!.Parse(program);
        _mapper.MapAndValidate(operations);


        _assembler.Assemble(operations, 0x4000);

        foreach (var operation in operations)
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(operation.ErrorMessage));
        }

        // Act
        _processor.Run(0x4000, false);

        // Assert
        _memoryMap!.ReadByte(0x022A).Should().Be(0x55);
    }

    [TestMethod]
    public void Standard_Test_2()
    {
        // Arrange
        const string program = @"
	LDA #85
	AND #83
	ORA #56
	EOR #17
	STA $99
	LDA #185
	STA $10
	LDA #231
	STA $11
	LDA #57
	STA $12
	LDA $99
	AND $10
    ORA $11
 	EOR $12
 	LDX #16
 	STA $99
 	LDA #188
 	STA $20
 	LDA #49
 	STA $21
 	LDA #23
    STA $22
 	LDA $99
 	AND $10,X
 	ORA $11,X
 	EOR $12,X
 	STA $99
 	LDA #111
 	STA $0110
 	LDA #60
 	STA $0111
 	LDA #39
 	STA $0112
    LDA $99
 	AND $0110
 	ORA $0111
 	EOR $0112
    STA $99
 	LDA #138
 	STA $0120
 	LDA #71
 	STA $0121
 	LDA #143
 	STA $0122
 	LDA $99
 	AND $0110,X
 	ORA $0111,X
 	EOR $0112,X
 	LDY #32
 	STA $99
 	LDA #115
 	STA $0130
 	LDA #42
 	STA $0131
 	LDA #241
 	STA $0132
 	LDA $99
 	AND $0110,Y
 	ORA $0111,Y
 	EOR $0112,Y
 	STA $99
 	LDA #112
 	STA $30
 	LDA #$01
 	STA $31
 	LDA #113
 	STA $32
    LDA #$01
 	STA $33
 	LDA #114
 	STA $34
    LDA #$01
    STA $35
 	LDA #197
 	STA $0170
 	LDA #124
 	STA $0171
 	LDA #161
 	STA $0172
 	LDA $99
    AND ($20,X)
 	ORA ($22,X)
 	EOR ($24,X)
 	STA $99
 	LDA #96
 	STA $40
	LDA #$01
	STA $41
	LDA #97
	STA $42
	LDA #$01
	STA $43
	LDA #98
	STA $44
	LDA #$01
	STA $45
	LDA #55
	STA $0250
	LDA #35
	STA $0251
	LDA #157
	STA $0252
    NOP
	LDA $99
	LDY #$F0
	AND ($40),Y
	ORA ($42),Y
	EOR ($44),Y
	STA $A9 
    KIL";

        var operations = _tokeniser.Parse(program);

        IMapper mapper = new Mapper();
        mapper.MapAndValidate(operations);

        _assembler.Assemble(operations, 0x4000);

        foreach (var operation in operations)
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(operation.ErrorMessage));
        }

        // Act
        _processor.Run(0x4000, false);

        // Assert
        _memoryMap!.ReadByte(0xA9).Should().Be(0xAA);
    }

    [TestMethod]
    public void Standard_Test_3()
    {
        // Arrange
        const string program = @"
        LDA #$FF
        LDX #$00
        STA $90
        INC $90
        INC $90
        LDA $90
        LDX $90
        STA $90,X
        INC $90,X
        LDA $90,X
        LDX $91
        STA $0190,X
        INC $0192
        LDA $0190,X
        LDX $0192
        STA $0190,X
        INC $0190,X
        LDA $0190,X
        LDX $0193
        STA $0170,X
        DEC $0170,X
        LDA $0170,X
        LDX $0174
        STA $0170,X
        DEC $0173
        LDA $0170,X
        LDX $0173
        STA $70,X
        DEC $70,X
        LDA $70,X
        LDX $72
        STA $70,X
        DEC $71
        DEC $71 
    KIL";

        ITokeniser tokeniser = new Tokeniser();
        var operations = tokeniser.Parse(program);

        IMapper mapper = new Mapper();
        mapper.MapAndValidate(operations);

        _assembler.Assemble(operations, 0x4000);

        foreach (var operation in operations)
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(operation.ErrorMessage));
        }

        // Act
        _processor.Run(0x4000, false);

        // Assert
        _memoryMap!.ReadByte(0x71).Should().Be(0xFF);
    }

    [TestMethod]
    public void Standard_Test_4()
    {
        // Arrange
        const string program = @"
        LDA #$4B
        LSR A
        ASL A
        STA $50
        ASL $50
        ASL $50
        LSR $50
        LDA $50
        LDX $50
        ORA #$C9
        STA $60
        ASL $4C,X
        LSR $4C,X
        LSR $4C,X
        LDA $4C,X
        LDX $60
        ORA #$41
        STA $012E
        LSR $0100,X
        LSR $0100,X
        ASL $0100,X
        LDA $0100,X
        LDX $012E
        ORA #$81
        STA $0100,X
        LSR $0136
        LSR $0136
        ASL $0136
        LDA $0100,X
        ROL A
        ROL A
        ROR A
        STA $70
        LDX $70
        ORA #$03
        STA $0C,X
        ROL $C0
        ROR $C0
        ROR $C0
        LDA $0C,X
        LDX $C0
    	STA $D0
    	ROL $75,X
    	ROL $75,X
    	ROR $75,X
    	LDA $D0
        LDX $D0
    	STA $0100,X
    	ROL $01B7
    	ROL $01B7
    	ROL $01B7
    	ROR $01B7
    	LDA $0100,X
    	LDX $01B7
    	STA $01DD
    	ROL $0100,X
    	ROR $0100,X
    	ROR $0100,X 
        KIL";

        var operations = _tokeniser!.Parse(program);

        IMapper mapper = new Mapper();
        mapper.MapAndValidate(operations);

        _assembler.Assemble(operations, 0x4000);

        foreach (var operation in operations)
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(operation.ErrorMessage));
        }

        // Act
        _processor.Run(0x4000, false);

        // Assert
        _memoryMap!.ReadByte(0x01DD).Should().Be(0x6E);
    }

    [TestMethod]
    public void Standard_Test_5()
    {
        // Arrange
        const string program = @"
               test04:
    	        LDA #36 
    	        STA $20
    	        LDA #64 
    	        STA $21
    	        LDA #$00
                ORA #$03
    	        JMP jump1:
    	        ORA #$FF  
                jump1:
    	        ORA #$30
    	        JSR subr:
    	        ORA #$42
    	        JMP ($0020)
    	        ORA #$FF
            subr:
 	        STA $30
 	        LDX $30
 	        LDA #$00
 	        RTS
         final:
 	        STA $0D,X 
            KIL";

        var operations = _tokeniser.Parse(program);

        IMapper mapper = new Mapper();
        mapper.MapAndValidate(operations);

        _assembler.Assemble(operations, 0x4000);

        foreach (var operation in operations)
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(operation.ErrorMessage));
        }

        // Act
        _processor.Run(0x4000, false);

        // Assert
        _memoryMap!.ReadByte(0x40).Should().Be(0x42);
    }

    [TestMethod]
    public void Standard_Test_6()
    {
        // Arrange
        const string program = @"
              LDA #$35
              TAX
              DEX
         	  DEX
              INX
              TXA
              TAY
              DEY
              DEY
              INY
              TYA
              TAX
              LDA #$20
              TXS
              LDX #$10
              TSX
              TXA
              STA $40 
              KIL";

        var operations = _tokeniser!.Parse(program);

        IMapper mapper = new Mapper();
        mapper.MapAndValidate(operations);


        foreach (var operation in operations)
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(operation.ErrorMessage));
        }

        // Act
        _processor.Run(0x4000, false);

        // Assert
        _memoryMap!.ReadByte(0x40).Should().Be(0x33);
    }

    [TestMethod]
    public void Standard_Test_7()
    {
        // Arrange
        const string program = @"
            LDA #$6A
        	STA $50
        	LDA #$6B
        	STA $51
        	LDA #$A1
        	STA $60
        	LDA #$A2
        	STA $61
            LDA #$FF
        	ADC #$FF
        	ADC #$FF
        	SBC #$AE
            STA $40
        	LDX $40
            ADC $00,X
        	SBC $01,X
        	ADC $60
            SBC $61
            STA $0120
        	LDA #$4D
        	STA $0121
        	LDA #$23
        	ADC $0120
        	SBC $0121
        	STA $F0
        	LDX $F0
        	LDA #$64
        	STA $0124
 	        LDA #$62
        	STA $0125
        	LDA #$26
        	ADC $0100,X
        	SBC $0101,X
            STA $F1
        	LDY $F1
        	LDA #$E5
        	STA $0128
        	LDA #$E9
        	STA $0129
        	LDA #$34
        	ADC $0100,Y
        	SBC $0101,Y
        	STA $F2
        	LDX $F2
        	LDA #$20
        	STA $70
        	LDA #$01
        	STA $71
        	LDA #$24
        	STA $72
        	LDA #$01
        	STA $73
        	ADC ($41,X)
        	SBC ($3F,X)
        	STA $F3
        	LDY $F3
        	LDA #$DA
        	STA $80
        	LDA #$00
            STA $81
        	LDA #$DC
            STA $82
            LDA #$00
        	STA $83
            LDA #$AA
        	ADC ($80),Y
        	SBC ($82),Y
        	STA $30 
                KIL";

        var operations = _tokeniser!.Parse(program);

        IMapper mapper = new Mapper();
        mapper.MapAndValidate(operations);

        _assembler.Assemble(operations, 0x4000);

        foreach (var operation in operations)
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(operation.ErrorMessage));
        }

        // Act
        _processor.Run(0x4000, false);

        // Assert
        _memoryMap!.ReadByte(0x30).Should().Be(0x9D);
    }

    [TestMethod]
    public void Standard_Test_8()
    {
        // Arrange
        const string program = @"
	LDA #$00
	STA $34
	LDA #$FF
	STA $0130
	LDA #$99
	STA $019D
	LDA #$DB
	STA $0199
	LDA #$2F
	STA $32
	LDA #$32
	STA $4F
	LDA #$30
	STA $33
	LDA #$70
	STA $AF
	LDA #$18
	STA $30
NOP
    CMP #$18
 	BEQ beq1:
 	AND #$00 
 beq1:
 	ORA #$01
 	CMP $30
 	BNE bne1: 
 	AND #$00 
 bne1:
 	LDX #$00
 	CMP $0130
 	BEQ beq2:
 	STA $40
 	LDX $40
 beq2:
 	CMP $27,X
 	BNE bne2:
 	ORA #$84
 	STA $41
 	LDX $41
 bne2:
 	AND #$DB
 	CMP $0100,X
 	BEQ beq3:
 	AND #$00 
 beq3:
 	STA $42
 	LDY $42
 	AND #$00
 	CMP $0100,Y
 	BNE bne3:
 	ORA #$0F
 bne3:
 	STA $43
 	LDX $43
 	ORA #$24
 	CMP ($40,X)
 	BEQ beq4:
	ORA #$7F
 beq4:
 	STA $44
 	LDY $44 
 	EOR #$0F
 	CMP ($33),Y
 	BNE bne4:
 	LDA $44
 	STA $15
 bne4: 
   KIL";

        var operations = _tokeniser.Parse(program);

        IMapper mapper = new Mapper();
        mapper.MapAndValidate(operations);

        _assembler.Assemble(operations, 0x4000);

        foreach (var operation in operations)
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(operation.ErrorMessage));
        }

        // Act
        _processor.Run(0x4000, false);

        // Assert
        _memoryMap!.ReadByte(0x15).Should().Be(0x7F);
    }

    [TestMethod]
    public void Standard_Test_9()
    {
        // Arrange
        const string program = @"
	LDA #$A5
	STA $20
	STA $0120
	LDA #$5A
	STA $21
	LDX #$A5
	CPX #$A5
	BEQ b1:
	LDX #$01
 b1:
 	CPX $20
 	BEQ b2:
 	LDX #$02

b2:
	CPX $0120
	BEQ b3:
    LDX #$03 ; not done
b3:
	STX $30
 	LDY $30
 	CPY #$A5
 	BEQ b4:
	LDY #$04  
b4:
 	CPY $20
 	BEQ b5:
	LDY #$05
b5:
 	CPY $0120
	BEQ b6:
	LDY #$06 
b6:	
 	STY $31
 	LDA $31
 	BIT $20
 	BNE b7:
	LDA #$07 
b7:
 	BIT $0120
 	BNE b8:
	LDA #$08
b8:
 	BIT $21
 	BNE b9:
	STA $42	
 b9: 
KIL";

        var operations = _tokeniser!.Parse(program);

        IMapper mapper = new Mapper();
        mapper.MapAndValidate(operations);

        _assembler.Assemble(operations, 0x4000);

        foreach (var operation in operations)
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(operation.ErrorMessage));
        }

        // Act
        _processor.Run(0x4000, false);

        // Assert
        _memoryMap!.ReadByte(0x42).Should().Be(0xA5);
    }

    [TestMethod]
    public void Standard_Test_10()
    {
        // Arrange
        const string program = @"
	LDA #$54
	STA $32
	LDA #$B3
	STA $A1
	LDA #$87
	STA $43
	LDX #$A1
    BPL bpl1:
	LDX #$32
 bpl1:
 	LDY $00,X
 	BPL bpl2:
	LDA #$05 
	LDX $A1 
bpl2:
 	BMI bmi1:
	SBC #$03
 bmi1:
 	BMI bmi2:
	LDA #$41
bmi2:
 	EOR #$30
 	STA $32
 	ADC $00,X
 	BVC bvc1:
	LDA #$03
 bvc1:
 	STA $54
 	LDX $00,Y
 	ADC $51,X
 	BVC bvc2:
	LDA #$E5 
bvc2:
    NOP
 	ADC $40,X
 	BVS bvs1:
	STA $0001,Y
 	ADC $55
 bvs1:
 	BVS bvs2:
	LDA #$00
 bvs2:
 	ADC #$F0
 	BCC bcc1:
	STA $60
  	ADC $43
 bcc1:
 	BCC bcc2:
	LDA #$FF
 bcc2:
 	ADC $54
 	BCS bcs1:
	ADC #$87
	LDX $60
 bcs1:	
 	BCS bcs2:
	LDA #$00 
bcs2:
 	STA $73,X 
 KIL";

        var operations = _tokeniser.Parse(program);

        IMapper mapper = new Mapper();
        mapper.MapAndValidate(operations);

        _assembler.Assemble(operations, 0x4000);

        foreach (var operation in operations)
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(operation.ErrorMessage));
        }

        // Act
        _processor.Run(0x4000, false);

        // Assert
        _memoryMap!.ReadByte(0x80).Should().Be(0x1F);
    }

    [TestMethod]
    public void Standard_Test_11()
    {
        // Arrange
        const string program = @"
    		ADC #$00
        	LDA #$99
        	ADC #$87
        	CLC
        	NOP
        	BCC t10bcc1:
        	ADC #$60
	        ADC #$93 ; not done
            t10bcc1:
        	SEC
        	NOP
        	BCC t10bcc2:
        	CLV
            t10bcc2:
        	BVC t10bvc1:
        	LDA #$00
            t10bvc1: 
        	ADC #$AD
        	NOP
        	STA $30 
             KIL";

        var operations = _tokeniser!.Parse(program);

        IMapper mapper = new Mapper();
        mapper.MapAndValidate(operations);

        _assembler.Assemble(operations, 0x4000);

        foreach (var operation in operations)
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(operation.ErrorMessage));
        }

        // Act
        _processor.Run(0x4000, false);

        // Assert
        _memoryMap!.ReadByte(0x30).Should().Be(0xCE);
    }

    [TestMethod]
    public void Standard_Test_12()
    {
        // Arrange
        const string program = @"
    		ADC #$01
        	LDA #$27
        	ADC #$01
        	SEC
        	PHP
        	CLC
        	PLP
        	ADC #$00
        	PHA
        	LDA #$00
        	PLA
        	STA $30 
             KIL";

        var operations = _tokeniser.Parse(program);

        IMapper mapper = new Mapper();
        mapper.MapAndValidate(operations);

        _assembler.Assemble(operations, 0x4000);

        foreach (var operation in operations)
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(operation.ErrorMessage));
        }

        // Act
        _processor.Run(0x4000, false);

        // Assert
        _memoryMap!.ReadByte(0x30).Should().Be(0x29);
    }

    [TestMethod]
    public void Can_Do_8bit_Mult()
    {
        // Arrange
        const string program = @".VAR  num1=$C000 ;comment
            .VAR num2=$C010
            .VAR num1Hi = $C020
            lda#100
            sta num1
            lda#10
            sta num2
            lda #$00
            tay
            sty num1Hi
            beq enterLoop:
            doAdd:
            clc
            adc num1
            tax
            tya
            adc num1Hi
            tay
            txa
            loop:
            asl num1
            rol num1Hi
            enterLoop:
            lsr num2
            bcs doAdd:
            bne loop:
            KIL
            ";

        var operations = _tokeniser!.Parse(program);

        IMapper mapper = new Mapper();
        mapper.MapAndValidate(operations);

        _assembler.Assemble(operations, 0x2000);

        foreach (var operation in operations)
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(operation.ErrorMessage));
        }

        // Act
        _processor.Run(0x2000, false);

        // Assert
        Assert.AreEqual(232, _processor.Accumulator);
        Assert.AreEqual(232, _processor.IX);
        Assert.AreEqual(3, _processor.IY);
    }

    [TestMethod]
    public void Can_Multiply_By_2x10()
    {
        // Arrange 
        var program = "LDA#2" + Environment.NewLine;
        program += "JSR MULT10:" + Environment.NewLine;
        program += "KIL" + Environment.NewLine;
        program += "MULT10:" + Environment.NewLine;
        program += "ASL A" + Environment.NewLine;
        program += "STA TEMP:" + Environment.NewLine;
        program += "ASL A" + Environment.NewLine;
        program += "ASL A" + Environment.NewLine;
        program += "CLC" + Environment.NewLine;
        program += "ADC TEMP:" + Environment.NewLine;
        program += "RTS" + Environment.NewLine;
        program += "TEMP:" + Environment.NewLine;
        program += ".byte 0" + Environment.NewLine;

        var operations = _tokeniser.Parse(program);

        IMapper mapper = new Mapper();
        mapper.MapAndValidate(operations);


        _assembler.Assemble(operations, 3000);

        // Act
        _processor.Run(3000, false);

        // Assert
        _processor.Accumulator.Should().Be(20);
    }
}