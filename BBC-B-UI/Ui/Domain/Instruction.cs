namespace MLDComputing.Emulators.BeebBox.Ui.Domain;

using System.ComponentModel;
using System.Runtime.CompilerServices;

public class Instruction : INotifyPropertyChanged
{
    private string _addressHex;

    private string _argument;

    private bool _breakPoint;

    private string _description;

    private string _errorMessage;

    private string _labelName;

    private ushort _memoryAddress;

    private string _mnemonic;

    public ushort MemoryAddress
    {
        get => _memoryAddress;
        set
        {
            _memoryAddress = value;
            OnPropertyChanged();
        }
    }

    public string AddressHex
    {
        get => _addressHex;
        set
        {
            _addressHex = value;
            OnPropertyChanged();
        }
    }

    public string Mnemonic
    {
        get => _mnemonic;
        set
        {
            _mnemonic = value;
            OnPropertyChanged();
        }
    }

    public string Argument
    {
        get => _argument;
        set
        {
            _argument = value;
            OnPropertyChanged();
        }
    }

    public string LabelName
    {
        get => _labelName;
        set
        {
            _labelName = value;
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            _description = value;
            OnPropertyChanged();
        }
    }

    public bool BreakPoint
    {
        get => _breakPoint;
        set
        {
            _breakPoint = value;
            OnPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}