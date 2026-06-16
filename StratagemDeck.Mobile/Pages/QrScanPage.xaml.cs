using HD2Companion.Mobile.ViewModels;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace HD2Companion.Mobile.Pages;

public partial class QrScanPage : ContentPage
{
    private readonly QrScanViewModel _vm;
    private readonly SettingsViewModel _settingsVm;

    public QrScanPage()
    {
        InitializeComponent();
        BindingContext = _vm = App.GetService<QrScanViewModel>();
        _settingsVm = App.GetService<SettingsViewModel>();

        BarcodeView.Options = new BarcodeReaderOptions
        {
            AutoRotate = true,
            Multiple = false,
            TryHarder = true,
            TryInverted = true
        };
        BarcodeView.CameraLocation = CameraLocation.Rear;

        Loaded += async (_, _) => await CheckPermissions();

        BarcodeView.BarcodesDetected += async (_, e) =>
        {
            var result = e.Results?.FirstOrDefault();
            if (result == null || !_vm.IsScanning) return;
            _vm.IsScanning = false;

            var text = result.Value;
            var parts = text.Split(':');
            if (parts.Length >= 2)
            {
                var ip = parts[0];
                var pin = string.Join(":", parts.Skip(1));
                _vm.Status = $"Connecting to {ip}...";
                await _settingsVm.HandleQrScanResult(ip, pin);
            }
            else
            {
                _vm.Status = "Invalid QR code";
                _vm.IsScanning = true;
            }
        };
    }

    private async void OnCancel(object? sender, EventArgs e)
    {
        _vm.IsScanning = false;
        await Shell.Current.GoToAsync("..");
    }

    private async Task CheckPermissions()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status != PermissionStatus.Granted)
        {
            _vm.Status = "Camera permission required";
            await Shell.Current.DisplayAlert("Permission", "Camera access is needed to scan QR codes", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }
}
