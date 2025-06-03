namespace MLDComputing.Emulators.BeebBox.Ui.Views;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BBCSim._6502.Disassembler;
using BBCSim._6502.Engine.Communication;
using BBCSim._6502.Extensions;
using BBCSim._6502.Storage;
using BBCSim.Beeb;
using Domain;
using Extensions;
using Byte = BBCSim._6502.Storage.Byte;

public partial class MainWindow : INotifyPropertyChanged
{
    private const int Columns = 40;
    private const int Rows = 25;
    private const double CharWidth = 16; // adjust to your font’s cell width
    private const double CharHeight = 20; // adjust to your font’s cell height

    private readonly AutoResetEvent _event = new(false);

    private readonly double _max;

    private ObservableCollection<string> _codeLabelsData;

    private ObservableCollection<MemoryByte> _dataDisassembly;

    private ObservableCollection<string> _dataLabelsData;

    private ObservableCollection<Instruction> _disassembly;

    private long _instructionCount;
    private DispatcherTimer _renderTimer;

    private ObservableCollection<MemoryByte> _stackDisassembly;

    private Stopwatch _sw = new();

    private Typeface _teletextTypeface;

    private long _time;

    private double _value;

    private BeebEm _vm;

    public MainWindow()
    {
        InitializeComponent();

        DataContext = this;

        SetupMicro();

        DissassembleAll();

        ProcessRegisters();

        _max = Slider.Maximum;
    }

    public Dictionary<ushort, Instruction> DissDict { get; set; }

    public Dictionary<ushort, MemoryByte> DataDict { get; set; }

    public Stopwatch Sw
    {
        get => _sw;
        set
        {
            _sw = value;
            OnPropertyChanged();
        }
    }

