using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Net.Wifi;
using Android.Views;
using StratagemDeck.Mobile.Services;

namespace StratagemDeck.Mobile;

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
        _multicastLock = wifiManager?.CreateMulticastLock("StratagemDeck");
        _multicastLock?.Acquire();
    }

    public override bool DispatchTouchEvent(MotionEvent? e)
    {
        if (e != null)
        {
            var screenHeight = Window?.DecorView?.Height
                ?? Resources?.DisplayMetrics?.HeightPixels
                ?? 0;

            SwipeUpWatcher.Track(e, screenHeight);
        }

        return base.DispatchTouchEvent(e);
    }

    protected override void OnDestroy()
    {
        _multicastLock?.Release();
        base.OnDestroy();
    }
}
