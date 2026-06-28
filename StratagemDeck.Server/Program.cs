using StratagemDeck.Server.Services;
using QRCoder;

const int cmdPort = 12345;
const int broadcastPort = 12346;

var localIps = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
    .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
    .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
    .Select(a => a.Address.ToString())
    .Where(ip => !ip.StartsWith("127."))
    .ToList();

if (localIps.Count == 0) localIps.Add("0.0.0.0");
var pinManager = new PinManager();
var initialQr = GenerateQrCode(localIps[^1], pinManager.CurrentPin);

Console.Title = "Stratagem Deck Server";
using var tui = new TerminalLayout();
tui.Initialize(localIps, pinManager.CurrentPin, initialQr);

using var listener = new CommandListener(pinManager, cmdPort);
using var broadcaster = new DiscoveryBroadcaster(pinManager, broadcastPort);

listener.OnStatusChanged += (category, msg) =>
{
    tui.EnqueueLog(new LogEntry(msg, category, DateTime.Now));
    // Block injected arrow keys from SendInput while stratagem executes
    if (category == LogCategory.Stratagem && msg.StartsWith("Executing:"))
        tui.IgnoreInput = true;
    else if (category == LogCategory.Success && (msg == "Ready" || msg.StartsWith("Done")))
        tui.IgnoreInput = false;
};

var cycleQr = () =>
{
    var ip = tui.CurrentIp;
    var qr = GenerateQrCode(ip, pinManager.CurrentPin);
    tui.UpdateQrCode(ip, pinManager.CurrentPin, qr);
    tui.EnqueueLog(new LogEntry($"Switched IP to {ip}", LogCategory.System, DateTime.Now));
};

tui.OnRegenerateRequested += () =>
{
    pinManager.Regenerate();
    var qr = GenerateQrCode(tui.CurrentIp, pinManager.CurrentPin);
    tui.UpdateQrCode(tui.CurrentIp, pinManager.CurrentPin, qr);
    tui.EnqueueLog(new LogEntry($"PIN regenerated: {pinManager.CurrentPin}", LogCategory.System, DateTime.Now));
};

tui.OnIpCycleRequested += _ => cycleQr();

tui.EnqueueLog(new LogEntry($"Listening on UDP {cmdPort} ", LogCategory.Info, DateTime.Now));
tui.EnqueueLog(new LogEntry($"Broadcasting on UDP {broadcastPort} ", LogCategory.Info, DateTime.Now));

listener.Start();
broadcaster.Start();

tui.Run();

listener.Stop();
broadcaster.Stop();

static string GenerateQrCode(string ip, string pin)
{
    var payload = $"{ip}:{pin}";
    using var qrGenerator = new QRCodeGenerator();
    using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.L);
    using var qrCode = new AsciiQRCode(qrData);
    return qrCode.GetGraphic(1, "█", " ", drawQuietZones: false);
}
