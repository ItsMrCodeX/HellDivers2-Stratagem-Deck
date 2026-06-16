using HD2Companion.Server.Services;
using QRCoder;

var cmdPort = 12345;
var pinManager = new PinManager();

Console.Title = "HD2 Companion Server";
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════╗");
Console.WriteLine("║   HD2 Companion Server v1.0  ║");
Console.WriteLine("╚══════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

var localIps = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
    .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
    .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
    .Select(a => a.Address.ToString())
    .Where(ip => !ip.StartsWith("127."))
    .ToList();

var mainIp = localIps.LastOrDefault() ?? "0.0.0.0";

Console.WriteLine($"IPs: {string.Join(", ", localIps)}");
Console.WriteLine($"PIN: {pinManager.CurrentPin}");
Console.WriteLine($"Listening on UDP port {cmdPort}");
Console.WriteLine();

PrintQrCode(mainIp, pinManager.CurrentPin);

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("Commands:");
Console.WriteLine("  R - Regenerate PIN");
Console.WriteLine("  Q - Quit");
Console.ResetColor();
Console.WriteLine();

using var listener = new CommandListener(pinManager, cmdPort);

listener.OnStatusChanged += msg =>
{
    Console.ForegroundColor = msg.Contains("Executing") ? ConsoleColor.Yellow : ConsoleColor.Green;
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
    Console.ResetColor();
};

listener.Start();

while (true)
{
    var key = Console.ReadKey(true);
    if (key.Key == ConsoleKey.Q) break;
    if (key.Key == ConsoleKey.R)
    {
        pinManager.Regenerate();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"New PIN: {pinManager.CurrentPin}");
        Console.ResetColor();
        PrintQrCode(mainIp, pinManager.CurrentPin);
    }
}

listener.Stop();
Console.WriteLine("Server stopped.");

static void PrintQrCode(string ip, string pin)
{
    var payload = $"{ip}:{pin}";
    using var qrGenerator = new QRCodeGenerator();
    using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.L);
    using var qrCode = new AsciiQRCode(qrData);
    var qr = qrCode.GetGraphic(1, drawQuietZones: true);
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine(qr);
    Console.ResetColor();
}
