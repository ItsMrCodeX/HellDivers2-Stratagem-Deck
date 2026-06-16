using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Net.Wifi;

namespace HD2Companion.Mobile;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ScreenOrientation = ScreenOrientation.Landscape,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private WifiManager.MulticastLock? _multicastLock;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var wifiManager = (WifiManager?)GetSystemService(WifiService);
        _multicastLock = wifiManager?.CreateMulticastLock("HD2Companion");
        _multicastLock?.Acquire();
    }

    protected override void OnDestroy()
    {
        _multicastLock?.Release();
        base.OnDestroy();
    }
}
