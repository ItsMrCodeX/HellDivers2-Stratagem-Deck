using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using StratagemDeck.Mobile.Models;
using StratagemDeck.Mobile.Services;

namespace StratagemDeck.Mobile.ViewModels;

public class GameViewModel : INotifyPropertyChanged
{
    private readonly SessionService _session;
    private readonly StratagemSender _sender;
    private readonly PreferencesService _prefs;
    private bool _isSending;
    private string _status = string.Empty;
    private bool _ctrlEnabled;
    private string _inputStatus = string.Empty;
    private string _lastInput = string.Empty;
    private ImageSource? _warmupIcon;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand TapSlotCommand { get; }
    public ICommand SendMissionStratagemCommand { get; }
    public ICommand ToggleCtrlCommand { get; }
    public ICommand SendInputCommand { get; }

    public ObservableCollection<LoadoutSlot> Slots => _session.Slots;

    public ImageSource? Slot0Icon => Slots.Count > 0 ? Slots[0].SlotIcon : null;
    public string Slot0Name => Slots.Count > 0 ? Slots[0].SlotName : "—";
    public ImageSource? Slot1Icon => Slots.Count > 1 ? Slots[1].SlotIcon : null;
    public string Slot1Name => Slots.Count > 1 ? Slots[1].SlotName : "—";
    public ImageSource? Slot2Icon => Slots.Count > 2 ? Slots[2].SlotIcon : null;
    public string Slot2Name => Slots.Count > 2 ? Slots[2].SlotName : "—";
    public ImageSource? Slot3Icon => Slots.Count > 3 ? Slots[3].SlotIcon : null;
    public string Slot3Name => Slots.Count > 3 ? Slots[3].SlotName : "—";
    public ObservableCollection<Stratagem> MissionStrats => Slots.Count > 4 ? Slots[4].MissionStrats : new ObservableCollection<Stratagem>();

    private void NotifySlotsChanged()
    {
        OnPropertyChanged(nameof(Slot0Icon));
        OnPropertyChanged(nameof(Slot0Name));
        OnPropertyChanged(nameof(Slot1Icon));
        OnPropertyChanged(nameof(Slot1Name));
        OnPropertyChanged(nameof(Slot2Icon));
        OnPropertyChanged(nameof(Slot2Name));
        OnPropertyChanged(nameof(Slot3Icon));
        OnPropertyChanged(nameof(Slot3Name));
        OnPropertyChanged(nameof(MissionStrats));
    }

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public bool IsConnected => _session.IsConnected;

    public bool CtrlEnabled
    {
        get => _ctrlEnabled;
        private set
        {
            if (_ctrlEnabled == value) return;
            _ctrlEnabled = value;
            OnPropertyChanged();
        }
    }

    public string InputStatus
    {
        get => _inputStatus;
        set { _inputStatus = value; OnPropertyChanged(); }
    }

    public string LastInput
    {
        get => _lastInput;
        private set { _lastInput = value; OnPropertyChanged(); }
    }

    public ImageSource? WarmupIcon
    {
        get => _warmupIcon;
        private set { _warmupIcon = value; OnPropertyChanged(); }
    }

    public GameViewModel(SessionService session, StratagemSender sender, PreferencesService prefs)
    {
        _session = session;
        _sender = sender;
        _prefs = prefs;
        _ctrlEnabled = prefs.GetCtrlInputs();

        TapSlotCommand = new Command<string>(async (idx) => await OnTapSlot(idx));
        SendMissionStratagemCommand = new Command<Stratagem>(async (s) => await OnSendMission(s));
        ToggleCtrlCommand = new Command(OnToggleCtrl);
        SendInputCommand = new Command<string>(async (key) => await OnSendInput(key));
        _session.OnConnectedChanged += () =>
        {
            OnPropertyChanged(nameof(IsConnected));

            if (!_session.IsConnected)
                _ = SetCtrlHeldAsync(false);
        };
        _session.OnLoadoutChanged += () =>
        {
            NotifySlotsChanged();
        };
        _session.OnIconsLoaded += () =>
        {
            NotifySlotsChanged();

            // Warm up the platform image loader (Glide on Android) once so its
            // one-time initialization does not stall the first search results.
            WarmupIcon ??= _session.GetAll().FirstOrDefault(s => s.IconSource != null)?.IconSource;
        };
    }

    public async Task InitializeAsync()
    {
        await _session.InitializeAsync();
        Status = _session.IsConnected ? "Connected" : "Not connected";
        _ = _session.EnsureIconsLoadedAsync();
    }

    private async Task OnTapSlot(string? indexStr)
    {
        if (!int.TryParse(indexStr, out var idx)) return;
        if (_isSending) return;

        var slot = Slots.FirstOrDefault(s => s.SlotIndex == idx);
        if (slot?.SelectedStratagem == null) return;

        await SendStratagem(slot.SelectedStratagem);
    }

    private async Task OnSendMission(Stratagem strat)
    {
        if (_isSending) return;
        await SendStratagem(strat);
    }

    private async Task SendStratagem(Stratagem strat)
    {
        if (!_session.IsConnected || string.IsNullOrEmpty(_session.ServerIp))
        {
            Status = "Not connected";
            return;
        }

        _isSending = true;
        try
        {
            Status = $"{strat.DisplayName}";
            await _sender.SendAsync(_session.ServerIp, _session.Pin, strat);
            Status = "Sent";
            await Task.Delay(1000);
            Status = "Connected";
        }
        catch (Exception ex)
        {
            Status = "Send failed";
            System.Diagnostics.Debug.WriteLine($"Send failed: {ex}");
        }
        finally
        {
            _isSending = false;
        }
    }

    public void UpdateConnectionStatus()
    {
        OnPropertyChanged(nameof(IsConnected));
    }

    public void RefreshPadState()
    {
        CtrlEnabled = _prefs.GetCtrlInputs();
        InputStatus = _session.IsConnected ? "Ready" : "Not connected";

        if (CtrlEnabled)
            _ = SetCtrlHeldAsync(true);
    }

    public Task ReleaseHeldKeysAsync() => SetCtrlHeldAsync(false);

    private void OnToggleCtrl()
    {
        CtrlEnabled = !CtrlEnabled;
        _prefs.SaveCtrlInputs(CtrlEnabled);
        InputStatus = CtrlEnabled ? "Ctrl held" : "Ctrl released";
        _ = SetCtrlHeldAsync(CtrlEnabled);
    }

    private async Task SetCtrlHeldAsync(bool hold)
    {
        if (string.IsNullOrEmpty(_session.ServerIp)) return;

        try
        {
            await _sender.SendKeyAsync(_session.ServerIp, _session.Pin, "ctrl", hold ? "down" : "up");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ctrl hold failed: {ex}");
        }
    }

    private async Task OnSendInput(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        if (!_session.IsConnected || string.IsNullOrEmpty(_session.ServerIp))
        {
            InputStatus = "Not connected";
            return;
        }

        InputStatus = $"Sent {key}";
        LastInput = key;

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
        catch { }

        try
        {
            if (CtrlEnabled)
                await _sender.SendKeyAsync(_session.ServerIp, _session.Pin, "ctrl", "down");

            await _sender.SendKeyAsync(_session.ServerIp, _session.Pin, key, "tap");
        }
        catch (Exception ex)
        {
            InputStatus = "Send failed";
            System.Diagnostics.Debug.WriteLine($"Input send failed: {ex}");
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
