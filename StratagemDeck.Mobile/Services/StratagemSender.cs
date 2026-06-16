using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using HD2Companion.Mobile.Models;

namespace HD2Companion.Mobile.Services;

public class StratagemSender
{
    private readonly int _port;

    public StratagemSender(int port = 12345)
    {
        _port = port;
    }

    public async Task SendAsync(string ip, string pin, Stratagem stratagem)
    {
        var msg = new
        {
            type = "stratagem",
            pin,
            name = stratagem.Name,
            keys = stratagem.Keys
        };

        var json = JsonSerializer.Serialize(msg);
        var data = Encoding.UTF8.GetBytes(json);

        using var udp = new UdpClient();
        await udp.SendAsync(data, new IPEndPoint(IPAddress.Parse(ip), _port));
    }
}
