using StratagemDeck.Mobile.Pages;
using StratagemDeck.Mobile.Services;
using StratagemDeck.Mobile.ViewModels;
using ZXing.Net.Maui.Controls;

namespace StratagemDeck.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<StratagemDataService>();
        builder.Services.AddSingleton<PreferencesService>();
        builder.Services.AddSingleton<UdpDiscoveryService>();
        builder.Services.AddSingleton<StratagemSender>();
        builder.Services.AddSingleton<SessionService>();

        builder.Services.AddSingleton<SetupViewModel>();
        builder.Services.AddSingleton<GameViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddSingleton<QrScanViewModel>();

        builder.Services.AddTransient<SetupPage>();
        builder.Services.AddTransient<GamePage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<QrScanPage>();

        return builder.Build();
    }
}
