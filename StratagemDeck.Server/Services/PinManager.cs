using System.Security.Cryptography;

namespace HD2Companion.Server.Services;

public class PinManager
{
    public string CurrentPin { get; private set; }

    public PinManager()
    {
        CurrentPin = GeneratePin();
    }

    public void Regenerate()
    {
        CurrentPin = GeneratePin();
    }

    public bool Validate(string? pin)
    {
        return pin == CurrentPin;
    }

    private static string GeneratePin()
    {
        return RandomNumberGenerator.GetInt32(0, 10000).ToString("D4");
    }
}
