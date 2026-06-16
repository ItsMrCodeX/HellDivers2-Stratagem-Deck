using HD2Companion.Mobile.ViewModels;

namespace HD2Companion.Mobile.Pages;

public partial class SetupPage : ContentPage
{
    private readonly SetupViewModel _vm;

    public SetupPage()
    {
        InitializeComponent();
        BindingContext = _vm = App.GetService<SetupViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }
}
