using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using HD2Companion.Mobile.Models;

namespace HD2Companion.Mobile.Services;

public class UdpDiscoveryService : IDisposable
{
    private readonly int _cmdPort;
    private CancellationTokenSource? _cts;
    private bool _isScanning;

    public event Action<DiscoveryInfo>? OnServerDiscovered;

    public UdpDiscoveryService(int cmdPort = 12345)
    {
        _cmdPort = cmdPort;
    }

    public void StartScanning()
    {
        if (_isScanning) return;
        _isScanning = true;
        _cts = new CancellationTokenSource();
        _ = ScanNetwork(_cts.Token);
    }

    public void StopScanning()
    {
        _isScanning = false;
        _cts?.Cancel();
    }

    public async Task<bool> PingServer(string ip, string pin)
    {
        try
        {
            var ping = new { type = "ping", pin };
            var json = JsonSerializer.Serialize(ping);
            var data = Encoding.UTF8.GetBytes(json);

            using var client = new UdpClient();
            client.Client.ReceiveTimeout = 1000;
            await client.SendAsync(data, new IPEndPoint(IPAddress.Parse(ip), _cmdPort));

            var result = await client.ReceiveAsync();
            var response = Encoding.UTF8.GetString(result.Buffer);

            return response.Contains("\"type\":\"pong\"");
        }
        catch
        {
            return false;
        }
    }

    private async Task ScanNetwork(CancellationToken ct)
    {
        var subnets = GetLocalSubnets();
        if (subnets.Count == 0) return;

        var discoverMsg = new { type = "discover" };
        var json = JsonSerializer.Serialize(discoverMsg);
        var data = Encoding.UTF8.GetBytes(json);

        using var listener = new UdpClient(0);
        listener.Client.ReceiveTimeout = 250;

        foreach (var subnet in subnets)
        {
            if (ct.IsCancellationRequested) break;

            var sem = new SemaphoreSlim(20);
            var tasks = new List<Task>();

            for (int i = 1; i < 255; i++)
            {
                if (ct.IsCancellationRequested) break;
                var ip = $"{subnet}.{i}";

                await sem.WaitAsync(ct);
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using var client = new UdpClient();
                        client.Client.SendTimeout = 100;
                        await client.SendAsync(data, new IPEndPoint(IPAddress.Parse(ip), _cmdPort));
                    }
                    catch { }
                    finally { sem.Release(); }
                }, ct));
            }

            await Task.WhenAll(tasks);
        }

        _ = ReceiveResponses(listener, ct);
    }

    private async Task ReceiveResponses(UdpClient listener, CancellationToken ct)
    {
        var seenIps = new HashSet<string>();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await listener.ReceiveAsync(ct);
                var response = Encoding.UTF8.GetString(result.Buffer);

                if (!response.Contains("\"type\":\"discovery\"")) continue;

                var msg = JsonSerializer.Deserialize<DiscoveryMessage>(response);
                if (msg == null) continue;

                var ip = result.RemoteEndPoint.Address.ToString();
                if (!seenIps.Add(ip)) continue;

                OnServerDiscovered?.Invoke(new DiscoveryInfo
                {
                    PcName = msg.PcName,
                    IpAddress = ip,
                    Pin = msg.Pin
                });
            }
            catch (OperationCanceledException) { break; }
            catch { break; }
        }
    }

    private static List<string> GetLocalSubnets()
    {
        var subnets = new HashSet<string>();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

            foreach (var ip in ni.GetIPProperties().UnicastAddresses)
            {
                if (ip.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                var bytes = ip.Address.GetAddressBytes();
                if (bytes[0] == 127) continue;
                subnets.Add($"{bytes[0]}.{bytes[1]}.{bytes[2]}");
            }
        }
        return subnets.ToList();
    }

    public void Dispose()
    {
        StopScanning();
    }

    private class DiscoveryMessage
    {
        public string Type { get; set; } = string.Empty;
        public string PcName { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }
}
