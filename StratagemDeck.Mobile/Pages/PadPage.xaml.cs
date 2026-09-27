using StratagemDeck.Mobile.ViewModels;

namespace StratagemDeck.Mobile.Pages;

public partial class PadPage : ContentPage
{
    private readonly GameViewModel _vm;
    private bool _isLeaving;

    public PadPage()
    {
        InitializeComponent();
        BindingContext = _vm = App.GetService<GameViewModel>();
        Pad.CloseCommand = new Command(() => _ = GoToGameAsync());
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isLeaving = false;
        _vm.RefreshPadState();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _ = _vm.ReleaseHeldKeysAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = GoToGameAsync();
        return true;
    }

    private async Task GoToGameAsync()
    {
        if (_isLeaving) return;
        _isLeaving = true;

        try
        {
            await Shell.Current.GoToAsync("//game");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            _isLeaving = false;
        }
    }
}
