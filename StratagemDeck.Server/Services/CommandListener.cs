using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using StratagemDeck.Server.Models;
using StratagemDeck.Server.Native;

namespace StratagemDeck.Server.Services;

public class CommandListener : IDisposable
{
    private readonly UdpClient _udp;
    private readonly PinManager _pinManager;
    private readonly CancellationTokenSource _cts = new();
    private readonly Channel<(string Key, string Action)> _keyQueue =
        Channel.CreateUnbounded<(string Key, string Action)>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
    private bool _receiving;

    public event Action<LogCategory, string>? OnStatusChanged;

    public CommandListener(PinManager pinManager, int port = 12345)
    {
        _pinManager = pinManager;
        _udp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
    }

    public void Start()
    {
        _ = ListenLoop(_cts.Token);
        _ = ProcessKeyLoop(_cts.Token);
        _ = WatchdogLoop(_cts.Token);
    }

    public void Stop()
    {
        _cts.Cancel();
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _udp.ReceiveAsync(ct);
                var json = Encoding.UTF8.GetString(result.Buffer);

                StratagemCommand? cmd;
                try
                {
                    cmd = JsonSerializer.Deserialize<StratagemCommand>(json);
                }
                catch (JsonException)
                {
                    OnStatusChanged?.Invoke(LogCategory.Error, $"Malformed message from {result.RemoteEndPoint.Address}");
                    continue;
                }

                if (cmd == null || string.IsNullOrEmpty(cmd.Type))
                    continue;

                switch (cmd.Type)
                {
                    case "ping":
                        await HandlePing(result.RemoteEndPoint, cmd);
                        break;
                    case "discover":
                        await HandleDiscover(result.RemoteEndPoint);
                        break;
                    case "stratagem":
                        await HandleStratagem(result.RemoteEndPoint, cmd);
                        break;
                    case "key":
                        HandleKey(result.RemoteEndPoint, cmd);
                        break;
                    default:
                        OnStatusChanged?.Invoke(LogCategory.Error, $"Unknown message type '{cmd.Type}' from {result.RemoteEndPoint.Address}");
                        break;
                }
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke(LogCategory.Error, $"Listener error: {ex.Message}");
            }
        }
    }

    private async Task HandleDiscover(IPEndPoint sender)
    {
        OnStatusChanged?.Invoke(LogCategory.Network, $"Discover from {sender.Address}:{sender.Port}");

        var msg = new DiscoveryMessage
        {
            PcName = Environment.MachineName,
            Pin = _pinManager.CurrentPin
        };
        var data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));
        await _udp.SendAsync(data, sender);

        OnStatusChanged?.Invoke(LogCategory.Network, $"Discovery response sent to {sender.Address}");
    }

    private async Task HandlePing(IPEndPoint sender, StratagemCommand ping)
    {
        if (!_pinManager.Validate(ping.Pin))
        {
            OnStatusChanged?.Invoke(LogCategory.Error, $"Invalid ping from {sender.Address}");
            return;
        }

        OnStatusChanged?.Invoke(LogCategory.Network, $"Ping from {sender.Address}");

        var pong = new PongMessage { PcName = Environment.MachineName };
        var data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pong));
        await _udp.SendAsync(data, sender);

        OnStatusChanged?.Invoke(LogCategory.Network, $"Pong sent to {sender.Address}");
    }

    private void HandleKey(IPEndPoint sender, StratagemCommand cmd)
    {
        if (!_pinManager.Validate(cmd.Pin))
        {
            OnStatusChanged?.Invoke(LogCategory.Error, $"Invalid key PIN from {sender.Address}");
            return;
        }

        if (string.IsNullOrEmpty(cmd.Key))
            return;

        var action = string.IsNullOrEmpty(cmd.Action) ? "tap" : cmd.Action;

        if (!_keyQueue.Writer.TryWrite((cmd.Key, action)))
            OnStatusChanged?.Invoke(LogCategory.Error, $"Input queue full - dropping {cmd.Key}");
    }

    private async Task ProcessKeyLoop(CancellationToken ct)
    {
        try
        {
            await foreach (var (key, action) in _keyQueue.Reader.ReadAllAsync(ct))
            {
                try
                {
                    switch (action)
                    {
                        case "down":
                            OnStatusChanged?.Invoke(LogCategory.Stratagem, $"Hold: {key}");
                            KeyInjector.SetKeyHeld(key, true);
                            break;
                        case "up":
                            OnStatusChanged?.Invoke(LogCategory.Stratagem, $"Release: {key}");
                            KeyInjector.SetKeyHeld(key, false);
                            break;
                        default:
                            OnStatusChanged?.Invoke(LogCategory.Stratagem, $"Input: {key}");
                            await KeyInjector.ExecuteKey(key);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    OnStatusChanged?.Invoke(LogCategory.Error, $"Input failed: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task WatchdogLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);

                if (KeyInjector.HasHeldKeys
                    && DateTime.UtcNow - KeyInjector.LastActivity > TimeSpan.FromSeconds(60))
                {
                    KeyInjector.ReleaseHeldKeys();
                    OnStatusChanged?.Invoke(LogCategory.Info, "Released held keys (idle)");
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task HandleStratagem(IPEndPoint sender, StratagemCommand cmd)
    {
        if (!_pinManager.Validate(cmd.Pin))
        {
            OnStatusChanged?.Invoke(LogCategory.Error, $"Invalid stratagem PIN from {sender.Address}");
            return;
        }

        if (_receiving)
        {
            OnStatusChanged?.Invoke(LogCategory.Error, $"Busy - ignoring {cmd.Name}");
            return;
        }

        _receiving = true;
        var sw = Stopwatch.StartNew();
        OnStatusChanged?.Invoke(LogCategory.Stratagem, $"Executing: {cmd.Name}");

        try
        {
            await KeyInjector.ExecuteSequence(cmd.Keys);
            sw.Stop();
            OnStatusChanged?.Invoke(LogCategory.Success, $"Done ({sw.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            sw.Stop();
            OnStatusChanged?.Invoke(LogCategory.Error, $"Failed ({sw.ElapsedMilliseconds}ms): {ex.Message}");
        }
        finally
        {
            _receiving = false;
            OnStatusChanged?.Invoke(LogCategory.Success, "Ready");
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _udp.Dispose();
    }
}
