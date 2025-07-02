namespace MLDComputing.Emulators.BBCSim._6502.Disassembler;

using System.Globalization;
using System.Reflection;
using System.Text.Json;

public static class Helper
{
    public static Dictionary<ushort, (ushort End, string Name)> GetStackMap()
    {
        var dict = new Dictionary<ushort, (ushort end, string name)>
        {
            [0x01FF] = (0x0100, "Stack")
        };

        return dict;
    }

    public static Dictionary<ushort, (ushort End, string Name)> GetDataLabelMap()
    {
        var dict = new Dictionary<ushort, (ushort end, string name)>
        {
            [0x8005] = (0x8022, "BASIC - Messages"),
            [0x8071] = (0x836C, "BASIC - Keyword Table"),
            [0x836D] = (0x8450, "BASIC - LSB/MSB Action Addresses"),
            [0x8451] = (0x84C4, "BASIC - Assembler Mnemonics"),
            [0x84C5] = (0x84Fc, "BASIC - Base Op Codes"),
            [0x8AA3] = (0x8AAD, "BASIC - Missing Text"),
            [0x8AD4] = (0x8AD9, "BASIC - Stop Text"),
            [0x8B5A] = (0x8B5F, "BASIC - No Text"),
            [0x8CB8] = (0x8cc0, "BASIC - No Room Text"),
            [0x8E99] = (0x8ea3, "BASIC - Missing Text"),
            [0x9128] = (0x912E, "BASIC - Bad Dim"),
            [0x9219] = (0x9221, "BASIC - Dim Space"),
            [0x9366] = (0x9379, "BASIC - Error Messages"),
            [0x95C0] = (0x95c8, "BASIC - Error Messages"),
            [0x97D2] = (0x97Dc, "BASIC - Error Messages"),
            [0x9822] = (0x9840, "BASIC - Error Messages"),
            [0x99A8] = (0x99Bd, "BASIC - DIV0 Messages"),
            [0x9C04] = (0x9C14, "BASIC - String too long Messages"),
            [0xaA5d] = (0xAA90, "BASIC - Constants"),
            [0xAe44] = (0xAe55, "BASIC - No Such Variable Message"),
            [0xb105] = (0xb111, "BASIC - No Such # Message"),
            [0xb18b] = (0xb192, "BASIC - Bad Call Message"),
            [0xb6c8] = (0xb6d6, "BASIC - Cant match for"),
            [0xb7a5] = (0xb7c3, "BASIC - FOR error messages"),
            [0xb90b] = (0xb914, "BASIC - ON Syntax messages"),
            [0xb9b6] = (0xb9c3, "BASIC - NO Such Line messages"),
            [0xbccd] = (0xbcd5, "BASIC - Line messages"),
            [0xbfc4] = (0xbfce, "BASIC - Missing messages"),
            [0xbff9] = (0xbfff, "BASIC - Rogers Wilson messages"),


            [0x0000] = (0x00ff, "OS - Zero Page"),
            [0x0100] = (0x01ff, "OS - Page 1 - Stack"),
            [0x0200] = (0x02ff, "OS - Page 2 - OS Workspace – System flags, pointers, handlers"),
            [0x0300] = (0x03ff, "OS - Page 3 - OS Workspace – File buffers, VDU drivers"),
            [0x0400] = (0x04ff, "OS - Page 3 - OS Workspace – More System variables"),
            [0x0500] = (0x05ff, "OS - Page 3 - OS Workspace – OS Workspace and user application variables"),
            [0xC000] = (0xC2ff, "OS - Character Definitions"),
            [0xC302] = (0xC4BF, "OS - VDU Tables"),
            [0xD940] = (0xD975, "OS - Default Vector Table"),
            [0xD976] = (0xD9CC, "OS - MOS Variable Table"),
            [0xDF0C] = (0xDF09, "OS - CopyrightString Match"),
            [0xDF10] = (0xDF88, "OS - OS Command table"),
            [0xE435] = (0xE44F, "OS - Hardware Buffer Addresses"),
            [0xE5B3] = (0xE639, "OS - OSBYTE Dispatch Table"),
            [0xE63B] = (0xE655, "OS - OSWORD Dispatch Table"),
            [0xEAD2] = (0xEAD8, "OS - BOOT File Text for ROMS"),
            [0xEDFB] = (0xEE12, "OS - PITCH lookup table"),
            [0xF03B] = (0xF044, "OS - Key data block 1"),
            [0xF04A] = (0xF054, "OS - Key data block 2"),
            [0xF05A] = (0xF064, "OS - Key data block 3"),
            [0xF06B] = (0xF074, "OS - Key data block 4"),
            [0xF075] = (0xF07A, "OS - Speech Routine Data"),
            [0xF085] = (0xF08A, "OS - Key data block 5"),
            [0xF08B] = (0xF094, "OS - Key data block 6"),
            [0xF09B] = (0xF0A4, "OS - Key data block 7"),
            [0xF18E] = (0xF1A2, "OS -  CFS/RFS FSC DISPATCH TABLE"),

            [0xFFB6] = (0xFFB8, "OS - DEFAULT VECTOR TABLE"),
            [0xFFFA] = (0xFFFB, "OS - NMI address"),
            [0xFFFC] = (0xFFFD, "OS - RESET address"),
            [0xFFFE] = (0xFFFF, "OS - IRQ address")
        };

        return dict;
    }

