namespace StratagemDeck.Mobile.Services;

public static class SwipeUpWatcher
{
    public static bool IsEnabled { get; set; }

    public static event Action? SwipedUp;

#if ANDROID
    private const float MinFallbackDistanceDp = 60f;
    private const float MinScreenHeightFraction = 0.5f;
    private const int MaxDurationMs = 1200;

    private static float _startX;
    private static float _startY;
    private static long _startTime;
    private static bool _tracking;

    private static float Density =>
        Android.App.Application.Context.Resources?.DisplayMetrics?.Density ?? 1f;

    public static void Track(Android.Views.MotionEvent e, int screenHeightPx)
    {
        if (!IsEnabled)
        {
            _tracking = false;
            return;
        }

        switch (e.ActionMasked)
        {
            case Android.Views.MotionEventActions.Down:
                _startX = e.GetX();
                _startY = e.GetY();
                _startTime = Android.OS.SystemClock.ElapsedRealtime();
                _tracking = true;
                break;

            case Android.Views.MotionEventActions.Move:
                if (!_tracking) return;

                var dy = _startY - e.GetY();
                var dx = Math.Abs(e.GetX() - _startX);
                var elapsed = Android.OS.SystemClock.ElapsedRealtime() - _startTime;

                if (elapsed > MaxDurationMs)
                {
                    _tracking = false;
                    return;
                }

                var minDistance = screenHeightPx > 0
                    ? screenHeightPx * MinScreenHeightFraction
                    : MinFallbackDistanceDp * Density;

                if (dy >= minDistance && dy > dx * 1.5f)
                {
                    _tracking = false;
                    SwipedUp?.Invoke();
                }
                break;

            case Android.Views.MotionEventActions.Up:
            case Android.Views.MotionEventActions.Cancel:
                _tracking = false;
                break;
        }
    }
#endif
}
