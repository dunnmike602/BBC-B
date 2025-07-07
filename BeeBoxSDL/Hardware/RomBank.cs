namespace BeeBoxSDL.Hardware.MLDComputing.Emulators.BBCSim.Beeb.Hardware;

public class RomBank
{
    private readonly byte[][] _banks = new byte[16][];
    private int _activeBank;

    public void LoadBank(int bankNumber, byte[] romData)
    {
        if (bankNumber is < 0 or > 15)
            throw new ArgumentOutOfRangeException(nameof(bankNumber));

        if (romData.Length != 0x4000)
            throw new ArgumentException("ROM must be exactly 16KB (0x4000)");

        _banks[bankNumber] = romData;
    }

    public void SetActiveBank(int bankNumber)
    {
        if (bankNumber is < 0 or > 15)
            return; // ignore invalid bank numbers for safety

        _activeBank = bankNumber;
    }

    public int GetActiveBank()
    {
        return _activeBank;
    }

    public byte Read(ushort address)
    {
        if (address is < 0x8000 or > 0xBFFF)
            throw new ArgumentOutOfRangeException(nameof(address), "ROM read out of bounds");

        var bank = _banks[_activeBank];
        return bank?[address - 0x8000] ?? 0xFF; // floating bus value
    }
}