    public static Dictionary<ushort, (ushort End, string Name)> GetCodeLabelMap()
    {
        var dict = new Dictionary<ushort, (ushort end, string name)>
        {
            [0x8000] = (0x8004, "BASIC - Initialisation"),
            [0x8023] = (0x806E, "BASIC - Language Initialisation"),
            [0x84FD] = (0x8501, "BASIC - Assembler Exit"),
            [0x8504] = (0x8672, "BASIC - Assembler Entry Point"),
            [0x8673] = (0x8896, "BASIC - Assembler Routines for Instruction Groups"),
            [0x8897] = (0x8AA2, "BASIC - Basic Routines 1"),
            [0x8AAE] = (0x8AD3, "BASIC - Basic Routines 2"),
            [0x8ADA] = (0x8B59, "BASIC - Basic Routines 3"),
            [0x8b60] = (0x8CB7, "BASIC - Basic Routines 4"),
            [0x8cc1] = (0x8E98, "BASIC - Basic Routines 5"),
            [0x8ea4] = (0x9127, "BASIC - Basic Routines 6"),
            [0x912F] = (0x9218, "BASIC - Basic Routines 7"),
            [0x9222] = (0x9365, "BASIC - Basic Routines 8"),
            [0x937A] = (0x95BF, "BASIC - Basic Routines 9"),
            [0x95C9] = (0x97D1, "BASIC - Basic Routines 10"),
            [0x97DD] = (0x9821, "BASIC - Basic Routines 11"),
            [0x9841] = (0x99A7, "BASIC - Basic Routines 12"),
            [0x99BE] = (0x9C03, "BASIC - Basic Routines 13"),
            [0x9C15] = (0xaA5D, "BASIC - Floating Point Routines"),
            [0xAA91] = (0xAE43, "BASIC - Floating Point Routines"),
            [0xAe56] = (0xB104, "BASIC - Deal with bracketed expressions"),
            [0xB112] = (0xB18A, "BASIC - Basic Routines 14"),
            [0xb195] = (0xB6c7, "BASIC FN Routines"),
            [0xB6d7] = (0xB7a4, "BASIC - Basic Routines 15"),
            [0xb7c4] = (0xB90a, "BASIC - FOR Routine"),
            [0xb915] = (0xB9b5, "BASIC - ON Routine"),
            [0xb9c4] = (0xbccc, "BASIC - Type Mismatch Routine messages"),
            [0xbCD6] = (0xbFC3, "BASIC - Basic Routines 16"),
            [0xbfc5] = (0xbFF6, "BASIC - Print String"),

            [0xC300] = (0xC302, "OS - VDU Startup Entry"),
            [0xC4C0] = (0xC734, "OS - VDU Part 1 - Main Routines"),
            [0xC735] = (0xCA38, "OS - VDU Part 2 - Read Pixel Etc"),
            [0xCA39] = (0xCDFE, "OS - VDU Part 3 - Read Character etc"),
            [0xCDFF] = (0xD10C, "OS - VDU Part 4 - Scroll And Plot"),
            [0xD10D] = (0xD4BC, "OS - VDU Part 5 - Margins Scaling"),
            [0xD4BF] = (0xD93F, "OS - VDU Part 6 - Fills"),
            [0xD9CD] = (0xD9E6, "OS - Reset/Break Entry Point"),
            [0xD9E7] = (0xDA02, "OS - Clear memory routines"),
            [0xDA03] = (0xDA25, "OS - Setup System Via"),
            [0xDA26] = (0xDA6A, "OS - Set up page 2"),
            [0xDA6B] = (0xDABC, "OS - Clear interrupt and enable registers of Both VIAs"),
            [0xDABD] = (0xDB10, "OS - Check sideways ROMs"),
            [0xDB11] = (0xDB26, "OS - Check Speech System"),
            [0xDB27] = (0xDB31, "OS - Screen Setup"),
            [0xDB32] = (0xDB86, "OS - Break Intercept with Carry Clear"),
            [0xDB87] = (0xDBBD, "OS - Break Intercept with Carry Set"),
            [0xDBBE] = (0xDBE6, "OS - ROM Language Handling"),
            [0xDBE7] = (0xDC07, "OS - Enter Language ROM OSBYTE 142"),
            [0xDC08] = (0xDC0A, "OS - Enter TUBE Software"),
            [0xDC0B] = (0xDC1B, "OS - OSRDRM entry point"),
            [0xDC1C] = (0xDC26, "OS - MAIN IRQ Entry point"),
            [0xDC27] = (0xDC53, "OS - BRK handling routine"),
            [0xDC54] = (0xDC67, "OS - DEFAULT BRK HANDLER"),
            [0xDC68] = (0xDC92, "OS - Serial Printer Port Handling"),
            [0xDC93] = (0xDD05, "OS - Main IRQ Handling routines, default IRQ1V destination"),
            [0xDD06] = (0xDD46, "OS - VIA INTERUPTS ROUTINES"),
            [0xDD47] = (0xDD67, "OS - PRINTER INTERRUPT USER VIA 1"),
            [0xDD69] = (0xDDC9, "OS - SYSTEM INTERRUPT 5   Speech"),
            [0xDDCA] = (0xDE46, "OS - SYSTEM INTERRUPT 6 10mS Clock"),
            [0xDE47] = (0xDE71, "OS - SYSTEM INTERRUPT 4 ADC end of conversion"),
            [0xDE72] = (0xDE88, "OS - SYSTEM INTERRUPT 0 Keyboard"),
            [0xDE89] = (0xDE8B, "OS - IRQ2V default entry"),
            [0xDE8C] = (0xDEC4, "OS - OSBYTE 17 Start conversion"),
            [0xDEC5] = (0xDF0B, "OS - OSRDCH Default entry point"),
            [0xDF89] = (0xE0A3, "OS - CLI"),
            [0xE0A4] = (0xE113, "OS - OSWRCH HANDLER"),
            [0xE114] = (0xE17B, "OS - PRINTER DRIVER"),
            [0xE17C] = (0xE196, "OS - OSBYTE 156"),
            [0xE197] = (0xE1D0, "OS - OSBYTE 123"),
            [0xE1D1] = (0xE20D, "OS - COUNT PURGE VECTOR"),
            [0xE20E] = (0xE23B, "OS - *SAVE/*LOAD SETUP"),
            [0xE23C] = (0xE274, "OS - *SAVE/*LOAD ENTRY"),
            [0xE275] = (0xE326, "OS - OSBYTE 119"),
            [0xE327] = (0xE341, "OS - *KEY ENTRY"),
            [0xE342] = (0xE434, "OS - *FX   OSBYTE"),
            [0xE450] = (0xE5B2, "OS - Buffer and Event handling"),
            [0xE658] = (0xE82C, "OS - OSBYTE and OSWORD Handling"),
            [0xE82D] = (0xEAD1, "OS - Sound System"),
            [0xEAD9] = (0xEDFA, "OS - OSBYTES and SOUND Interrupts"),
            [0xEE13] = (0xEED9, "OS - ROM FS Handling"),
            [0xEEDA] = (0xF03A, "OS - MAIN KEYBOARD HANDLER"),
            [0xF045] = (0xF049, "OS - OSBYTE 120  Write KEY pressed Data"),
            [0xF055] = (0xF059, "OS - Jim paged entry vector"),
            [0xF065] = (0xF06A, "OS - Jump to keyboard handler interupt routine"),
            [0xF085] = (0xF08A, "OS - OSBYTE 131  READ OSHWM  (PAGE in BASIC)"),
            [0xF095] = (0xF09A, "OS - Set input buffer number and flush it "),
            [0xF0A5] = (0xF0A7, "OS - go to eventV handling routine"),
            [0xF0A8] = (0xF134, "OS - OSBYTE 15/21 *HELP 122, 121 Keyboard Scan"),
            [0xF135] = (0xF166, "OS - OSBYTE 140  *TAPE   OSBYTE 141  *ROM"),
            [0xF168] = (0xF18D, "OS - OSBYTE 143"),
            [0xF18E] = (0xF1A2, "OS -  CFS OSARGS entry point "),
            [0xF1B1] = (0xF1C3, "OS - Filing System control entry     OSFSC "),
            [0xF1C4] = (0xF27C, "OS - Load File "),
            [0xF27D] = (0xF303, "OS - OS FILE ENntry "),
            [0xF305] = (0xF328, "OS - *RUN    ENTRY   "),
            [0xF32B] = (0xF68B, "OS - *CAT    ENTRY and file handling generally"),
            [0xF68D] = (0xFB19, "OS - *EXEC"),
            [0xFB1A] = (0xFBFF, "OS - Claim serial system for sequential Access"),
            [0xFF00] = (0xFFA6, "OS - Extended Vector ENntry Points"),
            [0xFFA7] = (0xFFB5, "OS -  OSBYTE CALLS"),
            [0xFFB9] = (0xFFF7, "OS -  OPERATING SYSTEM FUNCTION CALLS")
        };

        return dict;
    }

    public static Dictionary<ushort, string> GetComments()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "MLDComputing.Emulators.BBCSim._6502.Disassembler.Resources.json.txt";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream!);
        var json = reader.ReadToEnd();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var list = JsonSerializer.Deserialize<List<AddressComment>>(json, options);

        var dict = new Dictionary<ushort, string>();

        foreach (var item in list!)
        {
            if (ushort.TryParse(item.address.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                    out var address))
            {
                if (!dict.ContainsKey(address))
                {
                    dict[address] = item.comment;
                }
                // Optionally handle duplicates here, for example:
                // Console.WriteLine($"Duplicate address: 0x{address:X4}");
                // dict[address] += "; " + item.Comment; // or overwrite, or skip
            }
        }

        return dict;
    }

    private class AddressComment
    {
        public string? address { get; set; }

        public string? comment { get; set; }
    }
}