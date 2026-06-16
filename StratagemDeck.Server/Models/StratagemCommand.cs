using System.Text.Json.Serialization;

namespace HD2Companion.Server.Models;

public class StratagemCommand
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("pin")]
    public string Pin { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("keys")]
    public List<string> Keys { get; set; } = new();
}

public class DiscoveryMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "discovery";

    [JsonPropertyName("pc")]
    public string PcName { get; set; } = string.Empty;

    [JsonPropertyName("pin")]
    public string Pin { get; set; } = string.Empty;
}

public class PingMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "ping";

    [JsonPropertyName("pin")]
    public string Pin { get; set; } = string.Empty;
}

public class PongMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "pong";

    [JsonPropertyName("pc")]
    public string PcName { get; set; } = string.Empty;
}
