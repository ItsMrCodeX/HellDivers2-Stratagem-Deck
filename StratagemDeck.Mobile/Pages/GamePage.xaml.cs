using StratagemDeck.Mobile.Services;
using StratagemDeck.Mobile.ViewModels;

namespace StratagemDeck.Mobile.Pages;

public partial class GamePage : ContentPage
{
    private readonly GameViewModel _vm;

    public GamePage()
    {
        InitializeComponent();
        BindingContext = _vm = App.GetService<GameViewModel>();
        SwipeUpWatcher.SwipedUp += OnSwipedUp;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SwipeUpWatcher.IsEnabled = true;
        try
        {
            await _vm.InitializeAsync();
            _vm.UpdateConnectionStatus();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        SwipeUpWatcher.IsEnabled = false;
    }

    private async void OnSwipedUp()
    {
        try
        {
            if (Shell.Current.CurrentPage is GamePage)
                await Shell.Current.GoToAsync("//pad");
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }
}
