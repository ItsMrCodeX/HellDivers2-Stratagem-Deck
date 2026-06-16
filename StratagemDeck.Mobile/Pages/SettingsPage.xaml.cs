using HD2Companion.Mobile.ViewModels;

namespace HD2Companion.Mobile.Pages;

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
