using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using StratagemDeck.Mobile.Models;

namespace StratagemDeck.Mobile.Services;

public class StratagemSender
{
    private readonly int _port;

    public StratagemSender(int port = 12345)
    {
        _port = port;
    }

    public async Task SendAsync(string ip, string pin, Stratagem stratagem)
    {
        await SendJsonAsync(ip, new
        {
            type = "stratagem",
            pin,
            name = stratagem.Name,
            keys = stratagem.Keys
        });
    }

    public async Task SendKeyAsync(string ip, string pin, string key, string action = "tap")
    {
        await SendJsonAsync(ip, new
        {
            type = "key",
            pin,
            key,
            action
        });
    }

    private async Task SendJsonAsync(string ip, object message)
    {
        var json = JsonSerializer.Serialize(message);
        var data = Encoding.UTF8.GetBytes(json);

        using var udp = new UdpClient();
        await udp.SendAsync(data, new IPEndPoint(IPAddress.Parse(ip), _port));
    }
}
