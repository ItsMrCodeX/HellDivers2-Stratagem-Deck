using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using StratagemDeck.Server.Models;
using StratagemDeck.Server.Native;

namespace StratagemDeck.Server.Services;

public class CommandListener : IDisposable
{
    private readonly UdpClient _udp;
    private readonly PinManager _pinManager;
    private readonly CancellationTokenSource _cts = new();
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

                if (json.Contains("\"type\":\"ping\""))
                {
                    await HandlePing(result.RemoteEndPoint, json);
                    continue;
                }

                if (json.Contains("\"type\":\"discover\""))
                {
                    await HandleDiscover(result.RemoteEndPoint);
                    continue;
                }

                if (json.Contains("\"type\":\"stratagem\""))
                {
                    var cmd = JsonSerializer.Deserialize<StratagemCommand>(json);
                    if (cmd != null)
                        await HandleStratagem(result.RemoteEndPoint, cmd);
                }
            }
            catch (OperationCanceledException) { break; }
            catch { }
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

    private async Task HandlePing(IPEndPoint sender, string json)
    {
        var ping = JsonSerializer.Deserialize<PingMessage>(json);
        if (ping == null || !_pinManager.Validate(ping.Pin))
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
