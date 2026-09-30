using System.Windows.Input;

namespace StratagemDeck.Mobile.Controls;

public partial class InputPadView : ContentView
{
    public static readonly BindableProperty CtrlEnabledProperty = BindableProperty.Create(
        nameof(CtrlEnabled), typeof(bool), typeof(InputPadView), true,
        BindingMode.TwoWay, propertyChanged: OnCtrlEnabledChanged);

    public static readonly BindableProperty IsConnectedProperty = BindableProperty.Create(
        nameof(IsConnected), typeof(bool), typeof(InputPadView), false);

    public static readonly BindableProperty InputStatusProperty = BindableProperty.Create(
        nameof(InputStatus), typeof(string), typeof(InputPadView), string.Empty);

    public static readonly BindableProperty LastInputProperty = BindableProperty.Create(
        nameof(LastInput), typeof(string), typeof(InputPadView), string.Empty,
        propertyChanged: OnLastInputChanged);

    public static readonly BindableProperty KeyDownCommandProperty = BindableProperty.Create(
        nameof(KeyDownCommand), typeof(ICommand), typeof(InputPadView));

    public static readonly BindableProperty KeyUpCommandProperty = BindableProperty.Create(
        nameof(KeyUpCommand), typeof(ICommand), typeof(InputPadView));

    public static readonly BindableProperty ToggleCtrlCommandProperty = BindableProperty.Create(
        nameof(ToggleCtrlCommand), typeof(ICommand), typeof(InputPadView));

    public static readonly BindableProperty CloseCommandProperty = BindableProperty.Create(
        nameof(CloseCommand), typeof(ICommand), typeof(InputPadView));

    private const double KeySpacing = 8;
    private const double MinKeySize = 48;
    private const double MaxKeySize = 320;

    public InputPadView()
    {
        InitializeComponent();
        RootGrid.BindingContext = this;
        PadArea.SizeChanged += OnPadAreaSizeChanged;
    }

    private void OnPadAreaSizeChanged(object? sender, EventArgs e)
    {
        var width = PadArea.Width;
        var height = PadArea.Height;
        if (width <= 0 || height <= 0) return;

        var size = Math.Min(
            (width - 2 * KeySpacing) / 3,
            (height - KeySpacing) / 2);
        size = Math.Clamp(size, MinKeySize, MaxKeySize);

        var fontSize = Math.Clamp(size * 0.32, 20, 64);
        var cornerRadius = (int)Math.Clamp(size * 0.16, 10, 36);

        foreach (var child in ArrowCluster.Children)
        {
            if (child is Button key)
            {
                key.WidthRequest = size;
                key.HeightRequest = size;
                key.FontSize = fontSize;
                key.CornerRadius = cornerRadius;
            }
        }
    }

    public ICommand AbsorbTapCommand { get; } = new Command(() => { });

    private void OnKeyPressed(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string key })
            KeyDownCommand?.Execute(key);
    }

    private void OnKeyReleased(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string key })
            KeyUpCommand?.Execute(key);
    }

    public bool CtrlEnabled
    {
        get => (bool)GetValue(CtrlEnabledProperty);
        set => SetValue(CtrlEnabledProperty, value);
    }

    public bool IsConnected
    {
        get => (bool)GetValue(IsConnectedProperty);
        set => SetValue(IsConnectedProperty, value);
    }

    public string InputStatus
    {
        get => (string?)GetValue(InputStatusProperty) ?? string.Empty;
        set => SetValue(InputStatusProperty, value);
    }

    public string LastInput
    {
        get => (string?)GetValue(LastInputProperty) ?? string.Empty;
        set => SetValue(LastInputProperty, value);
    }

    public string LastInputGlyph => LastInput switch
    {
        "up" => "▲",
        "down" => "▼",
        "left" => "◀",
        "right" => "▶",
        _ => "·"
    };

    public ICommand? KeyDownCommand
    {
        get => (ICommand?)GetValue(KeyDownCommandProperty);
        set => SetValue(KeyDownCommandProperty, value);
    }

    public ICommand? KeyUpCommand
    {
        get => (ICommand?)GetValue(KeyUpCommandProperty);
        set => SetValue(KeyUpCommandProperty, value);
    }

    public ICommand? ToggleCtrlCommand
    {
        get => (ICommand?)GetValue(ToggleCtrlCommandProperty);
        set => SetValue(ToggleCtrlCommandProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public string CtrlStateLabel => CtrlEnabled ? "CTRL ON" : "CTRL OFF";

    private static void OnCtrlEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((InputPadView)bindable).OnPropertyChanged(nameof(CtrlStateLabel));
    }

    private static void OnLastInputChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((InputPadView)bindable).OnPropertyChanged(nameof(LastInputGlyph));
    }
}