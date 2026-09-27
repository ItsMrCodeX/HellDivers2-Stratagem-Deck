using System.Text.Json;
using System.Security.Cryptography;
using StratagemDeck.Mobile.Models;
using SkiaSharp;
using Svg.Skia;

namespace StratagemDeck.Mobile.Services;

public class StratagemDataService
{
    private Dictionary<string, List<Stratagem>> _byCategory = new();
    public List<string> Categories { get; private set; } = new();

    private class StratagemEntry
    {
        [System.Text.Json.Serialization.JsonPropertyName("keys")]
        public string Keys { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("shortName")]
        public string? ShortName { get; set; }
    }

    private static string CacheDir => Path.Combine(FileSystem.AppDataDirectory, "icon_cache");

    public async Task LoadAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("stratagems.json");
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var raw = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, StratagemEntry>>>(json);
        if (raw == null) return;

        _byCategory.Clear();
        Categories.Clear();

        foreach (var (category, strats) in raw)
        {
            Categories.Add(category);
            var list = new List<Stratagem>();
            foreach (var (name, entry) in strats)
            {
                list.Add(new Stratagem
                {
                    Name = name,
                    Category = category,
                    ShortName = entry.ShortName,
                    Keys = entry.Keys.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList()
                });
            }
            _byCategory[category] = list;
        }
    }

    public async Task LoadIconsAsync()
    {
        Directory.CreateDirectory(CacheDir);

        foreach (var strats in _byCategory.Values)
        {
            foreach (var strat in strats)
            {
                var source = await Task.Run(() => LoadIconAsync(strat));
                strat.IconSource = source;
            }
        }
    }

    private async Task<ImageSource?> LoadIconAsync(Stratagem strat)
    {
        var iconName = strat.GetNormalizedFileName();

        byte[] svgBytes;
        try
        {
            using var iconStream = await FileSystem.OpenAppPackageFileAsync(iconName);
            using var ms = new MemoryStream();
            await iconStream.CopyToAsync(ms);
            svgBytes = ms.ToArray();
        }
        catch
        {
            return null;
        }

        var hash = Convert.ToHexString(SHA256.HashData(svgBytes))[..16];
        var baseName = Path.GetFileNameWithoutExtension(iconName);
        var cacheKey = $"{baseName}_{hash}.png";
        var cachePath = Path.Combine(CacheDir, cacheKey);

        if (!IsCacheFileValid(cachePath))
        {
            using var svgStream = new MemoryStream(svgBytes);
            var pngBytes = DecodeSvgToPng(svgStream);
            if (pngBytes == null)
                return null;

            try
            {
                var tempPath = cachePath + ".tmp";
                await File.WriteAllBytesAsync(tempPath, pngBytes);
                File.Move(tempPath, cachePath, overwrite: true);
            }
            catch
            {
                return null;
            }
        }

        return IsCacheFileValid(cachePath)
            ? ImageSource.FromFile(cachePath)
            : null;
    }

    private static bool IsCacheFileValid(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists && info.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static byte[]? DecodeSvgToPng(Stream svgStream)
    {
        try
        {
            using var svg = new SKSvg();
            svg.Load(svgStream);
            if (svg.Picture == null)
                return null;

            var size = svg.Picture.CullRect;
            if (size.Width <= 0 || size.Height <= 0)
                return null;

            float maxDim = Math.Max(size.Width, size.Height);
            float scale = maxDim > 0 ? 96f / maxDim : 1f;
            int width = Math.Max(1, (int)(size.Width * scale));
            int height = Math.Max(1, (int)(size.Height * scale));

            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent);
            canvas.Scale(scale);
            canvas.DrawPicture(svg.Picture);

            using var image = SKImage.FromBitmap(bitmap);
            return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
        }
        catch
        {
            return null;
        }
    }

    public List<Stratagem> GetAll()
    {
        return _byCategory.Values.SelectMany(x => x).ToList();
    }

    public List<Stratagem> GetByCategory(string category)
    {
        return _byCategory.TryGetValue(category, out var list) ? list : new List<Stratagem>();
    }

    public List<Stratagem> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<Stratagem>();

        var lower = query.ToLowerInvariant();
        return _byCategory.Values
            .SelectMany(x => x)
            .Where(s => s.Name.Contains(lower, StringComparison.OrdinalIgnoreCase)
                     || s.DisplayName.Contains(lower, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public Stratagem? FindByName(string name, string? category = null)
    {
        if (category != null && _byCategory.TryGetValue(category, out var list))
            return list.FirstOrDefault(s => s.Name == name);
        return _byCategory.Values.SelectMany(x => x).FirstOrDefault(s => s.Name == name);
    }
}
