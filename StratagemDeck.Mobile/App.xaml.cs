namespace StratagemDeck.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    protected override void OnSleep()
    {
        base.OnSleep();

        try { _ = GetService<ViewModels.GameViewModel>().ReleaseHeldKeysAsync(); }
        catch { }
    }

    public static T GetService<T>() where T : notnull
    {
        if (IPlatformApplication.Current?.Services is IServiceProvider sp)
            return sp.GetRequiredService<T>();
        throw new InvalidOperationException($"Service {typeof(T).Name} not available");
    }
}
