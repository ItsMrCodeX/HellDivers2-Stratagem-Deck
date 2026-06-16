using HD2Companion.Mobile.ViewModels;

namespace HD2Companion.Mobile.Pages;

public partial class GamePage : ContentPage
{
    private readonly GameViewModel _vm;

    public GamePage()
    {
        InitializeComponent();
        BindingContext = _vm = App.GetService<GameViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
        _vm.UpdateConnectionStatus();
    }
}
