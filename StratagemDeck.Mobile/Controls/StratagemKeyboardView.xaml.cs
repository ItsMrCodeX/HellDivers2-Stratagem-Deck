using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;

namespace StratagemDeck.Mobile.Controls;

public partial class StratagemKeyboardView : ContentView
{
    private const string AllLetters = "ALL";
    private const string BackKey = "BACK";
    private const string ClearKey = "CLEAR";
    private const string SpaceKey = "SPACE";

    public static readonly BindableProperty QueryTextProperty = BindableProperty.Create(
        nameof(QueryText), typeof(string), typeof(StratagemKeyboardView), string.Empty,
        BindingMode.TwoWay, propertyChanged: OnQueryTextChanged);

    public static readonly BindableProperty SelectedLetterProperty = BindableProperty.Create(
        nameof(SelectedLetter), typeof(string), typeof(StratagemKeyboardView), AllLetters,
        BindingMode.TwoWay, propertyChanged: OnSelectedLetterChanged);

    public static readonly BindableProperty ResultsProperty = BindableProperty.Create(
        nameof(Results), typeof(IEnumerable), typeof(StratagemKeyboardView),
        propertyChanged: OnResultsChanged);

    public static readonly BindableProperty TargetLabelProperty = BindableProperty.Create(
        nameof(TargetLabel), typeof(string), typeof(StratagemKeyboardView), string.Empty);

    public static readonly BindableProperty ResultTappedCommandProperty = BindableProperty.Create(
        nameof(ResultTappedCommand), typeof(ICommand), typeof(StratagemKeyboardView));

    public static readonly BindableProperty CloseCommandProperty = BindableProperty.Create(
        nameof(CloseCommand), typeof(ICommand), typeof(StratagemKeyboardView));

    private bool _resultsEmpty = true;

    public StratagemKeyboardView()
    {
        InitializeComponent();
        KeyTappedCommand = new Command<string>(OnKeyTapped);
        RootGrid.BindingContext = this;
        UpdateEmptyState();
    }

    public IReadOnlyList<string> Letters { get; } = BuildLetters();

    public ICommand KeyTappedCommand { get; }

    public ICommand AbsorbTapCommand { get; } = new Command(() => { });

    public string QueryText
    {
        get => (string?)GetValue(QueryTextProperty) ?? string.Empty;
        set => SetValue(QueryTextProperty, value);
    }

    public string? SelectedLetter
    {
        get => (string?)GetValue(SelectedLetterProperty);
        set => SetValue(SelectedLetterProperty, value);
    }

    public IEnumerable? Results
    {
        get => (IEnumerable?)GetValue(ResultsProperty);
        set => SetValue(ResultsProperty, value);
    }

    public string TargetLabel
    {
        get => (string?)GetValue(TargetLabelProperty) ?? string.Empty;
        set => SetValue(TargetLabelProperty, value);
    }

    public ICommand? ResultTappedCommand
    {
        get => (ICommand?)GetValue(ResultTappedCommandProperty);
        set => SetValue(ResultTappedCommandProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public bool HasQuery => QueryText.Length > 0;

    public string EmptyHint => HasQuery || !IsAllLettersSelected
        ? "No matches"
        : "Type or pick a letter";

    public bool ShowEmptyHint => _resultsEmpty;

    private bool IsAllLettersSelected =>
        string.IsNullOrEmpty(SelectedLetter)
        || string.Equals(SelectedLetter, AllLetters, StringComparison.OrdinalIgnoreCase);

    private void OnKeyTapped(string? key)
    {
        if (string.IsNullOrEmpty(key)) return;

        switch (key)
        {
            case BackKey:
                if (QueryText.Length > 0)
                    QueryText = QueryText[..^1];
                break;
            case ClearKey:
                QueryText = string.Empty;
                break;
            case SpaceKey:
                QueryText += " ";
                break;
            default:
                QueryText += key;
                break;
        }
    }

    private static void OnQueryTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (StratagemKeyboardView)bindable;
        view.OnPropertyChanged(nameof(HasQuery));
        view.OnPropertyChanged(nameof(EmptyHint));
    }

    private static void OnSelectedLetterChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((StratagemKeyboardView)bindable).OnPropertyChanged(nameof(EmptyHint));
    }

    private static void OnResultsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (StratagemKeyboardView)bindable;

        if (oldValue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= view.OnResultsCollectionChanged;
        if (newValue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += view.OnResultsCollectionChanged;

        view.UpdateEmptyState();
    }

    private void OnResultsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateEmptyState();

    private void UpdateEmptyState()
    {
        _resultsEmpty = Results == null || !Results.Cast<object>().Any();
        OnPropertyChanged(nameof(ShowEmptyHint));
    }

    private static IReadOnlyList<string> BuildLetters()
    {
        var letters = new List<string> { AllLetters };
        for (var c = 'A'; c <= 'Z'; c++)
            letters.Add(c.ToString());
        return letters;
    }
}
