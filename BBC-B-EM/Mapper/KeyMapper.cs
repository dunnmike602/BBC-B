namespace MLDComputing.Emulators.BBCSim.Mapper;

using Enums;

public class KeyMapper
{
    private const int KeyMapSize = 512; // Or however many keys you want to support

    private readonly KeyMapping[,] _defaultKeyMap = new KeyMapping[KeyMapSize, 2];
    private readonly KeyMapping[,] _logicalKeyMap = new KeyMapping[KeyMapSize, 2];
    private readonly KeyMapping[,] _userKeyMap = new KeyMapping[KeyMapSize, 2];


    public void InitKeyMaps()
    {
        ResetKeyMap(_defaultKeyMap);
        ResetKeyMap(_logicalKeyMap);
        ResetKeyMap(_userKeyMap);

        // You can now populate DefaultKeyMap with actual mappings...
        // Example: Map PC keycode 65 ('A') to BBC row 4, column 1
        InitDefaultKeyMap(_defaultKeyMap);
        InitDefaultKeyMap(_logicalKeyMap);
        InitDefaultKeyMap(_userKeyMap);
    }

    // Auto-generated DefaultKeyMap initialization from BeebEm key matrix
    public static void InitDefaultKeyMap(KeyMapping[,] keyMap)
    {
        keyMap[8, 0] = new KeyMapping { Row = 5, Column = 9, RequiresShift = false };
        keyMap[8, 1] = new KeyMapping { Row = 5, Column = 9, RequiresShift = true };
        keyMap[9, 0] = new KeyMapping { Row = 6, Column = 0, RequiresShift = false };
        keyMap[9, 1] = new KeyMapping { Row = 6, Column = 0, RequiresShift = true };
        keyMap[13, 0] = new KeyMapping { Row = 4, Column = 9, RequiresShift = false };
        keyMap[13, 1] = new KeyMapping { Row = 4, Column = 9, RequiresShift = true };
        keyMap[16, 0] = new KeyMapping { Row = 0, Column = 0, RequiresShift = false };
        keyMap[16, 1] = new KeyMapping { Row = 0, Column = 0, RequiresShift = true };
        keyMap[17, 0] = new KeyMapping { Row = 0, Column = 1, RequiresShift = false };
        keyMap[17, 1] = new KeyMapping { Row = 0, Column = 1, RequiresShift = true };
        keyMap[20, 0] = new KeyMapping { Row = 4, Column = 0, RequiresShift = false };
        keyMap[20, 1] = new KeyMapping { Row = 4, Column = 0, RequiresShift = true };
        keyMap[27, 0] = new KeyMapping { Row = 7, Column = 0, RequiresShift = false };
        keyMap[27, 1] = new KeyMapping { Row = 7, Column = 0, RequiresShift = true };
        keyMap[32, 0] = new KeyMapping { Row = 6, Column = 2, RequiresShift = false };
        keyMap[32, 1] = new KeyMapping { Row = 6, Column = 2, RequiresShift = true };
        keyMap[35, 0] = new KeyMapping { Row = 6, Column = 9, RequiresShift = false };
        keyMap[35, 1] = new KeyMapping { Row = 6, Column = 9, RequiresShift = true };
        keyMap[37, 0] = new KeyMapping { Row = 1, Column = 9, RequiresShift = false };
        keyMap[37, 1] = new KeyMapping { Row = 1, Column = 9, RequiresShift = true };
        keyMap[38, 0] = new KeyMapping { Row = 3, Column = 9, RequiresShift = false };
        keyMap[38, 1] = new KeyMapping { Row = 3, Column = 9, RequiresShift = true };
        keyMap[39, 0] = new KeyMapping { Row = 7, Column = 9, RequiresShift = false };
        keyMap[39, 1] = new KeyMapping { Row = 7, Column = 9, RequiresShift = true };
        keyMap[40, 0] = new KeyMapping { Row = 2, Column = 9, RequiresShift = false };
        keyMap[40, 1] = new KeyMapping { Row = 2, Column = 9, RequiresShift = true };
        keyMap[46, 0] = new KeyMapping { Row = 5, Column = 9, RequiresShift = false };
        keyMap[46, 1] = new KeyMapping { Row = 5, Column = 9, RequiresShift = true };
        keyMap[48, 0] = new KeyMapping { Row = 2, Column = 7, RequiresShift = false };
        keyMap[48, 1] = new KeyMapping { Row = 2, Column = 7, RequiresShift = true };
        keyMap[49, 0] = new KeyMapping { Row = 3, Column = 0, RequiresShift = false };
        keyMap[49, 1] = new KeyMapping { Row = 3, Column = 0, RequiresShift = true };
        keyMap[50, 0] = new KeyMapping { Row = 3, Column = 1, RequiresShift = false };
        keyMap[50, 1] = new KeyMapping { Row = 3, Column = 1, RequiresShift = true };
        keyMap[51, 0] = new KeyMapping { Row = 1, Column = 1, RequiresShift = false };
        keyMap[51, 1] = new KeyMapping { Row = 1, Column = 1, RequiresShift = true };
        keyMap[52, 0] = new KeyMapping { Row = 1, Column = 2, RequiresShift = false };
        keyMap[52, 1] = new KeyMapping { Row = 1, Column = 2, RequiresShift = true };
        keyMap[53, 0] = new KeyMapping { Row = 1, Column = 3, RequiresShift = false };
        keyMap[53, 1] = new KeyMapping { Row = 1, Column = 3, RequiresShift = true };
        keyMap[54, 0] = new KeyMapping { Row = 3, Column = 4, RequiresShift = false };
        keyMap[54, 1] = new KeyMapping { Row = 3, Column = 4, RequiresShift = true };
        keyMap[55, 0] = new KeyMapping { Row = 2, Column = 4, RequiresShift = false };
        keyMap[55, 1] = new KeyMapping { Row = 2, Column = 4, RequiresShift = true };
        keyMap[56, 0] = new KeyMapping { Row = 1, Column = 5, RequiresShift = false };
        keyMap[56, 1] = new KeyMapping { Row = 1, Column = 5, RequiresShift = true };
        keyMap[57, 0] = new KeyMapping { Row = 2, Column = 6, RequiresShift = false };
        keyMap[57, 1] = new KeyMapping { Row = 2, Column = 6, RequiresShift = true };
        keyMap[65, 0] = new KeyMapping { Row = 4, Column = 1, RequiresShift = false };
        keyMap[65, 1] = new KeyMapping { Row = 4, Column = 1, RequiresShift = true };
        keyMap[66, 0] = new KeyMapping { Row = 6, Column = 4, RequiresShift = false };
        keyMap[66, 1] = new KeyMapping { Row = 6, Column = 4, RequiresShift = true };
        keyMap[67, 0] = new KeyMapping { Row = 5, Column = 2, RequiresShift = false };
        keyMap[67, 1] = new KeyMapping { Row = 5, Column = 2, RequiresShift = true };
        keyMap[68, 0] = new KeyMapping { Row = 3, Column = 2, RequiresShift = false };
        keyMap[68, 1] = new KeyMapping { Row = 3, Column = 2, RequiresShift = true };
        keyMap[69, 0] = new KeyMapping { Row = 2, Column = 2, RequiresShift = false };
        keyMap[69, 1] = new KeyMapping { Row = 2, Column = 2, RequiresShift = true };
        keyMap[70, 0] = new KeyMapping { Row = 4, Column = 3, RequiresShift = false };
        keyMap[70, 1] = new KeyMapping { Row = 4, Column = 3, RequiresShift = true };
        keyMap[71, 0] = new KeyMapping { Row = 5, Column = 3, RequiresShift = false };
        keyMap[71, 1] = new KeyMapping { Row = 5, Column = 3, RequiresShift = true };
        keyMap[72, 0] = new KeyMapping { Row = 5, Column = 4, RequiresShift = false };
        keyMap[72, 1] = new KeyMapping { Row = 5, Column = 4, RequiresShift = true };
        keyMap[73, 0] = new KeyMapping { Row = 2, Column = 5, RequiresShift = false };
        keyMap[73, 1] = new KeyMapping { Row = 2, Column = 5, RequiresShift = true };
        keyMap[74, 0] = new KeyMapping { Row = 4, Column = 5, RequiresShift = false };
        keyMap[74, 1] = new KeyMapping { Row = 4, Column = 5, RequiresShift = true };
        keyMap[75, 0] = new KeyMapping { Row = 4, Column = 6, RequiresShift = false };
        keyMap[75, 1] = new KeyMapping { Row = 4, Column = 6, RequiresShift = true };
        keyMap[76, 0] = new KeyMapping { Row = 5, Column = 6, RequiresShift = false };
        keyMap[76, 1] = new KeyMapping { Row = 5, Column = 6, RequiresShift = true };
        keyMap[77, 0] = new KeyMapping { Row = 6, Column = 5, RequiresShift = false };
        keyMap[77, 1] = new KeyMapping { Row = 6, Column = 5, RequiresShift = true };
        keyMap[78, 0] = new KeyMapping { Row = 5, Column = 5, RequiresShift = false };
        keyMap[78, 1] = new KeyMapping { Row = 5, Column = 5, RequiresShift = true };
        keyMap[79, 0] = new KeyMapping { Row = 3, Column = 6, RequiresShift = false };
        keyMap[79, 1] = new KeyMapping { Row = 3, Column = 6, RequiresShift = true };
        keyMap[80, 0] = new KeyMapping { Row = 3, Column = 7, RequiresShift = false };
        keyMap[80, 1] = new KeyMapping { Row = 3, Column = 7, RequiresShift = true };
        keyMap[81, 0] = new KeyMapping { Row = 1, Column = 0, RequiresShift = false };
        keyMap[81, 1] = new KeyMapping { Row = 1, Column = 0, RequiresShift = true };
        keyMap[82, 0] = new KeyMapping { Row = 3, Column = 3, RequiresShift = false };
        keyMap[82, 1] = new KeyMapping { Row = 3, Column = 3, RequiresShift = true };
        keyMap[83, 0] = new KeyMapping { Row = 5, Column = 1, RequiresShift = false };
        keyMap[83, 1] = new KeyMapping { Row = 5, Column = 1, RequiresShift = true };
        keyMap[84, 0] = new KeyMapping { Row = 2, Column = 3, RequiresShift = false };
        keyMap[84, 1] = new KeyMapping { Row = 2, Column = 3, RequiresShift = true };
        keyMap[85, 0] = new KeyMapping { Row = 3, Column = 5, RequiresShift = false };
        keyMap[85, 1] = new KeyMapping { Row = 3, Column = 5, RequiresShift = true };
        keyMap[86, 0] = new KeyMapping { Row = 6, Column = 3, RequiresShift = false };
        keyMap[86, 1] = new KeyMapping { Row = 6, Column = 3, RequiresShift = true };
        keyMap[87, 0] = new KeyMapping { Row = 2, Column = 1, RequiresShift = false };
        keyMap[87, 1] = new KeyMapping { Row = 2, Column = 1, RequiresShift = true };
        keyMap[88, 0] = new KeyMapping { Row = 4, Column = 2, RequiresShift = false };
        keyMap[88, 1] = new KeyMapping { Row = 4, Column = 2, RequiresShift = true };
        keyMap[89, 0] = new KeyMapping { Row = 4, Column = 4, RequiresShift = false };
        keyMap[89, 1] = new KeyMapping { Row = 4, Column = 4, RequiresShift = true };
        keyMap[90, 0] = new KeyMapping { Row = 6, Column = 1, RequiresShift = false };
        keyMap[90, 1] = new KeyMapping { Row = 6, Column = 1, RequiresShift = true };
        keyMap[93, 0] = new KeyMapping { Row = 6, Column = 2, RequiresShift = false };
        keyMap[93, 1] = new KeyMapping { Row = 6, Column = 2, RequiresShift = true };
        keyMap[112, 0] = new KeyMapping { Row = 7, Column = 1, RequiresShift = false };
        keyMap[112, 1] = new KeyMapping { Row = 7, Column = 1, RequiresShift = true };
        keyMap[113, 0] = new KeyMapping { Row = 7, Column = 2, RequiresShift = false };
        keyMap[113, 1] = new KeyMapping { Row = 7, Column = 2, RequiresShift = true };
        keyMap[114, 0] = new KeyMapping { Row = 7, Column = 3, RequiresShift = false };
        keyMap[114, 1] = new KeyMapping { Row = 7, Column = 3, RequiresShift = true };
        keyMap[115, 0] = new KeyMapping { Row = 1, Column = 4, RequiresShift = false };
        keyMap[115, 1] = new KeyMapping { Row = 1, Column = 4, RequiresShift = true };
        keyMap[116, 0] = new KeyMapping { Row = 7, Column = 4, RequiresShift = false };
        keyMap[116, 1] = new KeyMapping { Row = 7, Column = 4, RequiresShift = true };
        keyMap[117, 0] = new KeyMapping { Row = 7, Column = 5, RequiresShift = false };
        keyMap[117, 1] = new KeyMapping { Row = 7, Column = 5, RequiresShift = true };
        keyMap[118, 0] = new KeyMapping { Row = 1, Column = 6, RequiresShift = false };
        keyMap[118, 1] = new KeyMapping { Row = 1, Column = 6, RequiresShift = true };
        keyMap[119, 0] = new KeyMapping { Row = 7, Column = 6, RequiresShift = false };
        keyMap[119, 1] = new KeyMapping { Row = 7, Column = 6, RequiresShift = true };
        keyMap[120, 0] = new KeyMapping { Row = 7, Column = 7, RequiresShift = false };
        keyMap[120, 1] = new KeyMapping { Row = 7, Column = 7, RequiresShift = true };
        keyMap[121, 0] = new KeyMapping { Row = 2, Column = 0, RequiresShift = false };
        keyMap[121, 1] = new KeyMapping { Row = 2, Column = 0, RequiresShift = true };
        keyMap[122, 0] = new KeyMapping { Row = 2, Column = 0, RequiresShift = false };
        keyMap[122, 1] = new KeyMapping { Row = 2, Column = 0, RequiresShift = true };
        keyMap[186, 0] = new KeyMapping { Row = 5, Column = 7, RequiresShift = false };
        keyMap[186, 1] = new KeyMapping { Row = 5, Column = 7, RequiresShift = true };
        keyMap[187, 0] = new KeyMapping { Row = 1, Column = 8, RequiresShift = false };
        keyMap[187, 1] = new KeyMapping { Row = 1, Column = 8, RequiresShift = true };
        keyMap[188, 0] = new KeyMapping { Row = 6, Column = 6, RequiresShift = false };
        keyMap[188, 1] = new KeyMapping { Row = 6, Column = 6, RequiresShift = true };
        keyMap[189, 0] = new KeyMapping { Row = 1, Column = 7, RequiresShift = false };
        keyMap[189, 1] = new KeyMapping { Row = 1, Column = 7, RequiresShift = true };
        keyMap[190, 0] = new KeyMapping { Row = 6, Column = 7, RequiresShift = false };
        keyMap[190, 1] = new KeyMapping { Row = 6, Column = 7, RequiresShift = true };
        keyMap[191, 0] = new KeyMapping { Row = 6, Column = 8, RequiresShift = false };
        keyMap[191, 1] = new KeyMapping { Row = 6, Column = 8, RequiresShift = true };
        keyMap[192, 0] = new KeyMapping { Row = 4, Column = 8, RequiresShift = false };
        keyMap[192, 1] = new KeyMapping { Row = 4, Column = 8, RequiresShift = true };
        keyMap[219, 0] = new KeyMapping { Row = 3, Column = 8, RequiresShift = false };
        keyMap[219, 1] = new KeyMapping { Row = 3, Column = 8, RequiresShift = true };
        keyMap[220, 0] = new KeyMapping { Row = 7, Column = 8, RequiresShift = false };
        keyMap[220, 1] = new KeyMapping { Row = 7, Column = 8, RequiresShift = true };
        keyMap[221, 0] = new KeyMapping { Row = 5, Column = 8, RequiresShift = false };
        keyMap[221, 1] = new KeyMapping { Row = 5, Column = 8, RequiresShift = true };
        keyMap[222, 0] = new KeyMapping { Row = 2, Column = 8, RequiresShift = false };
        keyMap[222, 1] = new KeyMapping { Row = 2, Column = 8, RequiresShift = true };
        keyMap[223, 0] = new KeyMapping { Row = 4, Column = 7, RequiresShift = false };
        keyMap[223, 1] = new KeyMapping { Row = 4, Column = 7, RequiresShift = true };
    }

    private void ResetKeyMap(KeyMapping[,] map)
    {
        for (var pcKey = 0; pcKey < KeyMapSize; pcKey++)
        {
            for (var shift = 0; shift < 2; shift++)
            {
                map[pcKey, shift].Row = 0;
                map[pcKey, shift].Column = 0;
                map[pcKey, shift].RequiresShift = shift == 1;
            }
        }
    }

    public KeyMapping ProcessKeyPress(int keyCode, bool shiftHeld, bool isDown)
    {
        var shiftState = shiftHeld ? 1 : 0;

        var mapping = _userKeyMap[keyCode, shiftState];

        return mapping;
    }
}