    public long Time
    {
        get => _time;
        set
        {
            _time = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Instruction> Disassembly
    {
        get => _disassembly;
        set
        {
            _disassembly = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<MemoryByte> DataDisassembly
    {
        get => _dataDisassembly;
        set
        {
            _dataDisassembly = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<MemoryByte> StackDisassembly
    {
        get => _stackDisassembly;
        set
        {
            _stackDisassembly = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> CodeLabelsData
    {
        get => _codeLabelsData;
        set
        {
            _codeLabelsData = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> DataLabelsData
    {
        get => _dataLabelsData;
        set
        {
            _dataLabelsData = value;
            OnPropertyChanged();
        }
    }

    public bool IsStarted { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void SetupTeletextDisplay()
    {
        // Load the Teletext font
        _teletextTypeface = new Typeface(new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Teletext"),
            FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(20) // 50Hz
        };

        _renderTimer.Tick += (_, _) =>
        {
            RenderTeletextScreen(_vm.Memory.GetVideoBuffer());
            UpdateMachineAttributes();
        };
        _renderTimer.Start();
    }

    private void UpdateMachineAttributes()
    {
        FrameCount.Text = $"Frame Count: {_vm.FrameCount}";
        FrameRate.Text = $"Frame Rate: {_vm.FrameRate:F2} fps";
        InsCount.Text = $"Instruction Count: {_vm.CPU.InstructionCount}";
        CpuSpeed.Text = $"CPU Speed: {_vm.CpuSpeedMhz:F2} MHz";

        foreach (var stackFrame in StackDisassembly)
        {
            if (stackFrame.MemoryAddress == _vm.CPU.StackPointer + 0x100)
            {
                stackFrame.StackPointer = "-->";
            }
            else
            {
                stackFrame.StackPointer = string.Empty;
            }
        }
    }

    private void RenderTeletextScreen(byte[] buffer)
    {
        TeletextCanvas.Children.Clear();

        for (var row = 0; row < Rows; row++)
        {
            for (var col = 0; col < Columns; col++)
            {
                var ch = buffer[row * Columns + col];

                if (ch < 32 || ch > 127)
                {
                    continue;
                }

                var text = new FormattedText(
                    ((char)ch).ToString(),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    _teletextTypeface,
                    20, // Font size
                    Brushes.White, // Typical teletext color
                    VisualTreeHelper.GetDpi(this).PixelsPerDip
                );

                var textBlock = new TextBlock
                {
                    Text = ((char)ch).ToString(),
                    FontFamily = _teletextTypeface.FontFamily,
                    FontSize = 20,
                    Foreground = Brushes.White
                };

                Canvas.SetLeft(textBlock, col * CharWidth);
                Canvas.SetTop(textBlock, row * CharHeight);
                TeletextCanvas.Children.Add(textBlock);
            }
        }
    }

    private void DissassembleAll()
    {
        GetStackDisassembly();

        GetMemoryDisassembly();

        GetDisassembly();

        UpdateMachineAttributes();
    }

    private void SetupMicro()
    {
        _vm = new BeebEm();
        var osPath = Path.Combine(AppContext.BaseDirectory, "roms", string.Intern("os12.rom"));
        var basicPath = Path.Combine(AppContext.BaseDirectory, "roms", "basic2.rom");

        _vm.LoadRoms(osPath, basicPath);

        RenderTeletextScreen(_vm.Memory.GetVideoBuffer());

        FitTeletextScreen();
    }

    private void FitTeletextScreen()
    {
        TeletextCanvas.Width = Columns * CharWidth;
        TeletextCanvas.Height = Rows * CharHeight;
    }

    private void GetDisassembly()
    {
        var dis = Disassembler.Build();

        var comments = Helper.GetComments();

        var displayInstructions = Helper.GetCodeLabelMap().SelectMany(pair =>
            dis.Disassemble(_vm.Memory.ReadByte, pair.Key, pair.Value.End, 16)).Select(i => new Instruction
        {
            Mnemonic = i.Definition?.Mnemonic,
            Argument = i.Argument,
            ErrorMessage = i.ErrorMessage,
            MemoryAddress = (ushort)i.MemoryAddress,
            AddressHex = i.MemoryAddress.ToString("X4"),
            LabelName = i.OSLabel ?? string.Empty,
            Description = comments.ContainsKey((ushort)i.MemoryAddress)
                ? comments[(ushort)i.MemoryAddress]
                : string.Empty
        });

        Disassembly = new ObservableCollection<Instruction>(displayInstructions);

        DissDict = Disassembly.ToDictionary(k => k.MemoryAddress, v => v);

        CodeLabelsData =
            new ObservableCollection<string>(Disassembly.Where(d => !string.IsNullOrWhiteSpace(d.LabelName))
                .Select(d => d.LabelName));
    }

    private void GetMemoryDisassembly()
    {
        var dis = MemoryDisassembler.Build();

        var displayInstructions = Helper.GetDataLabelMap().SelectMany(pair =>
            dis.Disassemble(_vm.Memory.ReadByte, pair.Key, pair.Value.End, 16)).Select(i => new MemoryByte
        {
            MemoryAddress = (ushort)i.MemoryAddress,
            AddressHex = i.MemoryAddress.ToString("X4"),
            LabelName = i.OSLabel ?? string.Empty,
            Value = i.ActualOpCode,
            ValueHex = i.ActualOpCode.ToString("X4"),
            ValueBin = Convert.ToString(i.ActualOpCode, 2).PadLeft(8, '0'),
            Character = i.ActualOpCode.IsAsciiPrintable() ? (char)i.ActualOpCode : '-'
        });

        DataDisassembly = new ObservableCollection<MemoryByte>(displayInstructions);

        DataDict = DataDisassembly.ToDictionary(k => k.MemoryAddress, v => v);

        DataLabelsData =
            new ObservableCollection<string>(DataDisassembly.Where(d => !string.IsNullOrWhiteSpace(d.LabelName))
                .Select(d => d.LabelName));
    }

    private void GetStackDisassembly()
    {
        var dis = StackDisassembler.Build();

        var displayInstructions = Helper.GetStackMap().SelectMany(pair =>
            dis.Disassemble(_vm.Memory.ReadByte, pair.Key, pair.Value.End, 16)).Select(i => new MemoryByte
        {
            MemoryAddress = (ushort)i.MemoryAddress,
            AddressHex = i.MemoryAddress.ToString("X4"),
            LabelName = i.OSLabel ?? string.Empty,
            Value = i.ActualOpCode,
            ValueHex = i.ActualOpCode.ToString("X4"),
            ValueBin = Convert.ToString(i.ActualOpCode, 2).PadLeft(8, '0'),
            Character = i.ActualOpCode.IsAsciiPrintable() ? (char)i.ActualOpCode : '-'
        });

        StackDisassembly = new ObservableCollection<MemoryByte>(displayInstructions);
    }

    private void ProcessRegisters()
    {
        var cpu = _vm.CPU;

        ACC.Text = cpu.Accumulator.ToString("X2") + " h";
        ACCD.Text = cpu.Accumulator.ToString();

        PC.Text = cpu.ProgramCounter.ToString("X4") + " h";
        PCD.Text = cpu.ProgramCounter.ToString();

        IX.Text = cpu.IX.ToString("X2") + " h";
        IXD.Text = cpu.IX.ToString();
        IY.Text = cpu.IY.ToString("X2") + " h";
        IYD.Text = cpu.IY.ToString();

        SP.Text = cpu.StackPointer.ToString("X2") + " h";
        SPD.Text = cpu.StackPointer.ToString();

        StatusN.Text = cpu.Status.GetBit((Byte)Statuses.Negative) == Bit.One ? "1" : "0";
        StatusV.Text = cpu.Status.GetBit((Byte)Statuses.Overflow) == Bit.One ? "1" : "0";
        StatusB.Text = cpu.Status.GetBit((Byte)Statuses.BreakCommand) == Bit.One ? "1" : "0";
        StatusD.Text = cpu.Status.GetBit((Byte)Statuses.DecimalMode) == Bit.One ? "1" : "0";
        StatusI.Text = cpu.Status.GetBit((Byte)Statuses.InterruptDisable) == Bit.One ? "1" : "0";
        StatusZ.Text = cpu.Status.GetBit((Byte)Statuses.Zero) == Bit.One ? "1" : "0";
        StatusC.Text = cpu.Status.GetBit((Byte)Statuses.Carry) == Bit.One ? "1" : "0";
    }

    private void MainWindow_KeyUp(object sender, KeyEventArgs e)
    {
        if (_vm.Keyboard.TryMapKey((int)e.Key, out var pos))
        {
            _vm.Keyboard.SetKeyState(pos.Row, pos.Column, false);
            e.Handled = true;

            TrickOsIntoThinkingAKeyHasBeenPressed();
        }
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (_vm.Keyboard.TryMapKey((int)e.Key, out var pos))
        {
            _vm.Keyboard.SetKeyState(pos.Row, pos.Column, true);
            e.Handled = true;

            TrickOsIntoThinkingAKeyHasBeenPressed();
        }
    }

    private void TrickOsIntoThinkingAKeyHasBeenPressed()
    {
        // Notify OS that a key is logically "active"
        _vm.Memory.WriteByte(0x00EC, 0x01); // or ED
        _vm.Memory.WriteByte(0x0242, 0xFF); // Ensure scanning is enabled
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void MenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        ErrorScreen.Text = string.Empty;
    }

    private void Slider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _event.Set();
        _value = e.NewValue;

        if (StepButton != null)
        {
            StepButton.IsEnabled = e.NewValue >= Slider.Maximum;
        }
    }

    private void StepButton_OnClick(object sender, RoutedEventArgs e)
    {
        _event.Set();
    }

    private void StartVM()
    {
        IsStarted = false;

        //   _vm.OnUpdateDisplay += UpdateDisplay;
        _vm.Keyboard.CapsLockChanged -= KeyboardCapsLockChanged;
        _vm.CPU.Execute -= CPUBeforeExecute;
        _vm.CPU.ProcessorError -= CPUProcessorError;

        _vm.Keyboard.CapsLockChanged += KeyboardCapsLockChanged;
        _vm.CPU.Execute += CPUBeforeExecute;
        _vm.CPU.ProcessorError += CPUProcessorError;

        KeyDown += MainWindow_KeyDown;
        KeyUp += MainWindow_KeyUp;

        _vm.Start();

        IsStarted = true;
    }


    private void KeyboardCapsLockChanged(object sender, CapsChangedLockEventArgs e)
    {
        Dispatcher.Invoke(() => { CapsLock.IsOn = e.IsOn; });
    }

    private void CPUProcessorError(object sender, ProcessorErrorEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            ErrorScreen.Text +=
                "=========Exception thrown by VM=============================" + Environment.NewLine;
            ErrorScreen.Text += e.ErrorMessage + Environment.NewLine;
            ErrorScreen.Text += $"Instruction: {e.Instruction.Code}" + Environment.NewLine;
            ErrorScreen.Text += "Registers: " + Environment.NewLine;
            ErrorScreen.Text += $"{e.Registers}" + Environment.NewLine;
            ErrorScreen.Text +=
                "=========End Exception thrown by VM=========================" + Environment.NewLine;
        });
    }

    private void CPUBeforeExecute(object sender, ExecuteEventArgs e)
    {
        _instructionCount++;

        Dispatcher.Invoke(() =>
        {
            var stopAfter = int.TryParse(StopAfter.Text, out var tl) ? tl : 0L;

            if (stopAfter > 0 && _instructionCount >= stopAfter)
            {
                _value = _max;
                Slider.Value = Slider.Maximum;
                StepButton.IsEnabled = true;
            }
        });

        Instruction currentItem = null;

        try
        {
            currentItem = DissDict[e.Address];
        }
        catch (Exception)
        {
            // Hide errors here
        }

        if (currentItem is { BreakPoint: true })
        {
            Dispatcher.Invoke(() =>
            {
                Slider.Value = Slider.Maximum;
                StepButton.IsEnabled = true;
            });

            _event.WaitOne();
        }

        Dispatcher.Invoke(() =>
        {
            if (!(_value > 0))
            {
                return;
            }

            ProcessRegisters();

            DissGrid.SelectedItem = currentItem;

            if (DissGrid.SelectedItem == null)
            {
                return;
            }

            DissGrid.ScrollIntoView(DissGrid.SelectedItem);
        });

        if (_value > 0)
        {
            if (_value >= _max)
            {
                _sw.Stop();
                _event.WaitOne();
                _sw.Start();
            }
            else
            {
                _event.WaitOne((int)_value);
            }
        }
    }

    private void StartButton_OnClick(object sender, RoutedEventArgs e)
    {
        _instructionCount = 0;
        InsCount.Text = _instructionCount.ToString();

        Task.Run(StartVM);
    }

    private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
    {
        _vm.CPU.Execute -= CPUBeforeExecute;
        DissassembleAll();
        _vm.CPU.Execute += CPUBeforeExecute;
    }

    private void HeaderClick(object sender, RoutedEventArgs e)
    {
        if (((DataGridColumnHeader)e.Source).DisplayIndex == 0)
        {
            foreach (var instr in Disassembly)
            {
                instr.BreakPoint = false;
            }
        }
    }

    private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Search.Text))
        {
            return;
        }

        foreach (var drv in (ObservableCollection<Instruction>)DissGrid.ItemsSource)
        {
            if (drv.AddressHex.Contains(Search.Text, StringComparison.OrdinalIgnoreCase)
                || drv.LabelName.Contains(Search.Text, StringComparison.OrdinalIgnoreCase))
            {
                // This is the data row view record you want...
                DissGrid.SelectedItem = drv;
                DissGrid.ScrollIntoView(DissGrid.SelectedItem);
                return;
            }
        }
    }

    private void Label_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach (var drv in (ObservableCollection<Instruction>)DissGrid.ItemsSource)
        {
            if (drv.LabelName.Contains(CodeLabels.SelectedValue.ToString()!, StringComparison.OrdinalIgnoreCase))
            {
                // This is the data row view record you want...
                DissGrid.SelectedItem = drv;
                DissGrid.ScrollIntoView(DissGrid.SelectedItem);
                return;
            }
        }
    }

    private void SearchData_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SearchData.Text))
        {
            return;
        }

        foreach (var drv in (ObservableCollection<MemoryByte>)DataTableGrid.ItemsSource)
        {
            if (drv.AddressHex.Contains(SearchData.Text, StringComparison.OrdinalIgnoreCase)
                || drv.LabelName.Contains(SearchData.Text, StringComparison.OrdinalIgnoreCase))
            {
                // This is the data row view record you want...
                DataTableGrid.SelectedItem = drv;
                DataTableGrid.ScrollIntoView(DataTableGrid.SelectedItem);
                return;
            }
        }
    }

    private void DataLabelsData_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach (var drv in (ObservableCollection<MemoryByte>)DataTableGrid.ItemsSource)
        {
            if (drv.LabelName.Contains(DataLabels.SelectedValue.ToString()!, StringComparison.OrdinalIgnoreCase))
            {
                // This is the data row view record you want...
                DataTableGrid.SelectedItem = drv;
                DataTableGrid.ScrollIntoView(DataTableGrid.SelectedItem);
                return;
            }
        }
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        Keyboard.Focus(this);

        SetupTeletextDisplay();
    }

    private void EnableProcessorEvents_OnChecked(object sender, RoutedEventArgs e)
    {
        _vm.CPU.EnableProcessorEvents = EnableProcessorEvents.IsChecked!.Value;
    }
}