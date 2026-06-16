using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using HD2Companion.Server.Models;
using HD2Companion.Server.Native;

namespace HD2Companion.Server.Services;

public class CommandListener : IDisposable
{
    private readonly UdpClient _udp;
    private readonly PinManager _pinManager;
    private readonly CancellationTokenSource _cts = new();
    private bool _receiving;

    public event Action<string>? OnStatusChanged;

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
                        await HandleStratagem(cmd);
                }
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    private async Task HandleDiscover(IPEndPoint sender)
    {
        var msg = new DiscoveryMessage
        {
            PcName = Environment.MachineName,
            Pin = _pinManager.CurrentPin
        };
        var data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));
        await _udp.SendAsync(data, sender);
    }

    private async Task HandlePing(IPEndPoint sender, string json)
    {
        var ping = JsonSerializer.Deserialize<PingMessage>(json);
        if (ping == null || !_pinManager.Validate(ping.Pin)) return;

        var pong = new PongMessage { PcName = Environment.MachineName };
        var data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pong));
        await _udp.SendAsync(data, sender);
    }

    private async Task HandleStratagem(StratagemCommand cmd)
    {
        if (!_pinManager.Validate(cmd.Pin)) return;

        if (_receiving)
        {
            OnStatusChanged?.Invoke($"Busy - ignoring {cmd.Name}");
            return;
        }

        _receiving = true;
        OnStatusChanged?.Invoke($"Executing: {cmd.Name}");

        try
        {
            await KeyInjector.ExecuteSequence(cmd.Keys);
        }
        finally
        {
            _receiving = false;
            OnStatusChanged?.Invoke("Ready");
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _udp.Dispose();
    }
}
