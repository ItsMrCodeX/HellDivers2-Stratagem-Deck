namespace HD2Companion.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("qrscan", typeof(Pages.QrScanPage));
    }
}
