using System.Text.Json;
using HD2Companion.Mobile.Models;
using SkiaSharp;
using Svg.Skia;

namespace HD2Companion.Mobile.Services;

public class StratagemDataService
{
    private Dictionary<string, List<Stratagem>> _byCategory = new();
    public List<string> Categories { get; private set; } = new();

    public async Task LoadAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("stratagems.json");
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var raw = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
        if (raw == null) return;

        _byCategory.Clear();
        Categories.Clear();

        foreach (var (category, strats) in raw)
        {
            Categories.Add(category);
            var list = new List<Stratagem>();
            foreach (var (name, keysStr) in strats)
            {
                var strat = new Stratagem
                {
                    Name = name,
                    Category = category,
                    Keys = keysStr.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList()
                };

                var iconName = strat.GetNormalizedFileName();
                try
                {
                    using var iconStream = await FileSystem.OpenAppPackageFileAsync(iconName);
                    strat.IconSource = DecodeSvgToImageSource(iconStream);
                }
                catch
                {
                    strat.IconSource = null;
                }

                list.Add(strat);
            }
            _byCategory[category] = list;
        }
    }

    private static ImageSource DecodeSvgToImageSource(Stream svgStream)
    {
        using var svg = new SKSvg();
        svg.Load(svgStream);
        if (svg.Picture == null)
            throw new InvalidOperationException("Failed to load SVG");

        var size = svg.Picture.CullRect;
        float maxDim = Math.Max(size.Width, size.Height);
        float scale = maxDim > 0 ? 96f / maxDim : 1f;

        var bitmap = new SKBitmap(
            (int)(size.Width * scale),
            (int)(size.Height * scale));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scale);
        canvas.DrawPicture(svg.Picture);

        using var image = SKImage.FromBitmap(bitmap);
        using var pngData = image.Encode(SKEncodedImageFormat.Png, 100);
        var pngBytes = pngData.ToArray();
        return ImageSource.FromStream(() => new MemoryStream(pngBytes));
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
            .Where(s => s.Name.Contains(lower, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public Stratagem? FindByName(string name, string? category = null)
    {
        if (category != null && _byCategory.TryGetValue(category, out var list))
            return list.FirstOrDefault(s => s.Name == name);
        return _byCategory.Values.SelectMany(x => x).FirstOrDefault(s => s.Name == name);
    }
}
