using StratagemDeck.Mobile.ViewModels;

namespace StratagemDeck.Mobile.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _vm;

    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = _vm = App.GetService<SettingsViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }
}
