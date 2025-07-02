namespace MLDComputing.Emulators.BeebBox.Ui.Domain;

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class MemoryByte : INotifyPropertyChanged
{
    private string _addressHex;

    private char _character;

    private string _labelName;

    private ushort _memoryAddress;

    private string _stackPointer;

    private byte _value;

    private string _valueBin;

    private string _valueHex;

    public string StackPointer
    {
        get => _stackPointer;
        set
        {
            _stackPointer = value;
            OnPropertyChanged();
        }
    }

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

    public byte Value
    {
        get => _value;
        set
        {
            _value = value;
            OnPropertyChanged();
        }
    }


    public string ValueHex
    {
        get => _valueHex;
        set
        {
            _valueHex = value;
            OnPropertyChanged();
        }
    }


    public string ValueBin
    {
        get => _valueBin;
        set
        {
            _valueBin = value;
            OnPropertyChanged();
        }
    }

    public char Character
    {
        get => _character;
        set
        {
            _character = value;
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

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}