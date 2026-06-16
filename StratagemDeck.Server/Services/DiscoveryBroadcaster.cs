using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using HD2Companion.Server.Models;

namespace HD2Companion.Server.Services;

public class DiscoveryBroadcaster : IDisposable
{
    private readonly UdpClient _udp;
    private readonly PinManager _pinManager;
    private readonly int _port;
    private CancellationTokenSource? _cts;

    public DiscoveryBroadcaster(PinManager pinManager, int port = 12346)
    {
        _pinManager = pinManager;
        _port = port;
        _udp = new UdpClient();
        _udp.EnableBroadcast = true;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = BroadcastLoop(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private async Task BroadcastLoop(CancellationToken ct)
    {
        var endpoint = new IPEndPoint(IPAddress.Broadcast, _port);
        var pcName = Environment.MachineName;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var msg = new DiscoveryMessage
                {
                    PcName = pcName,
                    Pin = _pinManager.CurrentPin
                };
                var json = JsonSerializer.Serialize(msg);
                var data = Encoding.UTF8.GetBytes(json);
                await _udp.SendAsync(data, endpoint, ct);
            }
            catch (OperationCanceledException) { break; }
            catch { }

            try { await Task.Delay(2000, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _udp.Dispose();
    }
}
