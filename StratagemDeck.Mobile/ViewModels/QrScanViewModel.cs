using System.ComponentModel;
using System.Runtime.CompilerServices;
using HD2Companion.Mobile.Services;

namespace HD2Companion.Mobile.ViewModels;

public class QrScanViewModel : INotifyPropertyChanged
{
    private string _status = "Point camera at QR code";
    private bool _isScanning = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public bool IsScanning
    {
        get => _isScanning;
        set { _isScanning = value; OnPropertyChanged(); }
    }

    public QrScanViewModel(SessionService session)
    {
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
