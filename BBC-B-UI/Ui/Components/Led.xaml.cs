namespace MLDComputing.Emulators.BeebBox.Ui.Components;

using System.Windows;
using System.Windows.Controls;

/// <summary>
///     Interaction logic for Led.xaml
/// </summary>
public partial class Led : UserControl
{
    public static readonly DependencyProperty IsOnProperty =
        DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(Led), new PropertyMetadata(false));

    public Led()
    {
        InitializeComponent();
        DataContext = this;
    }

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }
}